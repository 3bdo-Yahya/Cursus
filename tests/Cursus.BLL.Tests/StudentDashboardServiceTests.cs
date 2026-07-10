using Cursus.BLL.Services;
using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;

namespace Cursus.BLL.Tests;

public sealed class StudentDashboardServiceTests
{
    [Fact]
    public async Task GetDashboardDataAsync_SemestersCompleted_ExcludesSummerAndUngradedTerms()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync();
        await SeedAcademicHistoryAsync(db);

        var service = CreateService(db);
        var dashboard = await service.GetDashboardDataAsync(PlannerTestData.StudentId);

        Assert.NotNull(dashboard);
        Assert.Equal(2, dashboard!.SemestersCompleted);
    }

    [Fact]
    public async Task GetDashboardDataAsync_SemestersCompleted_IsClampedToTotalSemesters()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync();
        var department = await db.Departments.FindAsync(PlannerTestData.DepartmentId);
        department!.TotalCreditsRequired = 24;
        await db.SaveChangesAsync();

        await SeedManyGradedTermsAsync(db, gradedFallSpringTerms: 10);

        var service = CreateService(db);
        var dashboard = await service.GetDashboardDataAsync(PlannerTestData.StudentId);

        Assert.NotNull(dashboard);
        Assert.True(dashboard!.SemestersCompleted <= dashboard.TotalSemesters);
        Assert.Equal(dashboard.TotalSemesters, dashboard.SemestersCompleted);
    }

    private static StudentDashboardService CreateService(ApplicationDbContext db)
    {
        var academicMetricsService = new AcademicMetricsService(db);
        return new StudentDashboardService(db, academicMetricsService);
    }

    private static async Task SeedAcademicHistoryAsync(ApplicationDbContext db)
    {
        var courses = new[]
        {
            PlannerTestData.Course(1, "H1"),
            PlannerTestData.Course(2, "H2"),
            PlannerTestData.Course(3, "H3"),
            PlannerTestData.Course(4, "H4")
        };
        await PlannerTestData.AddCoursesAsync(db, courses);

        db.StudentCourses.AddRange(
            Graded(1, SemesterType.Fall, "2022-2023", "B"),
            Graded(2, SemesterType.Spring, "2022-2023", "B"),
            Graded(3, SemesterType.Summer, "2022-2023", "A"),
            new StudentCourse
            {
                StudentId = PlannerTestData.StudentId,
                CourseId = 4,
                Status = StudentCourseStatus.InProgress,
                Semester = SemesterType.Fall,
                AcademicYear = PlannerTestData.AcademicYear,
                Course = courses[3]
            });

        await db.SaveChangesAsync();
    }

    private static async Task SeedManyGradedTermsAsync(ApplicationDbContext db, int gradedFallSpringTerms)
    {
        var courseId = 1;
        for (var i = 0; i < gradedFallSpringTerms; i++)
        {
            var yearStart = 2010 + i;
            var academicYear = $"{yearStart}-{yearStart + 1}";
            var fallCourse = PlannerTestData.Course(courseId++, $"F{i}");
            var springCourse = PlannerTestData.Course(courseId++, $"S{i}");
            await PlannerTestData.AddCoursesAsync(db, fallCourse, springCourse);
            db.StudentCourses.Add(Graded(fallCourse.Id, SemesterType.Fall, academicYear, "B", fallCourse));
            db.StudentCourses.Add(Graded(springCourse.Id, SemesterType.Spring, academicYear, "B", springCourse));
        }

        await db.SaveChangesAsync();
    }

    private static StudentCourse Graded(
        int courseId,
        SemesterType semester,
        string academicYear,
        string grade,
        Course? course = null) => new()
    {
        StudentId = PlannerTestData.StudentId,
        CourseId = courseId,
        Status = StudentCourseStatus.Completed,
        Grade = grade,
        Semester = semester,
        AcademicYear = academicYear,
        Course = course
    };
}
