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

        if (course is null)
            return false;

        if (!IsCourseInScope(course, student.DepartmentId))
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
            if (planned.Course is null)
            {
                toRemove.Add(planned);
                continue;
            }

            if (!planned.Course.IsActive)
            {
                toRemove.Add(planned);
                continue;
            }

            if (!IsCourseInScope(planned.Course, student.DepartmentId))
            {
                toRemove.Add(planned);
                continue;
            }

            if (supersededSet.Contains(planned.CourseId))
            {
                toRemove.Add(planned);
                continue;
            }

            if (IsPastTerm(planned.AcademicYear, planned.Semester, student.AcademicYear, student.CurrentSemester))
            {
                toRemove.Add(planned);
            }
        }

        if (toRemove.Count == 0)
            return;

        _db.PlannedCourses.RemoveRange(toRemove);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsSupersededAsync(
        string studentId,
        int courseId,
        CancellationToken cancellationToken)
    {
        return await _db.StudentCourses.AnyAsync(
            sc => sc.StudentId == studentId && sc.CourseId == courseId,
            cancellationToken);
    }

    private static bool IsCourseInScope(Course course, int? departmentId) =>
        course.CourseType == CourseType.UniversityReq
        || (departmentId.HasValue && course.DepartmentId == departmentId.Value);

    private static bool IsPastTerm(
        string plannedYear,
        SemesterType plannedSemester,
        string? currentYear,
        SemesterType currentSemester)
    {
        if (string.IsNullOrWhiteSpace(currentYear))
            return false;

        var yearCompare = string.Compare(plannedYear, currentYear, StringComparison.Ordinal);
        if (yearCompare < 0)
            return true;
        if (yearCompare > 0)
            return false;

        return plannedSemester < currentSemester;
    }
}
