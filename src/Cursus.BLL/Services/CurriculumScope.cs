using Cursus.Domain.Entities;

namespace Cursus.BLL.Services;

/// <summary>
/// Scopes curriculum queries to a student's department. University requirements are
/// stored per-department in seed data — never union all UniversityReq rows globally.
/// </summary>
public static class CurriculumScope
{
    public static bool IsInDepartmentCurriculum(Course course, int departmentId) =>
        course.DepartmentId == departmentId;

    public static IQueryable<Course> ForDepartment(IQueryable<Course> query, int departmentId) =>
        query.Where(c => c.DepartmentId == departmentId && c.IsActive);
}
