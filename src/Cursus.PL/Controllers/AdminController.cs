using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Interfaces.Services;
using Cursus.PL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Cursus.Domain.Constants;

namespace Cursus.PL.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAdminDashboardService _adminDashboardService;
    private readonly ICourseService _courseService;
    private readonly IUniversityService _universityService;
    private readonly IDepartmentService _departmentService;
    private readonly IStudentManagementService _studentManagementService;
    private readonly IAcademicMetricsService _academicMetricsService;

    public AdminController(
        ApplicationDbContext context,
        ICourseService courseService,
        IAdminDashboardService adminDashboardService,
        IUniversityService universityService,
        IDepartmentService departmentService,
        IStudentManagementService studentManagementService,
        IAcademicMetricsService academicMetricsService)
    {
        _context = context;
        _courseService = courseService;
        _adminDashboardService = adminDashboardService;
        _universityService = universityService;
        _departmentService = departmentService;
        _studentManagementService = studentManagementService;
        _academicMetricsService = academicMetricsService;
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
                course.Code.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                course.Name.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        courses = courses.OrderBy(course => course.Code);

        await PopulateDepartmentsFilterDropDownListAsync(departmentId);

        return View("CourseIndex", courses);
    }

    /// <summary>Legacy route — redirects to <see cref="Courses"/>.</summary>
    public IActionResult CourseIndex(string? searchTerm, int? departmentId, bool includeInactive = false)
        => RedirectToAction(nameof(Courses), new { searchTerm, departmentId, includeInactive });

    public async Task<IActionResult> Students(string? searchTerm, int? departmentId)
    {
        ViewData["SearchTerm"] = searchTerm;
        ViewData["SelectedDepartmentId"] = departmentId;

        var students = await _studentManagementService.GetStudentsAsync(searchTerm, departmentId);

        await PopulateDepartmentsFilterDropDownListAsync(departmentId);

        return View("Students/Index", students);
    }

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
    public IActionResult Profile() => View();

    // ── EditStudent ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> EditStudent(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return RedirectToAction(nameof(Students));

        var student = await _context.Users
            .Include(u => u.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (student is null)
            return NotFound();

        var vm = new EditStudentViewModel
        {
            Id = student.Id,
            DisplayName = student.DisplayName,
            Email = student.Email,
            DepartmentId = student.DepartmentId ?? 0,
            AcademicYear = student.AcademicYear ?? string.Empty,
            CurrentSemester = student.CurrentSemester,
            CurrentStanding = student.CurrentStanding,
            EnrollmentDate = student.EnrollmentDate
        };

        await PopulateEditStudentFormAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStudent(EditStudentViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateEditStudentFormAsync(vm);
            return View(vm);
        }

        var student = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == vm.Id);

        if (student is null)
            return NotFound();

        student.DepartmentId = vm.DepartmentId;
        student.AcademicYear = vm.AcademicYear.Trim();
        student.CurrentSemester = vm.CurrentSemester;
        student.CurrentStanding = vm.CurrentStanding;
        student.EnrollmentDate = vm.EnrollmentDate;

        try
        {
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = $"{student.DisplayName}'s profile updated successfully.";
            return RedirectToAction(nameof(StudentDetail), new { id = vm.Id });
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Unable to update student profile.";
            ModelState.AddModelError(string.Empty, "A database error occurred. Please try again.");
            await PopulateEditStudentFormAsync(vm);
            return View(vm);
        }
    }

    // ── Student Detail ────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> StudentDetail(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return RedirectToAction(nameof(Students));

        var student = await _studentManagementService.GetStudentDetailAsync(id);
        if (student is null)
            return NotFound();

        return View("Students/Detail", student);
    }

    // ── StudentAddCourse ──────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> StudentAddCourse(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return RedirectToAction(nameof(Students));

        var student = await _studentManagementService.GetStudentDetailAsync(id);
        if (student is null)
            return NotFound();

        var vm = new AddCourseRecordViewModel
        {
            StudentId = id,
            StudentName = student.DisplayName,
            AcademicYear = DateTime.Today.Year + "-" + (DateTime.Today.Year + 1)
        };

        await PopulateStudentCourseFormAsync(vm, student.DepartmentId);
        return View("Students/AddCourse", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentAddCourse(AddCourseRecordViewModel vm)
    {
        // ── Extra domain validation ───────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(vm.Grade) && !IsKnownGrade(vm.Grade))
            ModelState.AddModelError(nameof(vm.Grade), "Grade must be one of: A+, A, A-, B+, B, B-, C+, C, C-, D+, D, D-, F.");

        // Check for duplicate course record and validate enrollment
        if (ModelState.IsValid)
        {
            var duplicate = await _context.StudentCourses.AnyAsync(sc =>
                sc.StudentId == vm.StudentId &&
                sc.CourseId == vm.CourseId &&
                sc.Semester == vm.Semester &&
                sc.AcademicYear == vm.AcademicYear.Trim());

            if (duplicate)
            {
                ModelState.AddModelError(string.Empty,
                    "This student already has a record for the selected course in the same semester and academic year.");
            }
            else
            {
                var (canEnroll, blockReason) = await _academicMetricsService.CanEnrollInCourseAsync(vm.StudentId, vm.CourseId);
                if (!canEnroll)
                {
                    ModelState.AddModelError(string.Empty, blockReason ?? "Student is not eligible to enroll in this course.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            var student = await _studentManagementService.GetStudentDetailAsync(vm.StudentId);
            await PopulateStudentCourseFormAsync(vm, student?.DepartmentId);
            return View("Students/AddCourse", vm);
        }

        try
        {
            await _studentManagementService.AddCourseRecordAsync(
                vm.StudentId,
                vm.CourseId,
                vm.Grade,
                StudentCourseStatus.InProgress,   // service resolves this from grade
                vm.Semester,
                vm.AcademicYear.Trim());

            TempData["StatusMessage"] = "Course record added successfully.";
            return RedirectToAction(nameof(StudentDetail), new { id = vm.StudentId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Unable to add course record.";
            ModelState.AddModelError(string.Empty, ex.Message);
            var student = await _studentManagementService.GetStudentDetailAsync(vm.StudentId);
            await PopulateStudentCourseFormAsync(vm, student?.DepartmentId);
            return View("Students/AddCourse", vm);
        }
    }

    // ── StudentEditCourse ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> StudentEditCourse(int? id)
    {
        if (id is null)
            return RedirectToAction(nameof(Students));

        var record = await _context.StudentCourses
            .Include(sc => sc.Course)
            .Include(sc => sc.Student)
            .AsNoTracking()
            .FirstOrDefaultAsync(sc => sc.Id == id.Value);

        if (record is null)
            return NotFound();

        var vm = new EditCourseRecordViewModel
        {
            RecordId = record.Id,
            StudentId = record.StudentId,
            StudentName = record.Student?.DisplayName ?? "Student",
            CourseCode = record.Course?.Code ?? string.Empty,
            CourseName = record.Course?.Name ?? string.Empty,
            Grade = record.Grade,
            Status = record.Status,
            Semester = record.Semester,
            AcademicYear = record.AcademicYear
        };

        await PopulateStudentCourseFormAsync(vm, record.Student?.DepartmentId);
        return View("Students/EditCourse", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentEditCourse(EditCourseRecordViewModel vm)
    {
        if (!string.IsNullOrWhiteSpace(vm.Grade) && !IsKnownGrade(vm.Grade))
            ModelState.AddModelError(nameof(vm.Grade), "Grade must be one of: A+, A, A-, B+, B, B-, C+, C, C-, D+, D, D-, F.");

        if (ModelState.IsValid)
        {
            var existingRecord = await _context.StudentCourses
                .AsNoTracking()
                .FirstOrDefaultAsync(sc => sc.Id == vm.RecordId);

            if (existingRecord is not null)
            {
                var (canEnroll, blockReason) = await _academicMetricsService.CanEnrollInCourseAsync(
                    existingRecord.StudentId,
                    existingRecord.CourseId,
                    excludeStudentCourseId: vm.RecordId);

                var settingInProgress = string.IsNullOrWhiteSpace(vm.Grade)
                    || vm.Status == StudentCourseStatus.InProgress;
                var settingPassingGrade = !string.IsNullOrWhiteSpace(vm.Grade)
                    && !new[] { "D+", "D", "D-", "F" }.Contains(vm.Grade.Trim().ToUpper());

                if ((settingInProgress || settingPassingGrade) && !canEnroll)
                {
                    ModelState.AddModelError(string.Empty, blockReason ?? "Student is not eligible for this course state.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            var student = await _studentManagementService.GetStudentDetailAsync(vm.StudentId);
            await PopulateStudentCourseFormAsync(vm, student?.DepartmentId);
            return View("Students/EditCourse", vm);
        }

        try
        {
            await _studentManagementService.UpdateCourseRecordAsync(
                vm.RecordId,
                vm.Grade,
                vm.Status);

            TempData["StatusMessage"] = "Course record updated successfully.";
            return RedirectToAction(nameof(StudentDetail), new { id = vm.StudentId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Unable to update course record.";
            ModelState.AddModelError(string.Empty, ex.Message);
            var student = await _studentManagementService.GetStudentDetailAsync(vm.StudentId);
            await PopulateStudentCourseFormAsync(vm, student?.DepartmentId);
            return View("Students/EditCourse", vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentDeleteCourse(int id)
    {
        var record = await _context.StudentCourses.FindAsync(id);
        if (record is null)
            return NotFound();

        var studentId = record.StudentId;
        try
        {
            await _studentManagementService.DeleteCourseRecordAsync(id);
            TempData["StatusMessage"] = "Course record deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Unable to delete course record: {ex.Message}";
        }

        return RedirectToAction(nameof(StudentDetail), new { id = studentId });
    }

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

    // ── Form population helpers ───────────────────────────────────────────────

    private static readonly string[] KnownGrades =
        ["A+", "A", "A-", "B+", "B", "B-", "C+", "C", "C-", "D+", "D", "D-", "F"];

    private static bool IsKnownGrade(string? grade) =>
        !string.IsNullOrWhiteSpace(grade) &&
        KnownGrades.Contains(grade.Trim().ToUpper());

    /// <summary>
    /// Fills <paramref name="vm"/>'s CourseOptions, GradeOptions, and
    /// SemesterOptions SelectLists.  Courses are filtered to the student's
    /// department when <paramref name="departmentId"/> is supplied.
    /// </summary>
    private async Task PopulateStudentCourseFormAsync(
        CourseRecordFormBase vm, int? departmentId = null)
    {
        // Courses — scoped to student's department, active only
        var coursesQuery = _context.Courses
            .Include(c => c.Department)
            .Where(c => c.IsActive)
            .AsNoTracking()
            .AsQueryable();

        if (departmentId.HasValue)
            coursesQuery = coursesQuery.Where(c => c.DepartmentId == departmentId.Value);

        var courses = await coursesQuery
            .OrderBy(c => c.Code)
            .Select(c => new
            {
                c.Id,
                Label = $"{c.Code} — {c.Name} ({c.CreditHours} cr)"
            })
            .ToListAsync();

        vm.CourseOptions = courses.Select(c => new SelectListItem(c.Label, c.Id.ToString()));

        // Grade options — blank first for InProgress
        vm.GradeOptions =
        [
            new SelectListItem("— No grade (In Progress) —", ""),
            .. KnownGrades.Select(g => new SelectListItem(g, g))
        ];

        // Semester options from enum
        vm.SemesterOptions = Enum.GetValues<SemesterType>()
            .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString()));
    }

    private async Task PopulateEditStudentFormAsync(EditStudentViewModel vm)
    {
        var departments = await _context.Departments
            .Include(d => d.University)
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();

        vm.DepartmentOptions = departments.Select(d => new SelectListItem(
            d.University is null ? d.Name : $"{d.Name} ({d.University.Name})",
            d.Id.ToString()));

        vm.SemesterOptions = Enum.GetValues<SemesterType>()
            .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString()));

        vm.StandingOptions = Enum.GetValues<AcademicStanding>()
            .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString()));
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