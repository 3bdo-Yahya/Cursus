using Cursus.BLL.Services;
using Cursus.DAL.Database;
using Cursus.DAL.Repositories;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;

namespace Cursus.BLL.Tests;

public sealed class CourseMapServiceTests
{
    [Fact]
    public async Task GetCourseGraphForStudentAsync_MarksPrimaryTermPlannedCourse_AsIsPlanned()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync(plannedCourses:
        [
            new PlannedCourse
            {
                CourseId = 1,
                AcademicYear = PlannerTestData.AcademicYear,
                Semester = SemesterType.Fall
            }
        ]);

        await PlannerTestData.AddCoursesAsync(db, PlannerTestData.Course(1, "CS101"));

        var service = CreateService(db);
        var graph = await service.GetCourseGraphForStudentAsync(PlannerTestData.StudentId, PlannerTestData.DepartmentId);

        var node = graph.Nodes.Single(n => n.Id == 1);
        Assert.True(node.IsPlanned);
        Assert.Null(node.Status);
    }

    [Fact]
    public async Task GetCourseGraphForStudentAsync_DoesNotMarkForcedInProgress_AsIsPlanned()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync(
            forcedCreditsOnCurrentTerm: 3,
            plannedCourses:
            [
                new PlannedCourse
                {
                    CourseId = 100,
                    AcademicYear = PlannerTestData.AcademicYear,
                    Semester = SemesterType.Fall
                }
            ]);

        var service = CreateService(db);
        var graph = await service.GetCourseGraphForStudentAsync(PlannerTestData.StudentId, PlannerTestData.DepartmentId);

        var forcedNode = graph.Nodes.Single(n => n.Id == 100);
        Assert.False(forcedNode.IsPlanned);
        Assert.Equal(StudentCourseStatus.InProgress, forcedNode.Status);
    }

    [Fact]
    public async Task GetCourseGraphForStudentAsync_DoesNotMarkNonPrimaryTermPlanned_AsIsPlanned()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync(plannedCourses:
        [
            new PlannedCourse
            {
                CourseId = 1,
                AcademicYear = "2025-2026",
                Semester = SemesterType.Spring
            }
        ]);

        await PlannerTestData.AddCoursesAsync(db, PlannerTestData.Course(1, "CS101"));

        var service = CreateService(db);
        var graph = await service.GetCourseGraphForStudentAsync(PlannerTestData.StudentId, PlannerTestData.DepartmentId);

        var node = graph.Nodes.Single(n => n.Id == 1);
        Assert.False(node.IsPlanned);
    }

    private static CourseMapService CreateService(ApplicationDbContext db)
    {
        var courseRepository = new GenericRepository<Course>(db);
        var studentCourseRepository = new GenericRepository<StudentCourse>(db);
        var plannerService = new PlannerService(db);
        var academicMetricsService = new AcademicMetricsService(db);
        return new CourseMapService(
            db,
            courseRepository,
            studentCourseRepository,
            plannerService,
            academicMetricsService);
    }
}
