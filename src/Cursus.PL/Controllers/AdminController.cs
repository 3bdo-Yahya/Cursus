using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Interfaces.Services;
using Cursus.PL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Cursus.Domain.Constants;

namespace Cursus.PL.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminController : Controller
{
    private readonly IAdminDashboardService _adminDashboardService;
    private readonly ICourseService _courseService;
    private readonly IUniversityService _universityService;
    private readonly IDepartmentService _departmentService;
    public AdminController(ICourseService courseService, IAdminDashboardService adminDashboardService, IUniversityService universityService, IDepartmentService departmentService)
    {
        _courseService = courseService;
        _adminDashboardService = adminDashboardService;
        _universityService = universityService;
        _departmentService = departmentService;
    }

    public async Task<IActionResult> Courses(string? searchTerm, int? departmentId, bool includeInactive = false)
    {
        ViewData["SearchTerm"] = searchTerm;
        ViewData["SelectedDepartmentId"] = departmentId;
        ViewData["IncludeInactive"] = includeInactive;

        var courses = await _courseService.GetAllAsync();

        if (!includeInactive)
        {
            courses = courses.Where(course => course.IsActive);
        }

        if (departmentId.HasValue)
        {
            courses = courses.Where(course => course.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim();
            courses = courses.Where(course =>
                course.Code.Contains(normalizedSearchTerm) ||
                course.Name.Contains(normalizedSearchTerm));
        }

        await PopulateDepartmentsFilterDropDownListAsync(departmentId);

        courses = courses
            .OrderBy(course => course.Code);

        return View("CourseIndex", courses);
    }

    /// <summary>Legacy route — redirects to <see cref="Courses"/>.</summary>
    public IActionResult CourseIndex(string? searchTerm, int? departmentId, bool includeInactive = false)
        => RedirectToAction(nameof(Courses), new { searchTerm, departmentId, includeInactive });

    public IActionResult Students() => View();

    public IActionResult AddCourse() => RedirectToAction(nameof(CourseCreate));

    public IActionResult EditCourse(int? id)
    {
        if (id is null)
        {
            return RedirectToAction(nameof(Courses));
        }

        return RedirectToAction(nameof(CourseEdit), new { id = id.Value });
    }

    public IActionResult ViewCourse(int? id)
    {
        if (id is null)
        {
            return RedirectToAction(nameof(Courses));
        }

        return RedirectToAction(nameof(CourseEdit), new { id = id.Value });
    }

    public IActionResult AddStudent() => View();
    public IActionResult EditStudent() => View();
    public IActionResult ViewStudent() => View();
    public IActionResult Profile() => View();

    public async Task<IActionResult> Index()
    {
        var dashboard = await _adminDashboardService.GetAdminDashboardAsync();

        return View(dashboard);
    }

    public async Task<IActionResult> UniversityIndex()
    {
        var universities = await _universityService.GetAllAsync();

        return View(universities);
    }

    [HttpGet]
    public IActionResult UniversityCreate()
    {
        return View(new CreateUniversityDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UniversityCreate([Bind("Name")] CreateUniversityDto university)
    {
        university.Name = university.Name?.Trim() ?? string.Empty;

        if (await IsUniversityNameDuplicateAsync(university.Name))
        {
            ModelState.AddModelError(nameof(University.Name), "University name must be unique.");
        }

        if (!ModelState.IsValid)
        {
            return View(university);
        }


        try
        {
            await _universityService.AddAsync(university);
            TempData["StatusMessage"] = "University created successfully.";
            return RedirectToAction(nameof(UniversityIndex));
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Unable to create university.";
            ModelState.AddModelError(string.Empty, "Unable to save university. Ensure university name is unique.");
            return View(university);
        }
    }

    public async Task<IActionResult> DepartmentIndex()
    {
        var departments = await _departmentService.GetAllAsync();

        return View(departments);
    }

    [HttpGet]
    public async Task<IActionResult> DepartmentCreate()
    {
        await PopulateUniversitiesDropDownListAsync();
        return View(new CreateDepartmentDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartmentCreate([Bind("Name,UniversityId,TotalCreditsRequired,MinGpaForGraduation,IsActive")] CreateDepartmentDto department)
    {
        department.Name = department.Name?.Trim() ?? string.Empty;

        if (department.UniversityId <= 0)
        {
            ModelState.AddModelError(nameof(Department.UniversityId), "Please select a university.");
        }

        if (await IsDepartmentNameDuplicateAsync(department.UniversityId, department.Name))
        {
            ModelState.AddModelError(nameof(Department.Name), "Department name must be unique within the selected university.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateUniversitiesDropDownListAsync(department.UniversityId);
            return View(department);
        }

        try
        {
            await _departmentService.AddAsync(department);
            TempData["StatusMessage"] = "Department created successfully.";
            return RedirectToAction(nameof(DepartmentIndex));
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Unable to create department.";
            ModelState.AddModelError(string.Empty, "Unable to save department. Ensure department name is unique within the selected university.");
            await PopulateUniversitiesDropDownListAsync(department.UniversityId);
            return View(department);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DepartmentEdit(int id)
    {

        var department = await _departmentService.GetByIdAsync(id);
        if (department is null)
        {
            return NotFound();
        }

        await PopulateUniversitiesDropDownListAsync(department.UniversityId);
        var model = new EditDepartmentDto()
        {
            Id = department.Id,
            Name = department.Name,
            UniversityId = department.UniversityId,
            TotalCreditsRequired = department.TotalCreditsRequired,
            MinGpaForGraduation = department.MinGpaForGraduation,
            IsActive = department.IsActive
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartmentEdit(int id, [Bind("Id,Name,UniversityId,TotalCreditsRequired,MinGpaForGraduation,IsActive")] EditDepartmentDto department)
    {
        if (id != department.Id)
        {
            return NotFound();
        }

        department.Name = department.Name?.Trim() ?? string.Empty;

        if (department.UniversityId <= 0)
        {
            ModelState.AddModelError(nameof(Department.UniversityId), "Please select a university.");
        }

        if (await IsDepartmentNameDuplicateAsync(department.UniversityId, department.Name, department.Id))
        {
            ModelState.AddModelError(nameof(Department.Name), "Department name must be unique within the selected university.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateUniversitiesDropDownListAsync(department.UniversityId);
            return View(department);
        }

        try
        {
            await _departmentService.UpdateAsync(department);
            TempData["StatusMessage"] = "Department updated successfully.";
            return RedirectToAction(nameof(DepartmentIndex));
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await _departmentService.ExistsAsync(department.Id);
            if (!exists)
                return NotFound();

            throw;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Unable to update department.";
            ModelState.AddModelError(string.Empty, "Unable to save department. Ensure department name is unique within the selected university.");
            await PopulateUniversitiesDropDownListAsync(department.UniversityId);
            return View(department);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartmentDeactivate(int id)
    {
        var department = await _departmentService.GetByIdAsync(id);

        if (department is null)
        {
            return NotFound();
        }

        if (!department.IsActive)
        {
            TempData["StatusMessage"] = "Department is already inactive.";
            return RedirectToAction(nameof(DepartmentIndex));
        }

        await _departmentService.ToggleActiveAsync(id);

        TempData["StatusMessage"] = "Department deactivated successfully.";
        return RedirectToAction(nameof(DepartmentIndex));
    }

    [HttpGet]
    public async Task<IActionResult> CourseCreate()
    {
        await PopulateDepartmentsDropDownListAsync();
        return View(new CreateCourseDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CourseCreate([Bind("Code,Name,CreditHours,CourseType,SemesterAvailability,PassingGradeThreshold,DepartmentId,IsActive")] CreateCourseDto course)
    {
        course.Code = course.Code?.Trim() ?? string.Empty;
        course.Name = course.Name?.Trim() ?? string.Empty;

        if (course.DepartmentId <= 0)
        {
            ModelState.AddModelError(nameof(Course.DepartmentId), "Please select a department.");
        }

        if (await IsCourseCodeDuplicateAsync(course.DepartmentId, course.Code))
        {
            ModelState.AddModelError(nameof(Course.Code), "Course code must be unique within the selected department.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDepartmentsDropDownListAsync(course.DepartmentId);
            return View(course);
        }

        try
        {
            await _courseService.AddAsync(course);
            TempData["StatusMessage"] = "Course created successfully.";
            return RedirectToAction(nameof(Courses));
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Unable to create course.";
            ModelState.AddModelError(string.Empty, "Unable to save course. Ensure course code is unique within the selected department.");
            await PopulateDepartmentsDropDownListAsync(course.DepartmentId);
            return View(course);
        }
    }

    [HttpGet]
    public async Task<IActionResult> CourseEdit(int id)
    {

        var course = await _courseService.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        var model = new EditCourseDto()
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            DepartmentId = course.DepartmentId,
            CreditHours = course.CreditHours,
            PassingGradeThreshold = course.PassingGradeThreshold,
            CourseType = course.CourseType,
            SemesterAvailability = course.SemesterAvailability,
            IsActive = course.IsActive
        };

        await PopulateDepartmentsDropDownListAsync(course.DepartmentId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CourseEdit(int id, [Bind("Id,Code,Name,CreditHours,CourseType,SemesterAvailability,PassingGradeThreshold,DepartmentId,IsActive")] EditCourseDto course)
    {
        if (id != course.Id)
        {
            return NotFound();
        }

        course.Code = course.Code?.Trim() ?? string.Empty;
        course.Name = course.Name?.Trim() ?? string.Empty;

        if (course.DepartmentId <= 0)
        {
            ModelState.AddModelError(nameof(Course.DepartmentId), "Please select a department.");
        }

        if (await IsCourseCodeDuplicateAsync(course.DepartmentId, course.Code, course.Id))
        {
            ModelState.AddModelError(nameof(Course.Code), "Course code must be unique within the selected department.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDepartmentsDropDownListAsync(course.DepartmentId);
            return View(course);
        }

        try
        {
            await _courseService.UpdateAsync(course);
            TempData["StatusMessage"] = "Course updated successfully.";
            return RedirectToAction(nameof(Courses));
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await _courseService.ExistsAsync(course.Id);
            if (!exists)
                return NotFound();

            throw;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Unable to update course.";
            ModelState.AddModelError(string.Empty, "Unable to save course. Ensure course code is unique within the selected department.");
            await PopulateDepartmentsDropDownListAsync(course.DepartmentId);
            return View(course);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CourseDeactivate(int id, string? searchTerm, int? departmentId, bool includeInactive = false)
    {
        var course = await _courseService.GetByIdAsync(id);

        if (course is null)
        {
            return NotFound();
        }

        await _courseService.ToggleActiveAsync(id);

        TempData["StatusMessage"] = course.IsActive
            ? "Course reactivated successfully."
            : "Course deactivated successfully.";

        return RedirectToAction(nameof(Courses), new { searchTerm, departmentId, includeInactive });
    }

    private async Task PopulateUniversitiesDropDownListAsync(object? selectedUniversity = null)
    {
        var universities = await _universityService.GetAllAsync();

        ViewData["UniversityId"] = new SelectList(universities, "Id", "Name", selectedUniversity);
    }

    private async Task PopulateDepartmentsDropDownListAsync(int? selectedDepartment = null)
    {
        var departments = (await _departmentService.GetAllAsync(isActive: true)).ToList();

        if (selectedDepartment.HasValue && selectedDepartment.Value > 0 &&
            !departments.Any(d => d.Id == selectedDepartment.Value))
        {
            var inactiveDepartment = await _departmentService.GetByIdAsync(selectedDepartment.Value);

            if (inactiveDepartment is not null)
            {
                departments.Add(inactiveDepartment);
                departments = departments.OrderBy(d => d.Name).ToList();
            }
        }

        var departmentOptions = departments
            .Select(d => new
            {
                d.Id,
                DisplayName = string.IsNullOrEmpty(d.UniversityName)
                    ? d.Name
                    : $"{d.Name} ({d.UniversityName})"
            })
            .ToList();

        ViewData["DepartmentId"] = new SelectList(departmentOptions, "Id", "DisplayName", selectedDepartment);
    }

    private async Task PopulateDepartmentsFilterDropDownListAsync(int? selectedDepartment = null)
    {
        var departments = (await _departmentService.GetAllAsync()).ToList();

        var departmentOptions = departments
            .Select(d => new
            {
                d.Id,
                DisplayName = string.IsNullOrEmpty(d.UniversityName)
                    ? d.Name
                    : $"{d.Name} ({d.UniversityName})"
            })
            .ToList();

        ViewData["FilterDepartmentId"] = new SelectList(departmentOptions, "Id", "DisplayName", selectedDepartment);
    }

    private Task<bool> IsCourseCodeDuplicateAsync(int departmentId, string code, int? excludedCourseId = null)
    {
        return _courseService.IsCodeDuplicateAsync(departmentId, code, excludedCourseId);
    }

    private Task<bool> IsDepartmentNameDuplicateAsync(int universityId, string name, int? excludedDepartmentId = null)
    {
        return _departmentService.IsNameDuplicateAsync(universityId, name, excludedDepartmentId);
    }

    private Task<bool> IsUniversityNameDuplicateAsync(string name, int? excludeId = null)
    {
        return _universityService.IsNameDuplicateAsync(name, excludeId);
    }
}