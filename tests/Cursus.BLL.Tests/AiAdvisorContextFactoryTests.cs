using Cursus.BLL.Services;
using Cursus.Domain.DTOs;
using Cursus.Domain.Enums;

namespace Cursus.BLL.Tests;

public sealed class AiAdvisorContextFactoryTests
{
    [Fact]
    public void Create_MapsAuditProfileCategoriesAndCourseStatuses()
    {
        var audit = new GraduationAuditDto
        {
            StudentId = "student-1",
            StudentName = "Ahmed Kamal",
            DepartmentName = "Computer Science",
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Spring,
            CurrentStanding = AcademicStanding.Warning,
            TotalCreditsEarned = 45,
            TotalCreditsRequired = 120,
            Cgpa = 2.25m,
            EstimatedGradSemester = "Spring 2028/2029",
            IsOnTrack = true,
            MinGpaForGraduation = 2m,
            Categories =
            [
                new CategoryProgressDto
                {
                    CourseType = CourseType.Core,
                    Label = "Core Courses",
                    Description = "Mandatory courses",
                    RequiredCredits = 72,
                    EarnedCredits = 42,
                    InProgressCredits = 3,
                    Courses =
                    [
                        Course(1, "CS201", "Data Structures", "B+", CourseAuditStatus.Completed),
                        Course(2, "CS301", "Operating Systems", null, CourseAuditStatus.InProgress),
                        Course(3, "MTH102", "Calculus II", "F", CourseAuditStatus.Failed),
                        Course(4, "CS202", "Discrete Mathematics", "D+", CourseAuditStatus.Completed),
                        Course(5, "CS302", "Databases", null, CourseAuditStatus.Available),
                        Course(6, "CS401", "Capstone Project", null, CourseAuditStatus.Locked)
                    ]
                },
                new CategoryProgressDto
                {
                    CourseType = CourseType.FreeElective,
                    Label = "Free Elective",
                    Description = "Approved electives",
                    RequiredCredits = 6,
                    EarnedCredits = 3,
                    InProgressCredits = 0,
                    Courses =
                    [
                        // Duplicate requirement entries must not duplicate prompt courses.
                        Course(1, "CS201", "Data Structures", "B+", CourseAuditStatus.Completed)
                    ]
                }
            ]
        };

        var context = AiAdvisorContextFactory.Create(audit);

        Assert.Equal("Ahmed Kamal", context.DisplayName);
        Assert.Equal(AcademicStanding.Warning, context.AcademicStanding);
        Assert.Equal(2.25m, context.Cgpa);
        Assert.Equal(2m, context.MinGpaForGraduation);
        Assert.Equal(75, context.CreditsRemaining);
        Assert.Equal(38, context.OverallProgressPercentage);
        Assert.False(context.IsOverloadEligible);
        Assert.True(context.IsOnTrack);
        Assert.Equal(2, context.CategoryProgress.Count);
        Assert.Equal(2, context.CompletedCourses.Count);
        Assert.Single(context.InProgressCourses);
        Assert.Equal(2, context.FailedOrLowGradeCourses.Count);
        Assert.Single(context.AvailableCourses);
        Assert.Single(context.LockedCourses);
        Assert.Contains(context.FailedOrLowGradeCourses, course => course.Code == "MTH102");
        Assert.Contains(context.FailedOrLowGradeCourses, course => course.Code == "CS202");
        Assert.Contains(context.AvailableCourses, course => course.Code == "CS302");
        Assert.Contains(context.LockedCourses, course => course.Code == "CS401");
    }

    private static CourseAuditItemDto Course(
        int id,
        string code,
        string name,
        string? grade,
        CourseAuditStatus status) =>
        new()
        {
            CourseId = id,
            Code = code,
            Name = name,
            CreditHours = 3,
            Grade = grade,
            Status = status
        };
}
