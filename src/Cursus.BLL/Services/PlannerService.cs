using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services;

public sealed class PlannerService : IPlannerService
{
    private readonly ApplicationDbContext _db;

    public PlannerService(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<PlanningTermDto>> GetPlanningTermsAsync(
        string studentId,
        int creditLimit,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studentId);

        await PruneStalePlansAsync(studentId, cancellationToken);

        var student = await _db.Users
            .Include(u => u.Department)
            .Include(u => u.StudentCourses)
                .ThenInclude(sc => sc.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == studentId, cancellationToken);

        if (student is null)
            return Array.Empty<PlanningTermDto>();

        var requiredCredits = student.Department?.TotalCreditsRequired ?? 132;
        var completedCredits = student.StudentCourses
            .Where(sc => sc.Status == StudentCourseStatus.Completed && sc.Course is not null)
            .Sum(sc => sc.Course!.CreditHours);

        var remainingCredits = Math.Max(0, requiredCredits - completedCredits);
        var termCount = Math.Max(2, (int)Math.Ceiling(remainingCredits / (double)Math.Max(1, creditLimit)) + 1);

        var terms = BuildTermSequence(student.AcademicYear, student.CurrentSemester, termCount);

        var forcedCreditsByTerm = student.StudentCourses
            .Where(sc => sc.Status == StudentCourseStatus.InProgress && sc.Course is not null)
            .GroupBy(sc => TermKey(sc.AcademicYear, sc.Semester))
            .ToDictionary(g => g.Key, g => g.Sum(sc => sc.Course!.CreditHours));

        var primaryIndex = terms.FindIndex(term =>
        {
            var key = TermKey(term.AcademicYear, term.Semester);
            var forced = forcedCreditsByTerm.GetValueOrDefault(key, 0);
            return forced < creditLimit;
        });

        if (primaryIndex < 0)
        {
            primaryIndex = terms.Count - 1;
        }

        var result = new List<PlanningTermDto>(terms.Count);
        for (var i = 0; i < terms.Count; i++)
        {
            result.Add(new PlanningTermDto
            {
                AcademicYear = terms[i].AcademicYear,
                Semester = terms[i].Semester,
                IsPrimary = i == primaryIndex
            });
        }

        return result;
    }

    public async Task<PlannerTermCapacityDto> GetTermCapacityAsync(
        string studentId,
        string academicYear,
        SemesterType semester,
        int creditLimit,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(academicYear);

        await PruneStalePlansAsync(studentId, cancellationToken);

        var forcedCredits = await _db.StudentCourses
            .AsNoTracking()
            .Where(sc => sc.StudentId == studentId
                         && sc.AcademicYear == academicYear
                         && sc.Semester == semester
                         && sc.Status == StudentCourseStatus.InProgress
                         && sc.Course != null)
            .SumAsync(sc => sc.Course!.CreditHours, cancellationToken);

        var plannedCredits = await _db.PlannedCourses
            .AsNoTracking()
            .Where(pc => pc.StudentId == studentId
                         && pc.AcademicYear == academicYear
                         && pc.Semester == semester
                         && pc.Course != null)
            .SumAsync(pc => pc.Course!.CreditHours, cancellationToken);

        return new PlannerTermCapacityDto
        {
            AcademicYear = academicYear,
            Semester = semester,
            ForcedInProgressCredits = forcedCredits,
            PlannedCredits = plannedCredits,
            RemainingRoom = creditLimit - forcedCredits - plannedCredits
        };
    }

    public async Task<IReadOnlyList<PlannedCourseDto>> GetPlanAsync(
        string studentId,
        string academicYear,
        SemesterType semester,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(academicYear);

        await PruneStalePlansAsync(studentId, cancellationToken);

        return await QueryPlansAsync(studentId, academicYear, semester, cancellationToken);
    }

    public async Task<IReadOnlyList<PlannedCourseDto>> GetAllPlansAsync(
        string studentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studentId);

        await PruneStalePlansAsync(studentId, cancellationToken);

        return await _db.PlannedCourses
            .AsNoTracking()
            .Where(pc => pc.StudentId == studentId)
            .OrderBy(pc => pc.AcademicYear)
            .ThenBy(pc => pc.Semester)
            .ThenBy(pc => pc.Course!.Code)
            .Select(pc => new PlannedCourseDto
            {
                CourseId = pc.CourseId,
                Code = pc.Course!.Code,
                Name = pc.Course.Name,
                CreditHours = pc.Course.CreditHours,
                CourseType = pc.Course.CourseType,
                AcademicYear = pc.AcademicYear,
                Semester = pc.Semester
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AddPlannedCourseAsync(
        string studentId,
        int courseId,
        string academicYear,
        SemesterType semester,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(academicYear);

        var student = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == studentId, cancellationToken);

        if (student is null)
            return false;

        var course = await _db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.IsActive, cancellationToken);

        if (course is null || !IsCourseInScope(course, student.DepartmentId))
            return false;

        if (await IsSupersededAsync(studentId, courseId, cancellationToken))
            return false;

        var exists = await _db.PlannedCourses.AnyAsync(
            pc => pc.StudentId == studentId
                  && pc.CourseId == courseId
                  && pc.AcademicYear == academicYear
                  && pc.Semester == semester,
            cancellationToken);

        if (exists)
            return false;

        _db.PlannedCourses.Add(new PlannedCourse
        {
            StudentId = studentId,
            CourseId = courseId,
            AcademicYear = academicYear,
            Semester = semester
        });

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemovePlannedCourseAsync(
        string studentId,
        int courseId,
        string academicYear,
        SemesterType semester,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(academicYear);

        var planned = await _db.PlannedCourses.FirstOrDefaultAsync(
            pc => pc.StudentId == studentId
                  && pc.CourseId == courseId
                  && pc.AcademicYear == academicYear
                  && pc.Semester == semester,
            cancellationToken);

        if (planned is null)
            return false;

        _db.PlannedCourses.Remove(planned);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<PlannedCourseDto>> QueryPlansAsync(
        string studentId,
        string academicYear,
        SemesterType semester,
        CancellationToken cancellationToken)
    {
        return await _db.PlannedCourses
            .AsNoTracking()
            .Where(pc => pc.StudentId == studentId
                         && pc.AcademicYear == academicYear
                         && pc.Semester == semester)
            .OrderBy(pc => pc.Course!.Code)
            .Select(pc => new PlannedCourseDto
            {
                CourseId = pc.CourseId,
                Code = pc.Course!.Code,
                Name = pc.Course.Name,
                CreditHours = pc.Course.CreditHours,
                CourseType = pc.Course.CourseType,
                AcademicYear = pc.AcademicYear,
                Semester = pc.Semester
            })
            .ToListAsync(cancellationToken);
    }

    private async Task PruneStalePlansAsync(string studentId, CancellationToken cancellationToken)
    {
        var student = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == studentId, cancellationToken);

        if (student is null)
            return;

        var plannedCourses = await _db.PlannedCourses
            .Include(pc => pc.Course)
            .Where(pc => pc.StudentId == studentId)
            .ToListAsync(cancellationToken);

        if (plannedCourses.Count == 0)
            return;

        var supersededCourseIds = await _db.StudentCourses
            .AsNoTracking()
            .Where(sc => sc.StudentId == studentId)
            .Select(sc => sc.CourseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var supersededSet = supersededCourseIds.ToHashSet();
        var toRemove = new List<PlannedCourse>();

        foreach (var planned in plannedCourses)
        {
            if (planned.Course is null
                || !planned.Course.IsActive
                || !IsCourseInScope(planned.Course, student.DepartmentId)
                || supersededSet.Contains(planned.CourseId)
                || IsPastTerm(planned.AcademicYear, planned.Semester, student.AcademicYear, student.CurrentSemester))
            {
                toRemove.Add(planned);
            }
        }

        if (toRemove.Count == 0)
            return;

        _db.PlannedCourses.RemoveRange(toRemove);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsSupersededAsync(string studentId, int courseId, CancellationToken cancellationToken)
    {
        return await _db.StudentCourses.AnyAsync(
            sc => sc.StudentId == studentId && sc.CourseId == courseId,
            cancellationToken);
    }

    private static List<AcademicTerm> BuildTermSequence(string? startAcademicYear, SemesterType startSemester, int count)
    {
        var terms = new List<AcademicTerm>(count);
        var year = NormalizeAcademicYear(startAcademicYear);
        var semester = startSemester;

        while (terms.Count < count)
        {
            if (semester != SemesterType.Summer)
            {
                terms.Add(new AcademicTerm(year, semester));
            }

            (year, semester) = NextTerm(year, semester);
        }

        return terms;
    }

    private static (string Year, SemesterType Semester) NextTerm(string academicYear, SemesterType semester)
    {
        if (semester == SemesterType.Fall)
            return (academicYear, SemesterType.Spring);

        var yearStart = TryGetYearStart(academicYear);
        var nextStart = yearStart + 1;
        return ($"{nextStart}-{nextStart + 1}", SemesterType.Fall);
    }

    private static string NormalizeAcademicYear(string? academicYear)
    {
        var yearStart = TryGetYearStart(academicYear);
        return $"{yearStart}-{yearStart + 1}";
    }

    private static int TryGetYearStart(string? academicYear)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
            return DateTime.UtcNow.Year;

        var firstChunk = academicYear.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return int.TryParse(firstChunk, out var parsedYear) ? parsedYear : DateTime.UtcNow.Year;
    }

    private static string TermKey(string academicYear, SemesterType semester) => $"{academicYear}|{(int)semester}";

    private static bool IsCourseInScope(Course course, int? departmentId) =>
        departmentId.HasValue && course.DepartmentId == departmentId.Value;

    private static bool IsPastTerm(
        string plannedYear,
        SemesterType plannedSemester,
        string? currentYear,
        SemesterType currentSemester)
    {
        if (string.IsNullOrWhiteSpace(currentYear))
            return false;

        var plannedStart = TryGetYearStart(plannedYear);
        var currentStart = TryGetYearStart(currentYear);

        if (plannedStart != currentStart)
            return plannedStart < currentStart;

        return plannedSemester < currentSemester;
    }

    private sealed record AcademicTerm(string AcademicYear, SemesterType Semester);
}

