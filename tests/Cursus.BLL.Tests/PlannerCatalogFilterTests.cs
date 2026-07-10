using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Tests;

public sealed class PlannerCatalogFilterTests
{
    [Fact]
    public async Task CatalogFilter_IncludesOnlyCoursesAvailableForPrimaryFallTerm()
    {
        var db = await PlannerTestData.SeedPlannerStudentAsync();
        await PlannerTestData.AddCoursesAsync(
            db,
            PlannerTestData.Course(1, "ALL", availability: SemesterAvailability.All),
            PlannerTestData.Course(2, "FS", availability: SemesterAvailability.FallSpring),
            PlannerTestData.Course(3, "FALL", availability: SemesterAvailability.Fall),
            PlannerTestData.Course(4, "SPR", availability: SemesterAvailability.Spring));

        var primaryTerm = new { AcademicYear = PlannerTestData.AcademicYear, Semester = SemesterType.Fall };

        var catalog = await db.Courses
            .Where(c => c.DepartmentId == PlannerTestData.DepartmentId && c.IsActive)
            .Where(c =>
                c.SemesterAvailability == SemesterAvailability.All
                || c.SemesterAvailability == SemesterAvailability.FallSpring
                || (primaryTerm.Semester == SemesterType.Fall && c.SemesterAvailability == SemesterAvailability.Fall)
                || (primaryTerm.Semester == SemesterType.Spring && c.SemesterAvailability == SemesterAvailability.Spring))
            .Select(c => c.Code)
            .ToListAsync();

        Assert.Equal(3, catalog.Count);
        Assert.Contains("ALL", catalog);
        Assert.Contains("FS", catalog);
        Assert.Contains("FALL", catalog);
        Assert.DoesNotContain("SPR", catalog);
    }
}

