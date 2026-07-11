using System;
using System.Collections.Generic;
using System.Linq;
using Cursus.BLL.Services;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Xunit;

namespace Cursus.BLL.Tests;

public sealed class GraduationDelaySimulatorTests
{
    [Theory]
    [InlineData(AcademicStanding.Probation, 2.0, 12)]
    [InlineData(AcademicStanding.Warning, 2.0, 15)]
    [InlineData(AcademicStanding.Good, 2.99, 18)]
    [InlineData(AcademicStanding.Good, 3.0, 21)]
    public void GetMaxCreditsPerSemester_ObeysStandingRules(AcademicStanding standing, decimal cgpa, int expectedMax)
    {
        // Act
        var result = GraduationDelayCalculator.GetMaxCreditsPerSemester(standing, cgpa);

        // Assert
        Assert.Equal(expectedMax, result);
    }

    [Fact]
    public void SemestersUntilOffering_CalculatesCorrectWaitTimes()
    {
        // Fall-only course, currently in Spring. Spring -> Summer -> Fall (wait = 2)
        Assert.Equal(2, GraduationDelayCalculator.SemestersUntilOffering(SemesterType.Spring, SemesterAvailability.Fall));

        // Spring-only course, currently in Fall. Fall -> Spring (wait = 1)
        Assert.Equal(1, GraduationDelayCalculator.SemestersUntilOffering(SemesterType.Fall, SemesterAvailability.Spring));

        // All semesters course, currently in Spring. (wait = 1)
        Assert.Equal(1, GraduationDelayCalculator.SemestersUntilOffering(SemesterType.Spring, SemesterAvailability.All));
    }

    [Fact]
    public void Simulation_ObeysPrerequisiteConstraint()
    {
        // Arrange: Core course A (Id: 1) is a prerequisite for Core course B (Id: 2)
        var courseA = new Course
        {
            Id = 1,
            Code = "CORE101",
            Name = "Intro Course",
            CreditHours = 3,
            CourseType = CourseType.Core,
            SemesterAvailability = SemesterAvailability.All,
            Prerequisites = new List<CoursePrerequisite>()
        };

        var courseB = new Course
        {
            Id = 2,
            Code = "CORE102",
            Name = "Dependent Course",
            CreditHours = 3,
            CourseType = CourseType.Core,
            SemesterAvailability = SemesterAvailability.All,
            Prerequisites = new List<CoursePrerequisite>
            {
                new CoursePrerequisite { CourseId = 2, PrerequisiteId = 1 }
            }
        };

        var curriculum = new List<Course> { courseA, courseB };
        var completed = new HashSet<int>();
        var prerequisites = new Dictionary<int, List<int>>
        {
            [1] = new List<int> { 2 }
        };

        // Act: simulate failing prerequisite course A delays dependent B
        var result = GraduationDelayCalculator.Calculate(
            SemesterType.Spring,
            "2025-2026",
            AcademicStanding.Good,
            3.0m,
            1,
            SemesterAvailability.All,
            curriculum,
            completed,
            prerequisites);

        // Assert: Summer retake of A unlocks B; at most one semester of graduation delay.
        Assert.True(result.GraduationDelaySemesters <= 1);
        Assert.Contains(result.FailureSchedule, t => t.Courses.Any(c => c.IsRetake && c.CourseId == 1));
    }

    [Fact]
    public void Simulation_FallOnlyFailedCourse_SummerRetakeLimitsDelayToOneSemester()
    {
        var failed = new Course
        {
            Id = 10,
            Code = "CSW221",
            Name = "Data Structures",
            CreditHours = 3,
            CourseType = CourseType.Core,
            SemesterAvailability = SemesterAvailability.Fall,
            RecommendedSemester = 3,
            Prerequisites = new List<CoursePrerequisite>()
        };

        var dependentA = new Course
        {
            Id = 11,
            Code = "CSW241",
            Name = "Course A",
            CreditHours = 3,
            CourseType = CourseType.Core,
            SemesterAvailability = SemesterAvailability.All,
            RecommendedSemester = 4,
            Prerequisites = new List<CoursePrerequisite>
            {
                new() { CourseId = 11, PrerequisiteId = 10 }
            }
        };

        var dependentB = new Course
        {
            Id = 12,
            Code = "CSW326",
            Name = "Course B",
            CreditHours = 3,
            CourseType = CourseType.Core,
            SemesterAvailability = SemesterAvailability.All,
            RecommendedSemester = 4,
            Prerequisites = new List<CoursePrerequisite>
            {
                new() { CourseId = 12, PrerequisiteId = 10 }
            }
        };

        var curriculum = new List<Course> { failed, dependentA, dependentB };
        var prerequisites = new Dictionary<int, List<int>>
        {
            [10] = [11, 12]
        };

        var result = GraduationDelayCalculator.Calculate(
            SemesterType.Spring,
            "2025-2026",
            AcademicStanding.Warning,
            2.0m,
            failed.Id,
            failed.SemesterAvailability,
            curriculum,
            new HashSet<int>(),
            prerequisites);

        Assert.Equal(1, result.RetakeDelaySemesters);
        Assert.True(result.GraduationDelaySemesters <= 1);
        Assert.Contains(result.FailureSchedule, t => t.Courses.Any(c => c.IsRetake && c.CourseId == failed.Id));
        Assert.True(result.SemestersAffected >= result.GraduationDelaySemesters);
    }

    [Fact]
    public void Simulation_ChainedDependents_StillSchedulesSummerRetake()
    {
        var gateway = new Course
        {
            Id = 10,
            Code = "CS241",
            Name = "Data Structures",
            CreditHours = 3,
            CourseType = CourseType.Core,
            SemesterAvailability = SemesterAvailability.Fall,
            RecommendedSemester = 3,
            Prerequisites = []
        };

        var mid = new Course
        {
            Id = 11,
            Code = "CS211",
            Name = "OOP",
            CreditHours = 3,
            CourseType = CourseType.Core,
            SemesterAvailability = SemesterAvailability.All,
            RecommendedSemester = 4,
            Prerequisites = [new CoursePrerequisite { CourseId = 11, PrerequisiteId = 10 }]
        };

        var advanced = new Course
        {
            Id = 12,
            Code = "CS311",
            Name = "Algorithms",
            CreditHours = 3,
            CourseType = CourseType.Core,
            SemesterAvailability = SemesterAvailability.All,
            RecommendedSemester = 5,
            Prerequisites = [new CoursePrerequisite { CourseId = 12, PrerequisiteId = 11 }]
        };

        var curriculum = new List<Course> { gateway, mid, advanced };
        var prerequisites = new Dictionary<int, List<int>>
        {
            [10] = [11],
            [11] = [12]
        };

        var result = GraduationDelayCalculator.Calculate(
            SemesterType.Spring,
            "2025-2026",
            AcademicStanding.Good,
            3.0m,
            gateway.Id,
            gateway.SemesterAvailability,
            curriculum,
            new HashSet<int>(),
            prerequisites);

        Assert.Contains(result.FailureSchedule, t => t.Courses.Any(c => c.IsRetake && c.Code == "CS241"));
        Assert.True(result.GraduationDelaySemesters <= 2);
    }

    [Fact]
    public void Simulation_UnschedulableCourseBeyondCreditCap_HitsSafetyLimit()
    {
        // Arrange: 19-credit course cannot fit warning cap (15), making path unschedulable.
        var oversized = new Course
        {
            Id = 50,
            Code = "MEGA500",
            Name = "Oversized Requirement",
            CreditHours = 19,
            CourseType = CourseType.Core,
            SemesterAvailability = SemesterAvailability.All,
            Prerequisites = new List<CoursePrerequisite>()
        };

        // Act
        var result = GraduationDelayCalculator.Calculate(
            SemesterType.Fall,
            "2025-2026",
            AcademicStanding.Warning,
            2.0m,
            oversized.Id,
            oversized.SemesterAvailability,
            new List<Course> { oversized },
            new HashSet<int>(),
            new Dictionary<int, List<int>>());

        // Assert: this safeguards against false "normal" projections.
        Assert.Equal(60, result.GraduationDelaySemesters);
    }
}

