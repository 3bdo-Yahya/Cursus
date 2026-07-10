using System;
using System.Collections.Generic;
using System.Linq;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;

namespace Cursus.BLL.Services;

/// <summary>
/// Term-anchored, summer-aware graduation delay simulation that emits
/// baseline vs failure schedules for advisor-grade recovery planning.
/// </summary>
public static class GraduationDelayCalculator
{
    private const int DefaultMaxCreditsPerSemester = 18;
    private const int SafetyLimit = 60;

    public sealed record ScheduledCourseEntry(
        int CourseId,
        string Code,
        string Name,
        int CreditHours,
        bool IsRetake,
        bool IsNewlyUnlocked);

    public sealed record ScheduledTermEntry(
        SemesterType Semester,
        int CalendarYear,
        string Label,
        IReadOnlyList<ScheduledCourseEntry> Courses);

    public sealed record SimulationResult(
        int TermCount,
        string GraduationLabel,
        IReadOnlyList<ScheduledTermEntry> Schedule);

    public sealed record Result(
        int GraduationDelaySemesters,
        int RetakeDelaySemesters,
        int RecoverySemesters,
        int SemestersAffected,
        int MaxCreditsPerSemester,
        string RetakeSemesterLabel,
        string OriginalGraduationLabel,
        string ProjectedGraduationLabel,
        IReadOnlyList<ScheduledTermEntry> BaselineSchedule,
        IReadOnlyList<ScheduledTermEntry> FailureSchedule);

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

        var baselineCompleted = new HashSet<int>(completedCourseIds) { failedCourseId };
        var baseline = SimulatePath(
            isFailurePath: false,
            failedCourseId: null,
            baselineCompleted,
            allCurriculumCourses,
            prerequisites,
            currentSemester,
            academicYear,
            maxCredits);

        var failureCompleted = new HashSet<int>(completedCourseIds);
        failureCompleted.Remove(failedCourseId);
        var failure = SimulatePath(
            isFailurePath: true,
            failedCourseId: failedCourseId,
            failureCompleted,
            allCurriculumCourses,
            prerequisites,
            currentSemester,
            academicYear,
            maxCredits);

        var graduationDelay = Math.Max(0, failure.TermCount - baseline.TermCount);
        var retakeDelay = FindRetakeDelaySemesters(failure.Schedule, failedCourseId);
        var recoverySemesters = Math.Max(0, graduationDelay - retakeDelay);
        var semestersAffected = CountSemestersAffected(baseline.Schedule, failure.Schedule);

        var retakeSemesterLabel = FindRetakeLabel(failure.Schedule, failedCourseId)
            ?? FormatSemesterAfter(currentSemester, academicYear, retakeDelay);

        return new Result(
            GraduationDelaySemesters: graduationDelay,
            RetakeDelaySemesters: retakeDelay,
            RecoverySemesters: recoverySemesters,
            SemestersAffected: semestersAffected,
            MaxCreditsPerSemester: maxCredits,
            RetakeSemesterLabel: retakeSemesterLabel,
            OriginalGraduationLabel: baseline.GraduationLabel,
            ProjectedGraduationLabel: failure.GraduationLabel,
            BaselineSchedule: baseline.Schedule,
            FailureSchedule: failure.Schedule);
    }

    private static SimulationResult SimulatePath(
        bool isFailurePath,
        int? failedCourseId,
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
        {
            return new SimulationResult(0, FormatCurrentLabel(currentSemester, academicYear), []);
        }

        if (remaining.Any(c => c.CreditHours > maxCredits))
        {
            var stuckLabel = FormatSemesterAfter(currentSemester, academicYear, SafetyLimit);
            return new SimulationResult(SafetyLimit, stuckLabel, []);
        }

        var semester = currentSemester;
        var year = AcademicYearHelper.ParseCalendarYearStart(academicYear);
        var schedule = new List<ScheduledTermEntry>();
        var retakeCompleted = !isFailurePath || failedCourseId is null || completed.Contains(failedCourseId.Value);
        var unlockedAfterRetake = new HashSet<int>();
        var semesterCount = 0;

        while (remaining.Count > 0 && semesterCount < SafetyLimit)
        {
            (semester, year) = AdvanceSemester(semester, year);
            semesterCount++;

            var termCourses = new List<ScheduledCourseEntry>();

            if (isFailurePath && !retakeCompleted && failedCourseId.HasValue && semester == SemesterType.Summer)
            {
                var failed = allCurriculumCourses.First(c => c.Id == failedCourseId.Value);
                termCourses.Add(new ScheduledCourseEntry(
                    failed.Id,
                    failed.Code,
                    failed.Name,
                    failed.CreditHours,
                    IsRetake: true,
                    IsNewlyUnlocked: false));

                completed.Add(failed.Id);
                remaining.RemoveAll(c => c.Id == failed.Id);
                retakeCompleted = true;
            }

            var eligible = remaining
                .Where(c => IsOfferedIn(semester, c.SemesterAvailability) &&
                            c.Prerequisites.All(p => completed.Contains(p.PrerequisiteId)))
                .ToList();

            var prioritized = eligible
                .OrderBy(c => SemesterMath.ResolveRecommendedSemester(c))
                .ThenByDescending(c => c.CourseType == CourseType.Core)
                .ThenByDescending(c => CountDownstreamRemaining(c.Id, remaining, prerequisites))
                .ThenBy(c => c.Code, StringComparer.Ordinal)
                .ToList();

            var currentCredits = termCourses.Sum(c => c.CreditHours);

            foreach (var course in prioritized)
            {
                if (currentCredits + course.CreditHours > maxCredits)
                    continue;

                var isNewlyUnlocked = isFailurePath
                    && retakeCompleted
                    && unlockedAfterRetake.Add(course.Id)
                    && course.Prerequisites.Any(p => p.PrerequisiteId == failedCourseId);

                termCourses.Add(new ScheduledCourseEntry(
                    course.Id,
                    course.Code,
                    course.Name,
                    course.CreditHours,
                    IsRetake: false,
                    IsNewlyUnlocked: isNewlyUnlocked));

                completed.Add(course.Id);
                remaining.Remove(course);
                currentCredits += course.CreditHours;
            }

            if (eligible.Count > 0 && termCourses.Count == 0 && !retakeCompleted)
                return new SimulationResult(SafetyLimit, FormatLabel(semester, year), schedule);

            if (termCourses.Count > 0)
            {
                schedule.Add(new ScheduledTermEntry(
                    semester,
                    year,
                    FormatLabel(semester, year),
                    termCourses));
            }
        }

        var graduationLabel = schedule.Count > 0
            ? schedule[^1].Label
            : FormatCurrentLabel(currentSemester, academicYear);

        return new SimulationResult(
            remaining.Count == 0 ? semesterCount : SafetyLimit,
            graduationLabel,
            schedule);
    }

    public static string? FindTermLabelForCourse(
        IReadOnlyList<ScheduledTermEntry> schedule,
        int courseId)
    {
        foreach (var term in schedule)
        {
            if (term.Courses.Any(c => c.CourseId == courseId))
                return term.Label;
        }

        return null;
    }

    private static int FindRetakeDelaySemesters(IReadOnlyList<ScheduledTermEntry> failureSchedule, int failedCourseId)
    {
        for (var i = 0; i < failureSchedule.Count; i++)
        {
            if (failureSchedule[i].Courses.Any(c => c.CourseId == failedCourseId && c.IsRetake))
                return i + 1;
        }

        return 1;
    }

    private static string? FindRetakeLabel(IReadOnlyList<ScheduledTermEntry> failureSchedule, int failedCourseId)
    {
        foreach (var term in failureSchedule)
        {
            if (term.Courses.Any(c => c.CourseId == failedCourseId && c.IsRetake))
                return term.Label;
        }

        return null;
    }

    private static int CountSemestersAffected(
        IReadOnlyList<ScheduledTermEntry> baseline,
        IReadOnlyList<ScheduledTermEntry> failure)
    {
        var maxLen = Math.Max(baseline.Count, failure.Count);
        var affected = 0;

        for (var i = 0; i < maxLen; i++)
        {
            var baselineIds = i < baseline.Count
                ? baseline[i].Courses.Select(c => c.CourseId).OrderBy(x => x).ToList()
                : [];
            var failureIds = i < failure.Count
                ? failure[i].Courses.Select(c => c.CourseId).OrderBy(x => x).ToList()
                : [];

            if (!baselineIds.SequenceEqual(failureIds))
                affected++;
        }

        return affected;
    }

    private static int CountDownstreamRemaining(
        int courseId,
        List<Course> remaining,
        Dictionary<int, List<int>> adjacency)
    {
        var visited = new HashSet<int> { courseId };
        var queue = new Queue<int>();
        queue.Enqueue(courseId);

        var count = 0;
        var remainingIds = remaining.Select(r => r.Id).ToHashSet();

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            if (!adjacency.TryGetValue(curr, out var dependents))
                continue;

            foreach (var depId in dependents)
            {
                if (!visited.Add(depId))
                    continue;

                if (remainingIds.Contains(depId))
                    count++;

                queue.Enqueue(depId);
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
        var yearStart = AcademicYearHelper.ParseCalendarYearStart(academicYear);
        var semester = currentSemester;
        var year = currentSemester switch
        {
            SemesterType.Fall => yearStart,
            _ => yearStart + 1
        };

        for (var i = 0; i < semestersAhead; i++)
            (semester, year) = AdvanceSemester(semester, year);

        return FormatLabel(semester, year);
    }

    private static string FormatCurrentLabel(SemesterType semester, string? academicYear)
    {
        var yearStart = AcademicYearHelper.ParseCalendarYearStart(academicYear);
        var year = semester switch
        {
            SemesterType.Fall => yearStart,
            _ => yearStart + 1
        };

        return FormatLabel(semester, year);
    }

    private static string FormatLabel(SemesterType semester, int year) => $"{semester} {year}";

    private static (SemesterType semester, int year) AdvanceSemester(
        SemesterType semester, int year) =>
        semester switch
        {
            SemesterType.Fall => (SemesterType.Spring, year + 1),
            SemesterType.Spring => (SemesterType.Summer, year),
            _ => (SemesterType.Fall, year)
        };

}

