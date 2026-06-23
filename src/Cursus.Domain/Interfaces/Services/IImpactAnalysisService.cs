using Cursus.Domain.DTOs;

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
        /// or transitively blocked by the simulated failure.
        /// </summary>
        /// <param name="courseId">The primary-key ID of the course to simulate as failed.</param>
        /// <param name="departmentId">
        /// Scopes the graph to courses belonging to this department
        /// (plus university-requirement courses).
        /// </param>
        /// <returns>
        /// An ordered list of <see cref="BlockedCourseDto"/> records.
        /// Empty if the course does not exist or has no downstream dependents.
        /// </returns>
        Task<IEnumerable<BlockedCourseDto>> GetBlockedCoursesAsync(int courseId, int departmentId);
    }
}
