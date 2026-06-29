using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services
{
    public sealed class ImpactAnalysisService : IImpactAnalysisService
    {
        private readonly IGenericRepository<Course> _courseRepository;

        public ImpactAnalysisService(IGenericRepository<Course> courseRepository)
        {
            _courseRepository = courseRepository;
        }

        public async Task<ImpactAnalysisResultDto?> GetBlockedCoursesAsync(
            int courseId, int departmentId)
        {
            // ── 1-4: UNCHANGED — same loading, adjacency build, BFS loop ──
            var courses = await _courseRepository.GetAll()
                .Where(c => (c.DepartmentId == departmentId
                             || c.CourseType == CourseType.UniversityReq)
                            && c.IsActive)
                .Include(c => c.IsPrerequisiteFor)
                .AsNoTracking()
                .ToListAsync();

            var courseById = courses.ToDictionary(c => c.Id);

            if (!courseById.TryGetValue(courseId, out var failedCourse))
                return null;

            var adjacency = new Dictionary<int, List<int>>();
            foreach (var course in courses)
            {
                foreach (var edge in course.IsPrerequisiteFor)
                {
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

            var orderedBlocked = blocked
                .OrderBy(b => b.Depth)
                .ThenBy(b => b.Code, StringComparer.Ordinal)
                .ToList();

            // ── 5. NEW: aggregate metrics ──
            var cascadeDepth = orderedBlocked.Count > 0
                ? orderedBlocked.Max(b => b.Depth)
                : 0;

            var creditsAtRisk = orderedBlocked.Sum(b => b.CreditHours);

            var severity = GetSeverity(creditsAtRisk, cascadeDepth);

            return new ImpactAnalysisResultDto(
                FailedCourseId: failedCourse.Id,
                FailedCourseCode: failedCourse.Code,
                FailedCourseName: failedCourse.Name,
                FailedCourseCredits: failedCourse.CreditHours,
                BlockedCourses: orderedBlocked,
                BlockedCoursesCount: orderedBlocked.Count,
                CascadeDepth: cascadeDepth,
                CreditsAtRisk: creditsAtRisk,
                Severity: severity
            );
        }

        /// <summary>
        /// Severity tiers match the badges already styled in course-map.js
        /// and impact-analyzer.js (LOW/HIGH/CRITICAL), but driven by credits
        /// + depth instead of raw blocked-course count — a deep chain through
        /// required courses is worse than several low-credit electives.
        /// </summary>
        private static string GetSeverity(int creditsAtRisk, int cascadeDepth)
        {
            if (cascadeDepth >= 3 || creditsAtRisk > 12)
                return "Critical";

            if (cascadeDepth == 2 || creditsAtRisk >= 6)
                return "High";

            return creditsAtRisk > 0 ? "Low" : "None";
        }
    }
}