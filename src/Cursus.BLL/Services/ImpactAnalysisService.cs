using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services
{
    /// <summary>
    /// Implements fail-cascade analysis using Breadth-First Search (BFS)
    /// over the prerequisite graph to identify all courses blocked by
    /// a simulated course failure.
    /// </summary>
    public sealed class ImpactAnalysisService : IImpactAnalysisService
    {
        private readonly IGenericRepository<Course> _courseRepository;

        public ImpactAnalysisService(IGenericRepository<Course> courseRepository)
        {
            _courseRepository = courseRepository;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<BlockedCourseDto>> GetBlockedCoursesAsync(
            int courseId, int departmentId)
        {
            // ── 1. Load all active courses in the department (+ university requirements)
            //       with their forward-dependency navigation (IsPrerequisiteFor).
            var courses = await _courseRepository.GetAll()
                .Where(c => (c.DepartmentId == departmentId
                             || c.CourseType == CourseType.UniversityReq)
                            && c.IsActive)
                .Include(c => c.IsPrerequisiteFor)
                .AsNoTracking()
                .ToListAsync();

            // ── 2. Index courses by ID for O(1) lookup.
            var courseById = courses.ToDictionary(c => c.Id);

            // If the simulated failed course is not in the loaded set, return empty.
            if (!courseById.ContainsKey(courseId))
                return Enumerable.Empty<BlockedCourseDto>();

            // ── 3. Build adjacency list: prerequisiteId → [dependent courseIds].
            //       IsPrerequisiteFor contains CoursePrerequisite rows where
            //       PrerequisiteId == this course's Id, and CourseId == the dependent.
            var adjacency = new Dictionary<int, List<int>>();
            foreach (var course in courses)
            {
                foreach (var edge in course.IsPrerequisiteFor)
                {
                    // Only include edges whose target course is in our loaded set.
                    if (!courseById.ContainsKey(edge.CourseId))
                        continue;

                    if (!adjacency.TryGetValue(course.Id, out var dependents))
                    {
                        dependents = new List<int>();
                        adjacency[course.Id] = dependents;
                    }
                    dependents.Add(edge.CourseId);
                }
            }

            // ── 4. BFS from the failed course.
            var visited = new HashSet<int> { courseId };
            var queue = new Queue<(int Id, int Depth)>();
            queue.Enqueue((courseId, 0));

            var blocked = new List<BlockedCourseDto>();

            while (queue.Count > 0)
            {
                var (currentId, depth) = queue.Dequeue();

                if (!adjacency.TryGetValue(currentId, out var dependents))
                    continue;

                foreach (var depId in dependents)
                {
                    if (visited.Add(depId))
                    {
                        var dep = courseById[depId];
                        blocked.Add(new BlockedCourseDto(
                            CourseId: dep.Id,
                            Code: dep.Code,
                            Name: dep.Name,
                            CreditHours: dep.CreditHours,
                            Depth: depth + 1
                        ));
                        queue.Enqueue((depId, depth + 1));
                    }
                }
            }

            // ── 5. Return ordered by depth (direct first), then alphabetically by code.
            return blocked
                .OrderBy(b => b.Depth)
                .ThenBy(b => b.Code, StringComparer.Ordinal);
        }
    }
}
