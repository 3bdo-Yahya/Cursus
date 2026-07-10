using Cursus.BLL.Services;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Tests;

public sealed class PlannerServiceTests
{
    private const int CreditLimit = 18;

    [Fact]
    public async Task AddPlannedCourseAsync_AddsCourse_ReturnsTrue()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync();
        var course = PlannerTestData.Course(1, "CS101");
        await PlannerTestData.AddCoursesAsync(db, course);

        var service = new PlannerService(db);
        var added = await service.AddPlannedCourseAsync(
            PlannerTestData.StudentId,
            course.Id,
            PlannerTestData.AcademicYear,
            SemesterType.Fall);

        Assert.True(added);
        var plan = await service.GetPlanAsync(
            PlannerTestData.StudentId,
            PlannerTestData.AcademicYear,
            SemesterType.Fall);
        Assert.Single(plan);
        Assert.Equal("CS101", plan[0].Code);
    }

    [Fact]
    public async Task AddPlannedCourseAsync_DuplicateInSameTerm_ReturnsFalse()
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
        var course = PlannerTestData.Course(1, "CS101");
        await PlannerTestData.AddCoursesAsync(db, course);

        var service = new PlannerService(db);
        var added = await service.AddPlannedCourseAsync(
            PlannerTestData.StudentId,
            course.Id,
            PlannerTestData.AcademicYear,
            SemesterType.Fall);

        Assert.False(added);
    }

    [Fact]
    public async Task RemovePlannedCourseAsync_RemovesCourse_ReturnsTrue()
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
        var course = PlannerTestData.Course(1, "CS101");
        await PlannerTestData.AddCoursesAsync(db, course);

        var service = new PlannerService(db);
        var removed = await service.RemovePlannedCourseAsync(
            PlannerTestData.StudentId,
            course.Id,
            PlannerTestData.AcademicYear,
            SemesterType.Fall);

        Assert.True(removed);
        var plan = await service.GetPlanAsync(
            PlannerTestData.StudentId,
            PlannerTestData.AcademicYear,
            SemesterType.Fall);
        Assert.Empty(plan);
    }

    [Fact]
    public async Task GetAllPlansAsync_ReturnsPlansAcrossTerms()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync(plannedCourses:
        [
            new PlannedCourse { CourseId = 1, AcademicYear = PlannerTestData.AcademicYear, Semester = SemesterType.Fall },
            new PlannedCourse { CourseId = 2, AcademicYear = "2025-2026", Semester = SemesterType.Spring }
        ]);
        await PlannerTestData.AddCoursesAsync(
            db,
            PlannerTestData.Course(1, "CS101"),
            PlannerTestData.Course(2, "CS102"));

        var service = new PlannerService(db);
        var allPlans = await service.GetAllPlansAsync(PlannerTestData.StudentId);

        Assert.Equal(2, allPlans.Count);
    }

    [Fact]
    public async Task GetPlanAsync_PrunesSupersededPlan_WhenStudentCourseExists()
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
        var course = PlannerTestData.Course(1, "CS101");
        db.Courses.Add(course);
        db.StudentCourses.Add(new StudentCourse
        {
            StudentId = PlannerTestData.StudentId,
            CourseId = course.Id,
            Status = StudentCourseStatus.Failed,
            Grade = "F",
            Semester = SemesterType.Spring,
            AcademicYear = "2023-2024",
            Course = course
        });
        await db.SaveChangesAsync();

        var service = new PlannerService(db);
        var plan = await service.GetPlanAsync(
            PlannerTestData.StudentId,
            PlannerTestData.AcademicYear,
            SemesterType.Fall);

        Assert.Empty(plan);
        Assert.False(await db.PlannedCourses.AnyAsync());
    }

    [Fact]
    public async Task AddPlannedCourseAsync_ReturnsFalse_WhenCourseIsSuperseded()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync();
        var course = PlannerTestData.Course(1, "CS101");
        db.Courses.Add(course);
        db.StudentCourses.Add(new StudentCourse
        {
            StudentId = PlannerTestData.StudentId,
            CourseId = course.Id,
            Status = StudentCourseStatus.InProgress,
            Semester = SemesterType.Fall,
            AcademicYear = PlannerTestData.AcademicYear,
            Course = course
        });
        await db.SaveChangesAsync();

        var service = new PlannerService(db);
        var added = await service.AddPlannedCourseAsync(
            PlannerTestData.StudentId,
            course.Id,
            PlannerTestData.AcademicYear,
            SemesterType.Spring);

        Assert.False(added);
    }

    [Fact]
    public async Task GetTermCapacityAsync_CountsForcedAndPlannedCredits()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync(
            forcedCreditsOnCurrentTerm: 6,
            plannedCourses:
            [
                new PlannedCourse
                {
                    CourseId = 2,
                    AcademicYear = PlannerTestData.AcademicYear,
                    Semester = SemesterType.Fall
                }
            ]);

        await PlannerTestData.AddCoursesAsync(
            db,
            PlannerTestData.Course(2, "PLAN", 9));

        var service = new PlannerService(db);
        var capacity = await service.GetTermCapacityAsync(
            PlannerTestData.StudentId,
            PlannerTestData.AcademicYear,
            SemesterType.Fall,
            CreditLimit);

        Assert.Equal(6, capacity.ForcedInProgressCredits);
        Assert.Equal(9, capacity.PlannedCredits);
        Assert.Equal(3, capacity.RemainingRoom);
    }

    [Fact]
    public async Task GetPlanningTermsAsync_PrimaryTerm_IsCurrentTerm_WhenForcedCreditsBelowLimit()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync(forcedCreditsOnCurrentTerm: 6);
        var service = new PlannerService(db);

        var terms = await service.GetPlanningTermsAsync(PlannerTestData.StudentId, CreditLimit);
        var primary = terms.Single(t => t.IsPrimary);

        Assert.Equal(PlannerTestData.AcademicYear, primary.AcademicYear);
        Assert.Equal(SemesterType.Fall, primary.Semester);
    }

    [Fact]
    public async Task GetPlanningTermsAsync_PrimaryTerm_Advances_WhenCurrentTermIsAtCapacity()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync(forcedCreditsOnCurrentTerm: 18);
        var service = new PlannerService(db);

        var terms = await service.GetPlanningTermsAsync(PlannerTestData.StudentId, CreditLimit);
        var primary = terms.Single(t => t.IsPrimary);

        Assert.Equal(PlannerTestData.AcademicYear, primary.AcademicYear);
        Assert.Equal(SemesterType.Spring, primary.Semester);
    }
}
