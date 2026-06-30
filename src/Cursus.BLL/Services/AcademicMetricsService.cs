using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services;

public class AcademicMetricsService : IAcademicMetricsService
{
    private readonly ApplicationDbContext _db;
    private const int DefaultMaxCreditsPerSemester = 18;

    public AcademicMetricsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<string, decimal>> GetGradeScaleAsync(int? universityId)
    {
        if (universityId is null)
            return BuildDefaultGradeScale();

        var gradeScale = await _db.GradeScales
            .AsNoTracking()
            .Where(gs => gs.UniversityId == universityId)
            .ToDictionaryAsync(gs => gs.LetterGrade.ToUpper(), gs => gs.PointValue);

        return gradeScale.Count > 0 ? gradeScale : BuildDefaultGradeScale();
    }

    private static Dictionary<string, decimal> BuildDefaultGradeScale()
        => new()
        {
            ["A+"] = 4.0m, ["A"] = 4.0m, ["A-"] = 3.7m,
            ["B+"] = 3.3m, ["B"] = 3.0m, ["B-"] = 2.7m,
            ["C+"] = 2.3m, ["C"] = 2.0m, ["C-"] = 1.7m,
            ["D+"] = 1.3m, ["D"] = 1.0m, ["D-"] = 1.0m, ["F"] = 0.0m
        };

    public List<StudentCourse> ResolveBestAttempts(IEnumerable<StudentCourse> studentCourses)
    {
        var gradeOrder = new Dictionary<string, int>
        {
            ["A+"] = 13, ["A"] = 12, ["A-"] = 11,
            ["B+"] = 10, ["B"] = 9, ["B-"] = 8,
            ["C+"] = 7, ["C"] = 6, ["C-"] = 5,
            ["D+"] = 4, ["D"] = 3, ["D-"] = 2,
            ["F"] = 1
        };

        int GetGradeScore(string? grade)
        {
            if (string.IsNullOrWhiteSpace(grade)) return 0;
            return gradeOrder.TryGetValue(grade.Trim().ToUpper(), out var score) ? score : 0;
        }

        return studentCourses
            .GroupBy(sc => sc.CourseId)
            .Select(g => g.OrderBy(sc => sc.Status switch
            {
                StudentCourseStatus.Completed => 0,
                StudentCourseStatus.InProgress => 1,
                StudentCourseStatus.Failed => 2,
                _ => 3
            })
            .ThenByDescending(sc => GetGradeScore(sc.Grade))
            .First())
            .ToList();
    }

    public decimal CalculateCgpa(IEnumerable<StudentCourse> bestAttempts, Dictionary<string, decimal> gradeScale)
    {
        var totalPoints = 0m;
        var totalCredits = 0;

        foreach (var record in bestAttempts)
        {
            if (record.Course is null || string.IsNullOrWhiteSpace(record.Grade))
                continue;

            if (record.Status != StudentCourseStatus.Completed && record.Status != StudentCourseStatus.Failed)
                continue;

            var gradeKey = record.Grade.Trim().ToUpper();
            if (!gradeScale.TryGetValue(gradeKey, out var points))
                continue;

            totalPoints += points * record.Course.CreditHours;
            totalCredits += record.Course.CreditHours;
        }

        return totalCredits == 0 ? 0m : Math.Round(totalPoints / totalCredits, 2);
    }

    public List<TermGpaDto> CalculateSgpaByTerm(IEnumerable<StudentCourse> studentCourses, Dictionary<string, decimal> gradeScale)
    {
        var terms = studentCourses
            .Where(sc => sc.Course is not null)
            .GroupBy(sc => new { sc.AcademicYear, sc.Semester })
            .ToList();

        var orderedTerms = terms
            .OrderBy(t => t.Key.AcademicYear)
            .ThenBy(t => t.Key.Semester)
            .ToList();

        var result = new List<TermGpaDto>();
        var coursesAttemptsSoFar = new List<StudentCourse>();

        foreach (var termGroup in orderedTerms)
        {
            var termCourses = termGroup.ToList();

            var gradedTermCourses = termCourses
                .Where(sc => (sc.Status == StudentCourseStatus.Completed || sc.Status == StudentCourseStatus.Failed)
                             && !string.IsNullOrWhiteSpace(sc.Grade))
                .ToList();

            decimal termPoints = 0m;
            int termCredits = 0;
            foreach (var sc in gradedTermCourses)
            {
                var gradeKey = sc.Grade!.Trim().ToUpper();
                if (gradeScale.TryGetValue(gradeKey, out var points))
                {
                    termPoints += points * sc.Course!.CreditHours;
                    termCredits += sc.Course.CreditHours;
                }
            }
            decimal sgpa = termCredits == 0 ? 0m : Math.Round(termPoints / termCredits, 2);

            coursesAttemptsSoFar.AddRange(termCourses);

            var bestAttemptsUpToNow = ResolveBestAttempts(coursesAttemptsSoFar);
            decimal cgpa = CalculateCgpa(bestAttemptsUpToNow, gradeScale);

            result.Add(new TermGpaDto
            {
                AcademicYear = termGroup.Key.AcademicYear,
                Semester = termGroup.Key.Semester,
                SemLabel = FormatSemesterAbbrev(termGroup.Key.Semester, termGroup.Key.AcademicYear),
                SemesterGpa = sgpa,
                CumulativeGpa = cgpa
            });
        }

        return result;
    }

    public TermGpaDto? GetPreviousTerm(IReadOnlyList<TermGpaDto> terms, string? academicYear, SemesterType semester)
    {
        if (terms.Count == 0)
            return null;

        for (var i = 0; i < terms.Count; i++)
        {
            if (MatchesTerm(terms[i], academicYear, semester))
                return i > 0 ? terms[i - 1] : null;
        }

        return null;
    }

    private static bool MatchesTerm(TermGpaDto term, string? academicYear, SemesterType semester) =>
        string.Equals(term.AcademicYear, academicYear, StringComparison.OrdinalIgnoreCase)
        && term.Semester == semester;

    private static string FormatSemesterAbbrev(SemesterType semester, string academicYear)
    {
        var semName = semester switch
        {
            SemesterType.Fall => "Fall",
            SemesterType.Spring => "Spr",
            _ => "Sum"
        };

        var year = academicYear.Split('-', '/').FirstOrDefault() ?? "";
        if (year.Length >= 4)
            year = year[2..];

        return $"{semName}\n'{year}";
    }

    public int GetCreditLimits(AcademicStanding standing, decimal cgpa)
    {
        return standing switch
        {
            AcademicStanding.Probation => 12,
            AcademicStanding.Warning => 15,
            _ => cgpa >= 3.0m ? 21 : DefaultMaxCreditsPerSemester
        };
    }

    public async Task<(bool CanEnroll, string? BlockReason)> CanEnrollInCourseAsync(string studentId, int courseId)
    {
        var attempts = await _db.StudentCourses
            .AsNoTracking()
            .Where(sc => sc.StudentId == studentId && sc.CourseId == courseId)
            .ToListAsync();

        if (attempts.Count == 0)
            return (true, null);

        if (attempts.Any(sc => sc.Status == StudentCourseStatus.InProgress))
            return (false, "Student is already enrolled in this course.");

        var completedAttempts = attempts.Where(sc => sc.Status == StudentCourseStatus.Completed).ToList();
        if (completedAttempts.Count > 0)
        {
            var nonPassingGrades = new HashSet<string> { "D+", "D", "D-", "F" };

            var hasPassedWithPassingGrade = completedAttempts.Any(sc =>
                !string.IsNullOrWhiteSpace(sc.Grade) &&
                !nonPassingGrades.Contains(sc.Grade.Trim().ToUpper()));

            if (hasPassedWithPassingGrade)
                return (false, "Student has already passed this course.");
        }

        return (true, null);
    }
}
