using Cursus.BLL.Services;
using Cursus.DAL.Repositories;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;

namespace Cursus.BLL.Tests;

public sealed class ImpactAnalysisServiceTests
{
    [Fact]
    public async Task GetBlockedCoursesAsync_WorksForPlannedOnlyCourse_WithoutStudentRecord()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync();
        var prereq = PlannerTestData.Course(1, "PRQ");
        var dependent = PlannerTestData.Course(2, "DEP");
        db.Courses.AddRange(prereq, dependent);
        db.CoursePrerequisites.Add(new CoursePrerequisite
        {
            CourseId = dependent.Id,
            PrerequisiteId = prereq.Id,
            Course = dependent,
            Prerequisite = prereq
        });
        await db.SaveChangesAsync();

        var service = new ImpactAnalysisService(new GenericRepository<Course>(db));
        var result = await service.GetBlockedCoursesAsync(
            prereq.Id,
            PlannerTestData.DepartmentId,
            SemesterType.Fall,
            PlannerTestData.AcademicYear,
            AcademicStanding.Good,
            3.0m);

        Assert.NotNull(result);
        Assert.Equal("PRQ", result!.FailedCourseCode);
        Assert.Single(result.BlockedCourses);
        Assert.Equal("DEP", result.BlockedCourses.First().Code);
    }
}
