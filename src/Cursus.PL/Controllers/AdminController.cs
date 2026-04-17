using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Cursus.PL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Cursus.PL.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var dashboard = new AdminDashboardViewModel
        {
            TotalUniversities = await _context.Universities.CountAsync(),
            TotalGraduationRequirements = await _context.GraduationRequirements.CountAsync(),
            TotalDepartments = await _context.Departments.CountAsync(),
            ActiveDepartments = await _context.Departments.CountAsync(department => department.IsActive),
            InactiveDepartments = await _context.Departments.CountAsync(department => !department.IsActive),
            TotalCourses = await _context.Courses.CountAsync(),
            ActiveCourses = await _context.Courses.CountAsync(course => course.IsActive),
            InactiveCourses = await _context.Courses.CountAsync(course => !course.IsActive)
        };

        return View(dashboard);
    }

    public async Task<IActionResult> DepartmentIndex()
    {
        var departments = await _context.Departments
            .Include(department => department.University)
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .ToListAsync();

        return View(departments);
    }

    [HttpGet]
    public async Task<IActionResult> DepartmentCreate()
    {
        await PopulateUniversitiesDropDownListAsync();
        return View(new Department());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartmentCreate([Bind("Name,UniversityId,TotalCreditsRequired,MinGpaForGraduation,IsActive")] Department department)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUniversitiesDropDownListAsync(department.UniversityId);
            return View(department);
        }

        _context.Departments.Add(department);

        try
        {
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Department created successfully.";
            return RedirectToAction(nameof(DepartmentIndex));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Unable to save department. Ensure department name is unique within the selected university.");
            await PopulateUniversitiesDropDownListAsync(department.UniversityId);
            return View(department);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DepartmentEdit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var department = await _context.Departments.FindAsync(id);
        if (department is null)
        {
            return NotFound();
        }

        await PopulateUniversitiesDropDownListAsync(department.UniversityId);
        return View(department);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartmentEdit(int id, [Bind("Id,Name,UniversityId,TotalCreditsRequired,MinGpaForGraduation,IsActive")] Department department)
    {
        if (id != department.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateUniversitiesDropDownListAsync(department.UniversityId);
            return View(department);
        }

        try
        {
            _context.Departments.Update(department);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Department updated successfully.";
            return RedirectToAction(nameof(DepartmentIndex));
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await _context.Departments.AnyAsync(dept => dept.Id == department.Id);
            if (!exists)
            {
                return NotFound();
            }

            throw;
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Unable to save department. Ensure department name is unique within the selected university.");
            await PopulateUniversitiesDropDownListAsync(department.UniversityId);
            return View(department);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartmentDeactivate(int id)
    {
        var department = await _context.Departments.FindAsync(id);

        if (department is null)
        {
            return NotFound();
        }

        if (!department.IsActive)
        {
            TempData["StatusMessage"] = "Department is already inactive.";
            return RedirectToAction(nameof(DepartmentIndex));
        }

        department.IsActive = false;
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Department deactivated successfully.";
        return RedirectToAction(nameof(DepartmentIndex));
    }

    public async Task<IActionResult> CourseIndex(bool includeInactive = false)
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

    [HttpGet]
    public async Task<IActionResult> CourseCreate()
    {
        await PopulateDepartmentsDropDownListAsync();
        return View(new Course());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CourseCreate([Bind("Code,Name,CreditHours,CourseType,SemesterAvailability,PassingGradeThreshold,DepartmentId,IsActive")] Course course)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDepartmentsDropDownListAsync(course.DepartmentId);
            return View(course);
        }

        _context.Courses.Add(course);

        try
        {
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Course created successfully.";
            return RedirectToAction(nameof(CourseIndex));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Unable to save course. Ensure course code is unique within the selected department.");
            await PopulateDepartmentsDropDownListAsync(course.DepartmentId);
            return View(course);
        }
    }

    [HttpGet]
    public async Task<IActionResult> CourseEdit(int? id)
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
    public async Task<IActionResult> CourseEdit(int id, [Bind("Id,Code,Name,CreditHours,CourseType,SemesterAvailability,PassingGradeThreshold,DepartmentId,IsActive")] Course course)
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
            _context.Courses.Update(course);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Course updated successfully.";
            return RedirectToAction(nameof(CourseIndex));
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await _context.Courses.AnyAsync(c => c.Id == course.Id);
            if (!exists)
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
    public async Task<IActionResult> CourseDeactivate(int id)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course is null)
        {
            return NotFound();
        }

        if (!course.IsActive)
        {
            TempData["StatusMessage"] = "Course is already inactive.";
            return RedirectToAction(nameof(CourseIndex));
        }

        course.IsActive = false;
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Course deactivated successfully.";
        return RedirectToAction(nameof(CourseIndex));
    }

    private async Task PopulateUniversitiesDropDownListAsync(object? selectedUniversity = null)
    {
        var universities = await _context.Universities
            .AsNoTracking()
            .OrderBy(university => university.Name)
            .ToListAsync();

        ViewData["UniversityId"] = new SelectList(universities, "Id", "Name", selectedUniversity);
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
}