using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Cursus.PL.Controllers;

public class CoursesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CoursesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(bool includeInactive = false)
    {
        ViewData["IncludeInactive"] = includeInactive;

        var coursesQuery = _context.Courses
            .Include(course => course.Department)
            .AsNoTracking()
            .OrderBy(course => course.Code)
            .AsQueryable();

        if (!includeInactive)
        {
            coursesQuery = coursesQuery.Where(course => course.IsActive);
        }

        var courses = await coursesQuery.ToListAsync();
        return View(courses);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var course = await _context.Courses
            .Include(c => c.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (course is null)
        {
            return NotFound();
        }

        return View(course);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDepartmentsDropDownListAsync();
        return View(new Course());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Code,Name,CreditHours,CourseType,SemesterAvailability,PassingGradeThreshold,DepartmentId,IsActive")] Course course)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDepartmentsDropDownListAsync(course.DepartmentId);
            return View(course);
        }

        _context.Add(course);

        try
        {
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Course created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Unable to save course. Ensure course code is unique within the selected department.");
            await PopulateDepartmentsDropDownListAsync(course.DepartmentId);
            return View(course);
        }
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var course = await _context.Courses.FindAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        await PopulateDepartmentsDropDownListAsync(course.DepartmentId);
        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Code,Name,CreditHours,CourseType,SemesterAvailability,PassingGradeThreshold,DepartmentId,IsActive")] Course course)
    {
        if (id != course.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDepartmentsDropDownListAsync(course.DepartmentId);
            return View(course);
        }

        try
        {
            _context.Update(course);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Course updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await CourseExistsAsync(course.Id))
            {
                return NotFound();
            }

            throw;
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Unable to save course. Ensure course code is unique within the selected department.");
            await PopulateDepartmentsDropDownListAsync(course.DepartmentId);
            return View(course);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course is null)
        {
            return NotFound();
        }

        if (!course.IsActive)
        {
            TempData["StatusMessage"] = "Course is already inactive.";
            return RedirectToAction(nameof(Index));
        }

        course.IsActive = false;
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Course deactivated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDepartmentsDropDownListAsync(object? selectedDepartment = null)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .Where(department => department.IsActive)
            .OrderBy(department => department.Name)
            .ToListAsync();

        ViewData["DepartmentId"] = new SelectList(departments, "Id", "Name", selectedDepartment);
    }

    private Task<bool> CourseExistsAsync(int id)
    {
        return _context.Courses.AnyAsync(course => course.Id == id);
    }
}
