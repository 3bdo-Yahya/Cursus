using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Tests;

public static class PlannerTestData
{
    public const string StudentId = "student-1";
    public const int UniversityId = 1;
    public const int DepartmentId = 1;
    public const string AcademicYear = "2024-2025";

    public static ApplicationDbContext CreateDb(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    public static async Task<ApplicationDbContext> SeedPlannerStudentAsync(
        SemesterType currentSemester = SemesterType.Fall,
        int forcedCreditsOnCurrentTerm = 0,
        IReadOnlyList<PlannedCourse>? plannedCourses = null)
    {
        var db = CreateDb();
        var university = new University
        {
            Id = UniversityId,
            Name = "Test University"
        };

        var department = new Department
        {
            Id = DepartmentId,
            Name = "Computer Science",
            UniversityId = UniversityId,
            TotalCreditsRequired = 132,
            MinGpaForGraduation = 2.0m,
            IsActive = true
        };

        var student = new AppUser
        {
            Id = StudentId,
            UserName = "student@test.edu",
            Email = "student@test.edu",
            UniversityId = UniversityId,
            DepartmentId = DepartmentId,
            AcademicYear = AcademicYear,
            CurrentSemester = currentSemester,
            CurrentStanding = AcademicStanding.Good,
            EnrollmentDate = new DateTime(2022, 9, 1)
        };

        db.Universities.Add(university);
        db.Departments.Add(department);
        db.Users.Add(student);

        if (forcedCreditsOnCurrentTerm > 0)
        {
            var forcedCourse = Course(100, "FORCED", forcedCreditsOnCurrentTerm);
            db.Courses.Add(forcedCourse);
            db.StudentCourses.Add(new StudentCourse
            {
                StudentId = StudentId,
                CourseId = forcedCourse.Id,
                Status = StudentCourseStatus.InProgress,
                Semester = currentSemester,
                AcademicYear = AcademicYear,
                Course = forcedCourse
            });
        }

        if (plannedCourses is not null)
        {
            foreach (var planned in plannedCourses)
            {
                planned.StudentId = StudentId;
                db.PlannedCourses.Add(planned);
            }
        }

        await db.SaveChangesAsync();
        return db;
    }

    public static Course Course(
        int id,
        string code,
        int credits = 3,
        CourseType courseType = CourseType.Core,
        SemesterAvailability availability = SemesterAvailability.All,
        int departmentId = DepartmentId) => new()
    {
        Id = id,
        Code = code,
        Name = $"Course {code}",
        CreditHours = credits,
        CourseType = courseType,
        SemesterAvailability = availability,
        PassingGradeThreshold = "D",
        DepartmentId = departmentId,
        IsActive = true
    };

    public static async Task AddCoursesAsync(ApplicationDbContext db, params Course[] courses)
    {
        db.Courses.AddRange(courses);
        await db.SaveChangesAsync();
    }
}
