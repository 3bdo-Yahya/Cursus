using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Interfaces.Services;

namespace Cursus.BLL.Services;

public sealed class ImpactAnalysisService : IImpactAnalysisService
{
    private readonly ApplicationDbContext _db;
    private readonly IAcademicMetricsService _academicMetricsService;
    private readonly ILogger<ImpactAnalysisService> _logger;

    public ImpactAnalysisService(
        ApplicationDbContext db,
        IAcademicMetricsService academicMetricsService,
        ILogger<ImpactAnalysisService> logger)
    {
        _db = db;
        _academicMetricsService = academicMetricsService;
        _logger = logger;
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
        var courses = await CurriculumScope.ForDepartment(_db.Courses, departmentId)
            .Include(c => c.Prerequisites)
            .Include(c => c.IsPrerequisiteFor)
            .AsNoTracking()
            .ToListAsync();

        var courseById = courses.ToDictionary(c => c.Id);

        if (!courseById.TryGetValue(courseId, out var failedCourse))
            return null;

        var adjacency = BuildAdjacency(courses, courseById, _logger);
        var orderedBlocked = FindBlockedCourses(courseId, courseById, adjacency);
        var cascadeDepth = orderedBlocked.Count > 0 ? orderedBlocked.Max(b => b.Depth) : 0;
        var creditsAtRisk = orderedBlocked.Sum(b => b.CreditHours);
        var severity = GetSeverity(creditsAtRisk, cascadeDepth);

        var department = await _db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == departmentId);

        var gradeScale = await _academicMetricsService.GetGradeScaleAsync(department?.UniversityId);

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

        var completedCourseIds = completedCourseRows.Select(x => x.CourseId).ToHashSet();
        var completedCreditsByType = completedCourseRows
            .GroupBy(x => x.CourseType)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.CreditHours));

        var requirements = await _db.GraduationRequirements
            .Include(gr => gr.GraduationRequirementCourses)
            .Where(gr => gr.DepartmentId == departmentId)
            .AsNoTracking()
            .ToListAsync();

        var simulationCourses = BuildSimulationCourses(
            courses,
            requirements,
            completedCreditsByType,
            failedCourse);

        var studentCourses = await _db.StudentCourses
            .AsNoTracking()
            .Include(sc => sc.Course)
            .Where(sc => sc.StudentId == studentId)
            .ToListAsync();
        var bestAttempts = _academicMetricsService.ResolveBestAttempts(studentCourses);
        var (projectedCgpa, cgpaDelta, projectedStanding, standingWouldChange) =
            ComputeCgpaImpact(cgpa, standing, failedCourse, bestAttempts, gradeScale);
        var failurePathMaxCredits = GraduationDelayCalculator.GetMaxCreditsPerSemester(projectedStanding, projectedCgpa);

        var delay = GraduationDelayCalculator.Calculate(
            currentSemester,
            academicYear,
            standing,
            cgpa,
            failedCourse.Id,
            failedCourse.SemesterAvailability,
            simulationCourses,
            completedCourseIds,
            adjacency,
            baselineMaxCreditsOverride: null,
            failureMaxCreditsOverride: failurePathMaxCredits);

        var blockedWithTerms = EnrichBlockedTerms(orderedBlocked, delay);

        var replacementCourses = FindReplacementCourses(
            failedCourse,
            courses,
            completedCourseIds,
            blockedWithTerms.Select(b => b.CourseId).ToHashSet());
        var scenario = DetectScenario(
            failedCourse,
            blockedWithTerms,
            courses,
            completedCourseIds,
            replacementCourses);
        var recoverySchedule = MapRecoverySchedule(delay.FailureSchedule);
        var recommendations = BuildRecommendations(
            failedCourse,
            delay,
            scenario,
            blockedWithTerms,
            standingWouldChange,
            projectedStanding);

        return new ImpactAnalysisResultDto(
            FailedCourseId: failedCourse.Id,
            FailedCourseCode: failedCourse.Code,
            FailedCourseName: failedCourse.Name,
            FailedCourseCredits: failedCourse.CreditHours,
            BlockedCourses: blockedWithTerms,
            BlockedCoursesCount: blockedWithTerms.Count,
            CascadeDepth: cascadeDepth,
            CreditsAtRisk: creditsAtRisk,
            Severity: severity,
            GraduationDelaySemesters: delay.GraduationDelaySemesters,
            RetakeDelaySemesters: delay.RetakeDelaySemesters,
            RecoverySemesters: delay.RecoverySemesters,
            MaxCreditsPerSemester: delay.MaxCreditsPerSemester,
            SemestersAffected: delay.SemestersAffected,
            RetakeSemesterLabel: delay.RetakeSemesterLabel,
            ProjectedGraduationLabel: delay.ProjectedGraduationLabel,
            OriginalGraduationLabel: delay.OriginalGraduationLabel,
            CurrentCgpa: cgpa,
            ProjectedCgpa: projectedCgpa,
            CgpaDelta: cgpaDelta,
            CurrentStanding: standing,
            ProjectedStanding: projectedStanding,
            StandingWouldChange: standingWouldChange,
            ScenarioType: scenario.Type,
            ScenarioSummary: scenario.Summary,
            RecoverySchedule: recoverySchedule,
            Recommendations: recommendations,
            ReplacementCourses: replacementCourses);
    }

    private static Dictionary<int, List<int>> BuildAdjacency(
        List<Course> courses,
        Dictionary<int, Course> courseById,
        ILogger logger)
    {
        var adjacency = new Dictionary<int, List<int>>();
        foreach (var course in courses)
        {
            foreach (var edge in course.IsPrerequisiteFor)
            {
                if (!courseById.ContainsKey(edge.CourseId))
                {
                    logger.LogWarning(
                        "Prerequisite edge {PrereqCode} -> course {DependentId} omitted from blocked-course BFS (not in department scope or inactive).",
                        course.Code,
                        edge.CourseId);
                    continue;
                }

                if (!adjacency.TryGetValue(course.Id, out var dependents))
                {
                    dependents = [];
                    adjacency[course.Id] = dependents;
                }

                dependents.Add(edge.CourseId);
            }
        }

        return adjacency;
    }

    private static List<BlockedCourseDto> FindBlockedCourses(
        int courseId,
        Dictionary<int, Course> courseById,
        Dictionary<int, List<int>> adjacency)
    {
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
                if (!visited.Add(depId))
                    continue;

                var dep = courseById[depId];
                blocked.Add(new BlockedCourseDto(
                    CourseId: dep.Id,
                    Code: dep.Code,
                    Name: dep.Name,
                    CreditHours: dep.CreditHours,
                    Depth: depth + 1));

                queue.Enqueue((depId, depth + 1));
            }
        }

        return blocked
            .OrderBy(b => b.Depth)
            .ThenBy(b => b.Code, StringComparer.Ordinal)
            .ToList();
    }

    private static List<Course> BuildSimulationCourses(
        List<Course> courses,
        List<GraduationRequirement> requirements,
        Dictionary<CourseType, int> completedCreditsByType,
        Course failedCourse)
    {
        var prereqIds = courses
            .SelectMany(c => c.Prerequisites.Select(p => p.PrerequisiteId))
            .ToHashSet();

        var simulationCourses = courses
            .Where(c => c.CourseType == CourseType.Core
                        || prereqIds.Contains(c.Id)
                        || c.Id == failedCourse.Id)
            .Select(CopyCourseForSimulation)
            .ToList();

        var virtualIdCounter = 1;
        foreach (var req in requirements)
        {
            if (req.CategoryType == CourseType.Core)
                continue;

            var completedCredits = completedCreditsByType.TryGetValue(req.CategoryType, out var credits)
                ? credits
                : 0;
            var remainingCredits = Math.Max(0, req.RequiredCredits - completedCredits);
            var coursesNeeded = (int)Math.Ceiling(remainingCredits / 3.0);

            for (var i = 0; i < coursesNeeded; i++)
            {
                simulationCourses.Add(new Course
                {
                    Id = -100 - (int)req.CategoryType * 100 - virtualIdCounter++,
                        Code = $"ELEC-{req.CategoryType.ToString().ToUpper()}-{i}",
                        Name = $"{req.CategoryType} elective",
                    CreditHours = 3,
                    CourseType = req.CategoryType,
                    SemesterAvailability = SemesterAvailability.All,
                    RecommendedSemester = null,
                    Prerequisites = []
                });
            }
        }

        if (simulationCourses.All(c => c.Id != failedCourse.Id))
            simulationCourses.Add(CopyCourseForSimulation(failedCourse));

        return simulationCourses;
    }

    private static Course CopyCourseForSimulation(Course course) =>
        new()
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            CreditHours = course.CreditHours,
            CourseType = course.CourseType,
            SemesterAvailability = course.SemesterAvailability,
            RecommendedSemester = course.RecommendedSemester,
            Prerequisites = course.Prerequisites.Select(p => new CoursePrerequisite
            {
                CourseId = p.CourseId,
                PrerequisiteId = p.PrerequisiteId
            }).ToList()
        };

    private static List<BlockedCourseDto> EnrichBlockedTerms(
        List<BlockedCourseDto> blocked,
        GraduationDelayCalculator.Result delay)
    {
        return blocked.Select(b => b with
        {
            NormalTermLabel = GraduationDelayCalculator.FindTermLabelForCourse(
                delay.BaselineSchedule, b.CourseId),
            NewTermLabel = GraduationDelayCalculator.FindTermLabelForCourse(
                delay.FailureSchedule, b.CourseId)
        }).ToList();
    }

    private static (decimal projectedCgpa, decimal delta, AcademicStanding projected, bool wouldChange)
        ComputeCgpaImpact(
            decimal currentCgpa,
            AcademicStanding currentStanding,
            Course failedCourse,
            List<StudentCourse> bestAttempts,
            Dictionary<string, decimal> gradeScale)
    {
        if (!gradeScale.TryGetValue("F", out var fPoints))
            fPoints = 0m;

        var totalPoints = 0m;
        var totalCredits = 0;

        foreach (var record in bestAttempts)
        {
            if (record.Course is null || string.IsNullOrWhiteSpace(record.Grade))
                continue;

            if (record.CourseId == failedCourse.Id)
                continue;

            if (record.Status != StudentCourseStatus.Completed && record.Status != StudentCourseStatus.Failed)
                continue;

            var gradeKey = record.Grade.Trim().ToUpper();
            if (!gradeScale.TryGetValue(gradeKey, out var points))
                continue;

            totalPoints += points * record.Course.CreditHours;
            totalCredits += record.Course.CreditHours;
        }

        totalPoints += fPoints * failedCourse.CreditHours;
        totalCredits += failedCourse.CreditHours;

        var projected = totalCredits == 0
            ? 0m
            : Math.Round(totalPoints / totalCredits, 2);
        var delta = Math.Round(projected - currentCgpa, 2);
        var projectedStanding = ResolveStanding(projected);

        return (projected, delta, projectedStanding, projectedStanding != currentStanding);
    }

    private static AcademicStanding ResolveStanding(decimal cumulativeGpa)
    {
        if (cumulativeGpa < 2.00m)
            return AcademicStanding.Probation;

        if (cumulativeGpa < 2.25m)
            return AcademicStanding.Warning;

        return AcademicStanding.Good;
    }

    private sealed record ScenarioResult(FailureScenarioType Type, string Summary);

    private static ScenarioResult DetectScenario(
        Course failedCourse,
        List<BlockedCourseDto> blocked,
        List<Course> courses,
        HashSet<int> completedCourseIds,
        IReadOnlyList<RecoveryCourseDto> replacementCourses)
    {
        var recSem = failedCourse.RecommendedSemester
            ?? SemesterMath.InferFromCourseCode(failedCourse.Code);

        if (SemesterMath.IsSpringTerm(recSem))
        {
            return new ScenarioResult(
                FailureScenarioType.Semester2Failure,
                $"Failing {CourseDisplayHelper.Label(failedCourse)} in Spring delays dependents until it can be retaken as soon as it is offered, then normal continuation resumes.");
        }

        var nextSpringSem = recSem.HasValue ? recSem.Value + 1 : (int?)null;
        var blockedSpringCourses = courses
            .Where(c => c.RecommendedSemester == nextSpringSem)
            .Where(c => blocked.Any(b => b.CourseId == c.Id))
            .Select(c => CourseDisplayHelper.Label(c))
            .ToList();

        if (blockedSpringCourses.Count > 0)
        {
            var replacementText = replacementCourses.Count > 0
                ? $" Consider {string.Join(", ", replacementCourses.Take(3).Select(c => CourseDisplayHelper.Label(c.Code, c.Name)))} while waiting to recover."
                : string.Empty;

            return new ScenarioResult(
                FailureScenarioType.Semester1WithBlock,
                $"Failing {CourseDisplayHelper.Label(failedCourse)} blocks {string.Join(", ", blockedSpringCourses)} next Spring. Retake as soon as it is offered to unlock the blocked path.{replacementText}");
        }

        return new ScenarioResult(
            FailureScenarioType.Semester1NoBlock,
            $"Failing {CourseDisplayHelper.Label(failedCourse)} does not block next-term courses. Continue as planned and retake as soon as it is offered.");
    }

    private static List<RecoveryCourseDto> FindReplacementCourses(
        Course failedCourse,
        List<Course> courses,
        HashSet<int> completedCourseIds,
        HashSet<int> blockedIds)
    {
        var failedRec = SemesterMath.ResolveRecommendedSemester(failedCourse);

        return courses
            .Where(c => c.Id != failedCourse.Id)
            .Where(c => c.CourseType != CourseType.UniversityReq)
            .Where(c => !completedCourseIds.Contains(c.Id))
            .Where(c => !blockedIds.Contains(c.Id))
            .Where(c => SemesterMath.ResolveRecommendedSemester(c) > failedRec)
            .Where(c => c.Prerequisites.All(p => completedCourseIds.Contains(p.PrerequisiteId)))
            .OrderBy(c => SemesterMath.ResolveRecommendedSemester(c))
            .GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Select(c => new RecoveryCourseDto(
                Code: c.Code,
                Name: c.Name,
                CreditHours: c.CreditHours,
                IsRetake: false,
                IsNewlyUnlocked: false))
            .Take(5)
            .ToList();
    }

    private static List<RecoverySemesterDto> MapRecoverySchedule(
        IReadOnlyList<GraduationDelayCalculator.ScheduledTermEntry> failureSchedule)
    {
        return failureSchedule
            .Select(term =>
            {
                var courses = term.Courses
                    .Where(c => !CourseDisplayHelper.IsVirtualPlaceholderCode(c.Code))
                    .Select(c => new RecoveryCourseDto(
                        Code: c.Code,
                        Name: c.Name,
                        CreditHours: c.CreditHours,
                        IsRetake: c.IsRetake,
                        IsNewlyUnlocked: c.IsNewlyUnlocked))
                    .ToList();

                return new RecoverySemesterDto(
                    Label: term.Label,
                    Courses: courses,
                    IsRetakeTerm: term.Courses.Any(c => c.IsRetake));
            })
            .Where(term => term.Courses.Any())
            .ToList();
    }

    private static List<string> BuildRecommendations(
        Course failedCourse,
        GraduationDelayCalculator.Result delay,
        ScenarioResult scenario,
        List<BlockedCourseDto> blocked,
        bool standingWouldChange,
        AcademicStanding projectedStanding)
    {
        var recs = new List<string>
        {
            $"Register for <strong>{CourseDisplayHelper.Label(failedCourse)}</strong> in {delay.RetakeSemesterLabel} (retake, assume pass)."
        };

        if (delay.GraduationDelaySemesters > 0)
        {
            recs.Add(
                $"Graduation shifts from {delay.OriginalGraduationLabel} to {delay.ProjectedGraduationLabel} (+{delay.GraduationDelaySemesters} semester{(delay.GraduationDelaySemesters > 1 ? "s" : "")}).");
        }
        else
        {
            recs.Add("No graduation delay expected if you pass the retake on schedule.");
        }

        if (blocked.Count > 0)
        {
            var direct = blocked
                .Where(b => b.Depth == 1)
                .Select(b => CourseDisplayHelper.Label(b.Code, b.Name))
                .Take(3);
            recs.Add($"Blocked courses ({string.Join(", ", direct)}) unlock after the retake.");
        }

        if (standingWouldChange)
        {
            recs.Add($"CGPA impact may change standing to <strong>{projectedStanding}</strong> — meet your advisor promptly.");
        }

        return recs;
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




