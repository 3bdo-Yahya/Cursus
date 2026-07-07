using Cursus.Domain.DTOs;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Interfaces.Services;

public interface IPlannerService
{
    Task<IReadOnlyList<PlannedCourseDto>> GetPlanAsync(
        string studentId,
        string academicYear,
        SemesterType semester,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlannedCourseDto>> GetAllPlansAsync(
        string studentId,
        CancellationToken cancellationToken = default);

    Task<bool> AddPlannedCourseAsync(
        string studentId,
        int courseId,
        string academicYear,
        SemesterType semester,
        CancellationToken cancellationToken = default);

    Task<bool> RemovePlannedCourseAsync(
        string studentId,
        int courseId,
        string academicYear,
        SemesterType semester,
        CancellationToken cancellationToken = default);
}
