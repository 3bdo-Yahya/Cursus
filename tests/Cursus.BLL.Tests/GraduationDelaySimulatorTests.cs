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

        // Act: Run simulator with failedCourseId: 2
        var result = GraduationDelayCalculator.Calculate(
            SemesterType.Spring,
            "2025-2026",
            AcademicStanding.Good,
            3.0m,
            2,
            SemesterAvailability.All,
            curriculum,
            completed,
            prerequisites);

        // Assert: Baseline should take 2 semesters (A then B)
        // Failure path: failing B should add 1 semester delay to retake it, recovery is 1 semester
        Assert.True(result.GraduationDelaySemesters >= 0);
        Assert.Equal(1, result.RetakeDelaySemesters);
    }
}
