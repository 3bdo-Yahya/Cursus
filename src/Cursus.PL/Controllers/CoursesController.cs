using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cursus.PL.Controllers;

public class CoursesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CoursesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? searchTerm, int? universityId, int? departmentId)
    {
        ViewData["SearchTerm"] = searchTerm;
        ViewData["SelectedUniversityId"] = universityId;
        ViewData["SelectedDepartmentId"] = departmentId;

        var coursesQuery = _context.Courses
            .Include(course => course.Department)
            .AsNoTracking()
            .Where(course => course.IsActive)
            .AsQueryable();

        if (departmentId.HasValue)
        {
            coursesQuery = coursesQuery.Where(course => course.DepartmentId == departmentId.Value);
        }

        if (universityId.HasValue)
        {
            coursesQuery = coursesQuery.Where(course => course.Department!.UniversityId == universityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim();
            coursesQuery = coursesQuery.Where(course =>
                course.Code.Contains(normalizedSearchTerm) ||
                course.Name.Contains(normalizedSearchTerm));
        }

        ViewData["Universities"] = await _context.Universities
            .AsNoTracking()
            .OrderBy(university => university.Name)
            .ToListAsync();

        var departmentsQuery = _context.Departments
            .Include(department => department.University)
            .AsNoTracking()
            .Where(department => department.IsActive)
            .AsQueryable();

        if (universityId.HasValue)
        {
            departmentsQuery = departmentsQuery.Where(department => department.UniversityId == universityId.Value);
        }

        ViewData["Departments"] = await departmentsQuery
            .OrderBy(department => department.Name)
            .ToListAsync();

        var courses = await coursesQuery
            .OrderBy(course => course.Code)
            .ToListAsync();

        return View(courses);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var course = await _context.Courses
            .Include(course => course.Department)
                .ThenInclude(department => department!.University)
            .Include(course => course.Prerequisites)
                .ThenInclude(prerequisite => prerequisite.Prerequisite)
            .AsNoTracking()
            .FirstOrDefaultAsync(course => course.Id == id && course.IsActive);

        if (course is null)
        {
            return NotFound();
        }

        return View(course);
    }
}