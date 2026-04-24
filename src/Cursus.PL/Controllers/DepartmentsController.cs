using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cursus.PL.Controllers;

public class DepartmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? searchTerm)
    {
        ViewData["SearchTerm"] = searchTerm;

        var departmentsQuery = _context.Departments
            .Include(department => department.University)
            .Include(department => department.Courses.Where(course => course.IsActive))
            .AsNoTracking()
            .Where(department => department.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim();
            departmentsQuery = departmentsQuery.Where(department =>
                department.Name.Contains(normalizedSearchTerm) ||
                department.University!.Name.Contains(normalizedSearchTerm));
        }

        var departments = await departmentsQuery
            .OrderBy(department => department.Name)
            .ToListAsync();

        return View(departments);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var department = await _context.Departments
            .Include(department => department.University)
            .Include(department => department.Courses.Where(course => course.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(department => department.Id == id && department.IsActive);

        if (department is null)
        {
            return NotFound();
        }

        return View(department);
    }
}