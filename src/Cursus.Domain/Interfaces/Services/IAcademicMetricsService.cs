using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Interfaces.Services;

public interface IAcademicMetricsService
{
    Task<Dictionary<string, decimal>> GetGradeScaleAsync(int? universityId);
    List<StudentCourse> ResolveBestAttempts(IEnumerable<StudentCourse> studentCourses);
    decimal CalculateCgpa(IEnumerable<StudentCourse> bestAttempts, Dictionary<string, decimal> gradeScale);
    List<TermGpaDto> CalculateSgpaByTerm(IEnumerable<StudentCourse> studentCourses, Dictionary<string, decimal> gradeScale);
    TermGpaDto? GetPreviousTerm(IReadOnlyList<TermGpaDto> terms, string? academicYear, SemesterType semester);
    int GetCreditLimits(AcademicStanding standing, decimal cgpa);
    Task<(bool CanEnroll, string? BlockReason)> CanEnrollInCourseAsync(string studentId, int courseId);
}
