using Cursus.Domain.DTOs;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Interfaces.Services
{
    /// <summary>
    /// Analyses the prerequisite graph to determine the downstream impact
    /// of a simulated course failure using Breadth-First Search (BFS).
    /// </summary>
    public interface IImpactAnalysisService
    {
        /// <summary>
        /// Traverses the prerequisite graph via BFS starting from
        /// <paramref name="courseId"/> to find every course that is directly
        /// or transitively blocked by the simulated failure, plus aggregate
        /// impact metrics (cascade depth, credits at risk, severity).
        /// </summary>
        /// <param name="courseId">The primary-key ID of the course to simulate as failed.</param>
        /// <param name="departmentId">
        /// Scopes the graph to courses belonging to this department
        /// (plus university-requirement courses).
        /// </param>
        /// <param name="currentSemester">Student's current semester (for retake timing).</param>
        /// <param name="academicYear">Student's academic year label (e.g. 2025-2026).</param>
        /// <param name="standing">Current academic standing (caps credits per semester).</param>
        /// <param name="cgpa">Latest cumulative GPA (affects credit cap when in good standing).</param>
        /// <returns>
        /// An <see cref="ImpactAnalysisResultDto"/> summarizing the cascade,
        /// graduation delay, and recovery timeline,
        /// or <c>null</c> if <paramref name="courseId"/> doesn't exist in the
        /// department's curriculum.
        /// </returns>
        Task<ImpactAnalysisResultDto?> GetBlockedCoursesAsync(
            int courseId,
            int departmentId,
            SemesterType currentSemester,
            string? academicYear,
            AcademicStanding standing,
            decimal cgpa);
    }
}