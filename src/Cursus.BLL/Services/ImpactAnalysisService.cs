using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Interfaces.Services;

namespace Cursus.BLL.Services
{
    public sealed class ImpactAnalysisService : IImpactAnalysisService
    {
        private readonly ApplicationDbContext _db;

        public ImpactAnalysisService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ImpactAnalysisResultDto?> GetBlockedCoursesAsync(
            string studentId,
            int courseId,
            int departmentId,
            SemesterType currentSemester,
            string? academicYear,
            AcademicStanding standing,
            decimal cgpa)
        {
            var courses = await _db.Courses
                .Where(c => (c.DepartmentId == departmentId
                             || c.CourseType == CourseType.UniversityReq)
                            && c.IsActive)
                .Include(c => c.Prerequisites)
                .Include(c => c.IsPrerequisiteFor)
                .AsNoTracking()
                .ToListAsync();

            var courseById = courses.ToDictionary(c => c.Id);

            if (!courseById.TryGetValue(courseId, out var failedCourse))
                return null;

            // Build adjacency graph for BFS and simulation
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

            // Run BFS to find blocked courses
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

            var cascadeDepth = orderedBlocked.Count > 0
                ? orderedBlocked.Max(b => b.Depth)
                : 0;

            var creditsAtRisk = orderedBlocked.Sum(b => b.CreditHours);
            var severity = GetSeverity(creditsAtRisk, cascadeDepth);

            // Fetch student's completed courses with category metadata.
            var completedCourseRows = await _db.StudentCourses
                .AsNoTracking()
                .Where(sc => sc.StudentId == studentId
                             && sc.Status == StudentCourseStatus.Completed
                             && sc.Course != null)
                .Select(sc => new
                {
                    sc.CourseId,
                    sc.Course!.CourseType,
                    sc.Course.CreditHours
                })
                .ToListAsync();

            var completedCourseIds = completedCourseRows
                .Select(x => x.CourseId)
                .ToHashSet();

            var completedCreditsByType = completedCourseRows
                .GroupBy(x => x.CourseType)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.CreditHours));

            // Fetch graduation requirements to simulate remaining degree path
            var requirements = await _db.GraduationRequirements
                .Include(gr => gr.GraduationRequirementCourses)
                .Where(gr => gr.DepartmentId == departmentId)
                .AsNoTracking()
                .ToListAsync();

            var simulationCourses = new List<Course>();
            var coreReq = requirements.FirstOrDefault(r => r.CategoryType == CourseType.Core);
            var coreCourseIds = coreReq?.GraduationRequirementCourses.Select(rc => rc.CourseId).ToHashSet() ?? new HashSet<int>();

            // 1. Add all core courses
            foreach (var course in courses)
            {
                if (course.CourseType == CourseType.Core || coreCourseIds.Contains(course.Id))
                {
                    simulationCourses.Add(new Course
                    {
                        Id = course.Id,
                        Code = course.Code,
                        Name = course.Name,
                        CreditHours = course.CreditHours,
                        CourseType = CourseType.Core,
                        SemesterAvailability = course.SemesterAvailability,
                        Prerequisites = course.Prerequisites.Select(p => new CoursePrerequisite
                        {
                            CourseId = p.CourseId,
                            PrerequisiteId = p.PrerequisiteId
                        }).ToList()
                    });
                }
            }

            // 2. Add placeholder elective courses for elective requirements
            int virtualIdCounter = 1;
            foreach (var req in requirements)
            {
                if (req.CategoryType == CourseType.Core)
                    continue;

                int completedCredits = completedCreditsByType.TryGetValue(req.CategoryType, out var credits)
                    ? credits
                    : 0;

                int remainingCredits = Math.Max(0, req.RequiredCredits - completedCredits);
                int coursesNeeded = (int)Math.Ceiling(remainingCredits / 3.0);

                for (int i = 0; i < coursesNeeded; i++)
                {
                    simulationCourses.Add(new Course
                    {
                        Id = -100 - (int)req.CategoryType * 100 - virtualIdCounter++,
                        Code = $"ELEC-{req.CategoryType.ToString().ToUpper()}-{i}",
                        Name = $"{req.CategoryType} Elective {i}",
                        CreditHours = 3,
                        CourseType = req.CategoryType,
                        SemesterAvailability = SemesterAvailability.All,
                        Prerequisites = new List<CoursePrerequisite>()
                    });
                }
            }

            // Ensure the failed course itself is included in the simulation list
            if (!simulationCourses.Any(c => c.Id == courseId))
            {
                simulationCourses.Add(new Course
                {
                    Id = failedCourse.Id,
                    Code = failedCourse.Code,
                    Name = failedCourse.Name,
                    CreditHours = failedCourse.CreditHours,
                    CourseType = failedCourse.CourseType,
                    SemesterAvailability = failedCourse.SemesterAvailability,
                    Prerequisites = failedCourse.Prerequisites.Select(p => new CoursePrerequisite
                    {
                        CourseId = p.CourseId,
                        PrerequisiteId = p.PrerequisiteId
                    }).ToList()
                });
            }

            var delay = GraduationDelayCalculator.Calculate(
                currentSemester,
                academicYear,
                standing,
                cgpa,
                failedCourse.Id,
                failedCourse.SemesterAvailability,
                simulationCourses,
                completedCourseIds,
                adjacency);

            return new ImpactAnalysisResultDto(
                FailedCourseId: failedCourse.Id,
                FailedCourseCode: failedCourse.Code,
                FailedCourseName: failedCourse.Name,
                FailedCourseCredits: failedCourse.CreditHours,
                BlockedCourses: orderedBlocked,
                BlockedCoursesCount: orderedBlocked.Count,
                CascadeDepth: cascadeDepth,
                CreditsAtRisk: creditsAtRisk,
                Severity: severity,
                GraduationDelaySemesters: delay.GraduationDelaySemesters,
                RetakeDelaySemesters: delay.RetakeDelaySemesters,
                RecoverySemesters: delay.RecoverySemesters,
                MaxCreditsPerSemester: delay.MaxCreditsPerSemester,
                SemestersAffected: delay.GraduationDelaySemesters,
                RetakeSemesterLabel: delay.RetakeSemesterLabel,
                ProjectedGraduationLabel: delay.ProjectedGraduationLabel
            );
        }

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