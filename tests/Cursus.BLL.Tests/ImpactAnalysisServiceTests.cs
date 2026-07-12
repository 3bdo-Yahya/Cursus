using Cursus.BLL.Services;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

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

        var service = new ImpactAnalysisService(
            db,
            new AcademicMetricsService(db),
            NullLogger<ImpactAnalysisService>.Instance);
        var result = await service.GetBlockedCoursesAsync(
            PlannerTestData.StudentId,
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
        Assert.NotNull(result.ReplacementCourses);
    }

    [Fact]
    public async Task GetBlockedCoursesAsync_ReturnsEligibleReplacementCourses()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync();
        var failed = PlannerTestData.Course(1, "CS101");
        failed.RecommendedSemester = 1;

        var blocked = PlannerTestData.Course(2, "CS201");
        blocked.RecommendedSemester = 2;

        var replacement = PlannerTestData.Course(3, "IS210", courseType: CourseType.DeptElective);
        replacement.RecommendedSemester = 2;

        var universityReq = PlannerTestData.Course(4, "HU111", courseType: CourseType.UniversityReq);
        universityReq.RecommendedSemester = 2;

        db.Courses.AddRange(failed, blocked, replacement, universityReq);
        db.CoursePrerequisites.Add(new CoursePrerequisite
        {
            CourseId = blocked.Id,
            PrerequisiteId = failed.Id,
            Course = blocked,
            Prerequisite = failed
        });
        await db.SaveChangesAsync();

        var service = new ImpactAnalysisService(
            db,
            new AcademicMetricsService(db),
            NullLogger<ImpactAnalysisService>.Instance);
        var result = await service.GetBlockedCoursesAsync(
            PlannerTestData.StudentId,
            failed.Id,
            PlannerTestData.DepartmentId,
            SemesterType.Fall,
            PlannerTestData.AcademicYear,
            AcademicStanding.Good,
            3.0m);

        Assert.NotNull(result);
        Assert.NotNull(result!.ReplacementCourses);
        Assert.Contains(result.ReplacementCourses, c => c.Code == "IS210");
        Assert.DoesNotContain(result.ReplacementCourses, c => c.Code == "CS201");
        Assert.DoesNotContain(result.ReplacementCourses, c => c.Code == "HU111");
    }
}
