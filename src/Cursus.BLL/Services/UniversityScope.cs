using Cursus.Domain.Entities;

namespace Cursus.BLL.Services;

/// <summary>
/// Filters catalog entities to a single university. Prefer this over loading all rows
/// and filtering in memory.
/// </summary>
public static class UniversityScope
{
    public static IQueryable<Department> ForUniversity(IQueryable<Department> query, int universityId) =>
        query.Where(d => d.UniversityId == universityId);

    public static IQueryable<Course> ForUniversity(IQueryable<Course> query, int universityId) =>
        query.Where(c => c.Department != null && c.Department.UniversityId == universityId);

    public static IQueryable<GraduationRequirement> ForUniversity(
        IQueryable<GraduationRequirement> query, int universityId) =>
        query.Where(g => g.Department != null && g.Department.UniversityId == universityId);
}
