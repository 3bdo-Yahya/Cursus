using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.PL.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Cursus.PL.Tests;

public sealed class StartupSeederTests
{
    [Fact]
    public async Task SeedSampleCatalogAsync_NormalizesNamesAndPrunesStaleCatalogRows()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        await using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var legacyUniversity = new University { Name = "South Valley National University" };
            db.Universities.Add(legacyUniversity);
            await db.SaveChangesAsync();

            var department = new Department
            {
                Name = "Computer Science",
                UniversityId = legacyUniversity.Id,
                TotalCreditsRequired = 1,
                MinGpaForGraduation = 1.00m,
                IsActive = false
            };
            db.Departments.Add(department);
            await db.SaveChangesAsync();

            var stalePrerequisiteCourse = new Course
            {
                Code = "OLD998",
                Name = "Stale Prerequisite Course",
                CreditHours = 3,
                CourseType = CourseType.UniversityReq,
                SemesterAvailability = SemesterAvailability.All,
                PassingGradeThreshold = "D",
                DepartmentId = department.Id,
                IsActive = true
            };

            var staleCourse = new Course
            {
                Code = "OLD999",
                Name = "Stale Seed Course",
                CreditHours = 3,
                CourseType = CourseType.UniversityReq,
                SemesterAvailability = SemesterAvailability.All,
                PassingGradeThreshold = "D",
                DepartmentId = department.Id,
                IsActive = true
            };

            db.Courses.AddRange(stalePrerequisiteCourse, staleCourse);
            await db.SaveChangesAsync();

            db.CoursePrerequisites.Add(new CoursePrerequisite
            {
                CourseId = staleCourse.Id,
                PrerequisiteId = stalePrerequisiteCourse.Id
            });
            await db.SaveChangesAsync();
        }

        await StartupSeeder.SeedSampleCatalogAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Assert.Contains(await db.Universities.ToListAsync(), university => university.Name == "South Valley University");
            Assert.DoesNotContain(await db.Universities.ToListAsync(), university => university.Name == "South Valley National University");

            var svuComputerScience = await db.Departments
                .Include(department => department.University)
                .SingleAsync(department =>
                    department.Name == "Computer Science" &&
                    department.University!.Name == "South Valley University");

            Assert.True(svuComputerScience.IsActive);
            Assert.Equal(144, svuComputerScience.TotalCreditsRequired);
            Assert.Equal(2.00m, svuComputerScience.MinGpaForGraduation);

            var staleCourse = await db.Courses.SingleAsync(course => course.Code == "OLD999");
            Assert.False(staleCourse.IsActive);
            Assert.False(await db.CoursePrerequisites.AnyAsync(prerequisite => prerequisite.CourseId == staleCourse.Id));

            var idssDepartment = await db.Departments
                .Include(department => department.University)
                .SingleAsync(department =>
                    department.Name == "Information and Decision Support Systems" &&
                    department.University!.Name == "Sinai University");

            var idssUniversityRequirementCodes = await db.Courses
                .Where(course =>
                    course.DepartmentId == idssDepartment.Id &&
                    course.IsActive &&
                    course.CourseType == CourseType.UniversityReq)
                .Select(course => course.Code)
                .OrderBy(code => code)
                .ToListAsync();

            Assert.Equal(9, idssUniversityRequirementCodes.Count);

            var idssUniversityRequirement = await db.GraduationRequirements
                .SingleAsync(requirement =>
                    requirement.DepartmentId == idssDepartment.Id &&
                    requirement.CategoryType == CourseType.UniversityReq);

            var linkedCodes = await db.GraduationRequirementCourses
                .Include(requirementCourse => requirementCourse.Course)
                .Where(requirementCourse =>
                    requirementCourse.GraduationRequirementId == idssUniversityRequirement.Id)
                .Select(requirementCourse => requirementCourse.Course!)
                .OrderBy(course => course.Code)
                .ToListAsync();

            Assert.Equal(idssUniversityRequirementCodes, linkedCodes.Select(course => course.Code).ToList());
            Assert.All(linkedCodes, course =>
            {
                Assert.Equal(idssDepartment.Id, course.DepartmentId);
                Assert.True(course.IsActive);
            });

            // FR-002d / FR-011: CSSE-only dept electives must not attach to IDSS.
            var idssRequirementIds = await db.GraduationRequirements
                .Where(requirement => requirement.DepartmentId == idssDepartment.Id)
                .Select(requirement => requirement.Id)
                .ToListAsync();

            var idssLinkedCodes = await db.GraduationRequirementCourses
                .Where(requirementCourse => idssRequirementIds.Contains(requirementCourse.GraduationRequirementId))
                .Select(requirementCourse => requirementCourse.Course!.Code)
                .ToListAsync();

            Assert.DoesNotContain("CSW433", idssLinkedCodes);
            Assert.DoesNotContain("ISD351", idssLinkedCodes);

            var csw433 = await db.Courses
                .Include(course => course.Department)
                .ThenInclude(department => department!.University)
                .SingleAsync(course =>
                    course.Code == "CSW433" &&
                    course.Department!.University!.Name == "Sinai University" &&
                    course.IsActive);

            Assert.Equal("Computer Science and Software Engineering", csw433.Department!.Name);
            Assert.Equal(CourseType.DeptElective, csw433.CourseType);
        }
    }
}

