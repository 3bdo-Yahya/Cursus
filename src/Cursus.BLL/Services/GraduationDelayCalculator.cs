using System;
using System.Collections.Generic;
using System.Linq;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;

namespace Cursus.BLL.Services;

/// <summary>
/// Semester-by-semester graduation delay simulation based on retake availability,
/// prerequisite constraint satisfaction, and standing credit limits.
/// </summary>
public static class GraduationDelayCalculator
{
    private const int DefaultMaxCreditsPerSemester = 18;

    public sealed record Result(
        int GraduationDelaySemesters,
        int RetakeDelaySemesters,
        int RecoverySemesters,
        int MaxCreditsPerSemester,
        string RetakeSemesterLabel,
        string ProjectedGraduationLabel);

    public static Result Calculate(
        SemesterType currentSemester,
        string? academicYear,
        AcademicStanding standing,
        decimal cgpa,
        int failedCourseId,
        SemesterAvailability failedCourseAvailability,
        List<Course> allCurriculumCourses,
        HashSet<int> completedCourseIds,
        Dictionary<int, List<int>> prerequisites)
    {
        var maxCredits = GetMaxCreditsPerSemester(standing, cgpa);
        var retakeDelay = SemestersUntilOffering(currentSemester, failedCourseAvailability);

        // 1. Baseline Path (student passed the course)
        var baselineCompleted = new HashSet<int>(completedCourseIds) { failedCourseId };
        var baselineSemesters = SimulateGraduation(
            baselineCompleted,
            allCurriculumCourses,
            prerequisites,
            currentSemester,
            academicYear,
            maxCredits);

        // 2. Failure Path (student failed the course)
        var failureCompleted = new HashSet<int>(completedCourseIds);
        failureCompleted.Remove(failedCourseId);
        var failureSemesters = SimulateGraduation(
            failureCompleted,
            allCurriculumCourses,
            prerequisites,
            currentSemester,
            academicYear,
            maxCredits);

        var graduationDelay = Math.Max(0, failureSemesters - baselineSemesters);
        // Recovery is the extra path after accounting for waiting to retake.
        var recoverySemesters = Math.Max(0, graduationDelay - retakeDelay);

        var retakeSemesterLabel = FormatSemesterAfter(
            currentSemester, academicYear, retakeDelay);
        var projectedGraduationLabel = FormatSemesterAfter(
            currentSemester, academicYear, failureSemesters);

        return new Result(
            GraduationDelaySemesters: graduationDelay,
            RetakeDelaySemesters: retakeDelay,
            RecoverySemesters: recoverySemesters,
            MaxCreditsPerSemester: maxCredits,
            RetakeSemesterLabel: retakeSemesterLabel,
            ProjectedGraduationLabel: projectedGraduationLabel);
    }

    private static int SimulateGraduation(
        HashSet<int> startingCompletedCourses,
        List<Course> allCurriculumCourses,
        Dictionary<int, List<int>> prerequisites,
        SemesterType currentSemester,
        string? academicYear,
        int maxCredits)
    {
        var completed = new HashSet<int>(startingCompletedCourses);
        var remaining = allCurriculumCourses
            .Where(c => !completed.Contains(c.Id))
            .ToList();

        if (remaining.Count == 0)
            return 0;

        var semester = currentSemester;
        var year = ParseAcademicYearStart(academicYear);

        int semesterCount = 0;
        int safetyLimit = 60; // Safety limit of 20 years

        // A curriculum with a course above cap is unschedulable by definition.
        if (remaining.Any(c => c.CreditHours > maxCredits))
            return safetyLimit;

        while (remaining.Count > 0 && semesterCount < safetyLimit)
        {
            (semester, year) = AdvanceSemester(semester, year);
            semesterCount++;

            // Find all eligible courses to schedule in this term
            var eligible = remaining
                .Where(c => IsOfferedIn(semester, c.SemesterAvailability) &&
                            c.Prerequisites.All(p => completed.Contains(p.PrerequisiteId)))
                .ToList();

            if (eligible.Count == 0)
            {
                continue;
            }

            // Prioritize:
            // 1. Core courses first
            // 2. Then courses that are prerequisites for remaining courses (out-degree)
            // 3. Then by code to be deterministic
            var prioritized = eligible
                .OrderByDescending(c => c.CourseType == CourseType.Core)
                .ThenByDescending(c => CountDownstreamRemaining(c.Id, remaining, prerequisites))
                .ThenBy(c => c.Code, StringComparer.Ordinal)
                .ToList();

            int currentCredits = 0;
            var scheduledThisSemester = new List<Course>();

            foreach (var course in prioritized)
            {
                if (currentCredits + course.CreditHours <= maxCredits)
                {
                    scheduledThisSemester.Add(course);
                    currentCredits += course.CreditHours;
                }
            }

            if (scheduledThisSemester.Count == 0)
            {
                // Eligible-but-never-packable means current cap/prereq state is stuck.
                return safetyLimit;
            }

            foreach (var course in scheduledThisSemester)
            {
                completed.Add(course.Id);
                remaining.Remove(course);
            }
        }

        return remaining.Count == 0 ? semesterCount : safetyLimit;
    }

    private static int CountDownstreamRemaining(
        int courseId,
        List<Course> remaining,
        Dictionary<int, List<int>> adjacency)
    {
        var visited = new HashSet<int> { courseId };
        var queue = new Queue<int>();
        queue.Enqueue(courseId);

        int count = 0;
        var remainingIds = remaining.Select(r => r.Id).ToHashSet();

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            if (adjacency.TryGetValue(curr, out var dependents))
            {
                foreach (var depId in dependents)
                {
                    if (visited.Add(depId))
                    {
                        if (remainingIds.Contains(depId))
                        {
                            count++;
                        }
                        queue.Enqueue(depId);
                    }
                }
            }
        }
        return count;
    }

    public static int GetMaxCreditsPerSemester(AcademicStanding standing, decimal cgpa) =>
        standing switch
        {
            AcademicStanding.Probation => 12,
            AcademicStanding.Warning => 15,
            _ => cgpa >= 3.0m ? 21 : DefaultMaxCreditsPerSemester
        };

    public static int SemestersUntilOffering(
        SemesterType currentSemester,
        SemesterAvailability availability)
    {
        if (availability == SemesterAvailability.All)
            return 1;

        var semester = currentSemester;
        for (var wait = 1; wait <= 4; wait++)
        {
            semester = NextSemester(semester);
            if (IsOfferedIn(semester, availability))
                return wait;
        }

        return 1;
    }

    private static bool IsOfferedIn(SemesterType semester, SemesterAvailability availability) =>
        availability switch
        {
            SemesterAvailability.All => true,
            SemesterAvailability.FallSpring =>
                semester is SemesterType.Fall or SemesterType.Spring,
            SemesterAvailability.Fall => semester == SemesterType.Fall,
            SemesterAvailability.Spring => semester == SemesterType.Spring,
            _ => true
        };

    private static SemesterType NextSemester(SemesterType semester) =>
        semester switch
        {
            SemesterType.Fall => SemesterType.Spring,
            SemesterType.Spring => SemesterType.Summer,
            _ => SemesterType.Fall
        };

    private static string FormatSemesterAfter(
        SemesterType currentSemester,
        string? academicYear,
        int semestersAhead)
    {
        var yearStart = ParseAcademicYearStart(academicYear);
        var semester = currentSemester;
        var year = currentSemester switch
        {
            SemesterType.Fall => yearStart,
            _ => yearStart + 1
        };

        for (var i = 0; i < semestersAhead; i++)
            (semester, year) = AdvanceSemester(semester, year);

        return $"{semester} {year}";
    }

    private static (SemesterType semester, int year) AdvanceSemester(
        SemesterType semester, int year) =>
        semester switch
        {
            SemesterType.Fall => (SemesterType.Spring, year + 1),
            SemesterType.Spring => (SemesterType.Summer, year),
            _ => (SemesterType.Fall, year)
        };

    private static int ParseAcademicYearStart(string? academicYear)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
            return DateTime.UtcNow.Year;

        var part = academicYear.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return int.TryParse(part, out var year) ? year : DateTime.UtcNow.Year;
    }
}
