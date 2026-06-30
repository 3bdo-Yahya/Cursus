using Cursus.BLL.Services;
using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Tests;

public sealed class AcademicMetricsServiceTests
{
    private static readonly Dictionary<string, decimal> DefaultScale = new()
    {
        ["A"] = 4.0m,
        ["B"] = 3.0m,
        ["C"] = 2.0m,
        ["D"] = 1.0m,
        ["F"] = 0.0m
    };

    private static Course Course(int id, int credits = 3) => new()
    {
        Id = id,
        Code = $"C{id}",
        Name = $"Course {id}",
        CreditHours = credits,
        CourseType = CourseType.Core,
        SemesterAvailability = SemesterAvailability.All,
        PassingGradeThreshold = "D",
        DepartmentId = 1,
        IsActive = true
    };

    private static StudentCourse Record(
        int courseId,
        StudentCourseStatus status,
        string? grade,
        SemesterType semester = SemesterType.Fall,
        string academicYear = "2024-2025",
        int creditHours = 3) => new()
    {
        StudentId = "s1",
        CourseId = courseId,
        Status = status,
        Grade = grade,
        Semester = semester,
        AcademicYear = academicYear,
        Course = Course(courseId, creditHours)
    };

    [Fact]
    public void ResolveBestAttempts_PrefersCompletedOverInProgressAndFailed()
    {
        var service = CreateService(out _);

        var attempts = new[]
        {
            Record(1, StudentCourseStatus.Failed, "F"),
            Record(1, StudentCourseStatus.InProgress, null, SemesterType.Spring),
            Record(1, StudentCourseStatus.Completed, "B", SemesterType.Summer)
        };

        var best = service.ResolveBestAttempts(attempts);

        Assert.Single(best);
        Assert.Equal(StudentCourseStatus.Completed, best[0].Status);
        Assert.Equal("B", best[0].Grade);
    }

    [Fact]
    public void ResolveBestAttempts_TieBreaksByHighestGrade()
    {
        var service = CreateService(out _);

        var attempts = new[]
        {
            Record(1, StudentCourseStatus.Completed, "B"),
            Record(1, StudentCourseStatus.Completed, "A", SemesterType.Spring)
        };

        var best = service.ResolveBestAttempts(attempts);

        Assert.Single(best);
        Assert.Equal("A", best[0].Grade);
    }

    [Fact]
    public void CalculateCgpa_ComputesWeightedAverage()
    {
        var service = CreateService(out _);

        var bestAttempts = new[]
        {
            Record(1, StudentCourseStatus.Completed, "A"),
            Record(2, StudentCourseStatus.Completed, "B")
        };

        var cgpa = service.CalculateCgpa(bestAttempts, DefaultScale);

        Assert.Equal(3.50m, cgpa);
    }

    [Fact]
    public void CalculateCgpa_ReturnsZeroWhenNoGradedCourses()
    {
        var service = CreateService(out _);

        var cgpa = service.CalculateCgpa(Array.Empty<StudentCourse>(), DefaultScale);

        Assert.Equal(0m, cgpa);
    }

    [Fact]
    public void GetPreviousTerm_ReturnsChronologicalPredecessor_NotLaterSemesterInSameYear()
    {
        var service = CreateService(out _);

        var terms = new List<TermGpaDto>
        {
            new() { AcademicYear = "2024-2025", Semester = SemesterType.Fall, SemesterGpa = 3.0m, CumulativeGpa = 3.0m },
            new() { AcademicYear = "2024-2025", Semester = SemesterType.Spring, SemesterGpa = 3.5m, CumulativeGpa = 3.25m },
            new() { AcademicYear = "2024-2025", Semester = SemesterType.Summer, SemesterGpa = 4.0m, CumulativeGpa = 3.4m }
        };

        var previous = service.GetPreviousTerm(terms, "2024-2025", SemesterType.Spring);

        Assert.NotNull(previous);
        Assert.Equal(SemesterType.Fall, previous!.Semester);
    }

    [Fact]
    public void GetPreviousTerm_ReturnsNullForFirstTerm()
    {
        var service = CreateService(out _);

        var terms = new List<TermGpaDto>
        {
            new() { AcademicYear = "2024-2025", Semester = SemesterType.Fall, SemesterGpa = 3.0m, CumulativeGpa = 3.0m }
        };

        var previous = service.GetPreviousTerm(terms, "2024-2025", SemesterType.Fall);

        Assert.Null(previous);
    }

    [Fact]
    public async Task CanEnrollInCourseAsync_BlocksInProgressEnrollment()
    {
        var service = CreateService(out var db);

        db.StudentCourses.Add(new StudentCourse
        {
            StudentId = "s1",
            CourseId = 10,
            Status = StudentCourseStatus.InProgress,
            Semester = SemesterType.Fall,
            AcademicYear = "2024-2025"
        });
        await db.SaveChangesAsync();

        var (canEnroll, reason) = await service.CanEnrollInCourseAsync("s1", 10);

        Assert.False(canEnroll);
        Assert.Equal("Student is already enrolled in this course.", reason);
    }

    [Fact]
    public async Task CanEnrollInCourseAsync_BlocksPassedCourse()
    {
        var service = CreateService(out var db);

        db.StudentCourses.Add(new StudentCourse
        {
            StudentId = "s1",
            CourseId = 10,
            Status = StudentCourseStatus.Completed,
            Grade = "B",
            Semester = SemesterType.Fall,
            AcademicYear = "2024-2025"
        });
        await db.SaveChangesAsync();

        var (canEnroll, reason) = await service.CanEnrollInCourseAsync("s1", 10);

        Assert.False(canEnroll);
        Assert.Equal("Student has already passed this course.", reason);
    }

    [Fact]
    public async Task CanEnrollInCourseAsync_AllowsRetakeForLowGrade()
    {
        var service = CreateService(out var db);

        db.StudentCourses.Add(new StudentCourse
        {
            StudentId = "s1",
            CourseId = 10,
            Status = StudentCourseStatus.Completed,
            Grade = "D+",
            Semester = SemesterType.Fall,
            AcademicYear = "2024-2025"
        });
        await db.SaveChangesAsync();

        var (canEnroll, reason) = await service.CanEnrollInCourseAsync("s1", 10);

        Assert.True(canEnroll);
        Assert.Null(reason);
    }

    private static AcademicMetricsService CreateService(out ApplicationDbContext db)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        db = new ApplicationDbContext(options);
        return new AcademicMetricsService(db);
    }
}
