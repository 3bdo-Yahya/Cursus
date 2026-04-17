using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Cursus.PL.Controllers;

public class DepartmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(bool includeInactive = false)
    {
        ViewData["IncludeInactive"] = includeInactive;

        var departmentsQuery = _context.Departments
            .Include(department => department.University)
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .AsQueryable();

        if (!includeInactive)
        {
            departmentsQuery = departmentsQuery.Where(department => department.IsActive);
        }

        var departments = await departmentsQuery.ToListAsync();
        return View(departments);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var department = await _context.Departments
            .Include(d => d.University)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (department is null)
        {
            return NotFound();
        }

        return View(department);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateUniversitiesDropDownListAsync();
        return View(new Department());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,UniversityId,TotalCreditsRequired,MinGpaForGraduation,IsActive")] Department department)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUniversitiesDropDownListAsync(department.UniversityId);
            return View(department);
        }

        _context.Add(department);

        try
        {
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Department created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Unable to save department. Ensure department name is unique within the selected university.");
            await PopulateUniversitiesDropDownListAsync(department.UniversityId);
            return View(department);
        }
    }

    public async Task<IActionResult> Edit(int? id)
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
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,UniversityId,TotalCreditsRequired,MinGpaForGraduation,IsActive")] Department department)
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
            _context.Update(department);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Department updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await DepartmentExistsAsync(department.Id))
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
    public async Task<IActionResult> Deactivate(int id)
    {
        var department = await _context.Departments.FindAsync(id);

        if (department is null)
        {
            return NotFound();
        }

        if (!department.IsActive)
        {
            TempData["StatusMessage"] = "Department is already inactive.";
            return RedirectToAction(nameof(Index));
        }

        department.IsActive = false;
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Department deactivated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateUniversitiesDropDownListAsync(object? selectedUniversity = null)
    {
        var universities = await _context.Universities
            .AsNoTracking()
            .OrderBy(university => university.Name)
            .ToListAsync();

        ViewData["UniversityId"] = new SelectList(universities, "Id", "Name", selectedUniversity);
    }

    private Task<bool> DepartmentExistsAsync(int id)
    {
        return _context.Departments.AnyAsync(department => department.Id == id);
    }
}
