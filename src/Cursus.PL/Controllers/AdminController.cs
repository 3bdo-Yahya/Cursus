using Cursus.DAL.Database;
using Cursus.Domain;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Interfaces.Services;
using Cursus.PL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Cursus.Domain.Constants;

namespace Cursus.PL.Controllers;

[Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAdminDashboardService _adminDashboardService;
    private readonly IAdminScopeService _adminScopeService;
    private readonly ICourseService _courseService;
    private readonly IUniversityService _universityService;
    private readonly IUniversityAdminService _universityAdminService;
    private readonly IDepartmentService _departmentService;
    private readonly IStudentManagementService _studentManagementService;
    private readonly IAcademicMetricsService _academicMetricsService;
    private readonly UserManager<AppUser> _userManager;

    private const int PageSize = 10;

    public AdminController(
        ApplicationDbContext context,
        ICourseService courseService,
        IAdminDashboardService adminDashboardService,
        IAdminScopeService adminScopeService,
        IUniversityService universityService,
        IUniversityAdminService universityAdminService,
        IDepartmentService departmentService,
        IStudentManagementService studentManagementService,
        IAcademicMetricsService academicMetricsService,
        UserManager<AppUser> userManager)
    {
        _context = context;
        _courseService = courseService;
        _adminDashboardService = adminDashboardService;
        _adminScopeService = adminScopeService;
        _universityService = universityService;
        _universityAdminService = universityAdminService;
        _departmentService = departmentService;
        _studentManagementService = studentManagementService;
        _academicMetricsService = academicMetricsService;
        _userManager = userManager;
    }

    private async Task<(AdminScope? Scope, IActionResult? Error)> ResolveScopeAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return (null, Challenge());

        var scope = await _adminScopeService.ResolveAsync(user.Id);
        return (scope, null);
    }

    private async Task<(int UniversityId, IActionResult? Error)> RequireUniversityIdAsync()
    {
        var (scope, error) = await ResolveScopeAsync();
        if (error is not null)
            return (0, error);

        if (scope is null || !scope.CanManageCatalog)
            return (0, Forbid());

        return (scope.RequireUniversityId(), null);
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Courses(string? searchTerm, int? departmentId, bool includeInactive = false, int page = 1)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        ViewData["SearchTerm"] = searchTerm;
        ViewData["SelectedDepartmentId"] = departmentId;
        ViewData["IncludeInactive"] = includeInactive;

        var courses = await _courseService.GetAllAsync(universityId);

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

        await PopulateDepartmentsFilterDropDownListAsync(universityId, departmentId);

        var courseList = courses.ToList();
        ViewData["ActiveCount"]   = courseList.Count(c => c.IsActive);
        ViewData["InactiveCount"] = courseList.Count(c => !c.IsActive);

        var paginated = PaginatedList<CourseDto>.Create(courseList, page, PageSize);

        ViewData["PageIndex"]  = paginated.PageIndex;
        ViewData["TotalPages"] = paginated.TotalPages;
        ViewData["TotalCount"] = paginated.TotalCount;
        ViewData["PageSize"]   = paginated.PageSize;
        ViewData["PagingAction"] = nameof(Courses);
        ViewData["PagingRouteValues"] = new Dictionary<string, string?>
        {
            ["searchTerm"]      = searchTerm,
            ["departmentId"]    = departmentId?.ToString(),
            ["includeInactive"] = includeInactive ? "true" : null
        };

        return View("CourseIndex", paginated);
    }

    /// <summary>Legacy route — redirects to <see cref="Courses"/>.</summary>
    public IActionResult CourseIndex(string? searchTerm, int? departmentId, bool includeInactive = false)
        => RedirectToAction(nameof(Courses), new { searchTerm, departmentId, includeInactive });

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Students(string? searchTerm, int? departmentId, int page = 1)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        ViewData["SearchTerm"] = searchTerm;
        ViewData["SelectedDepartmentId"] = departmentId;

        var students = await _studentManagementService.GetStudentsAsync(searchTerm, departmentId, universityId);
        var studentList = students.ToList();
        ViewData["GoodStandingCount"]       = studentList.Count(s => s.CurrentStanding == Domain.Enums.AcademicStanding.Good);
        ViewData["WarningProbationCount"]   = studentList.Count(s => s.CurrentStanding == Domain.Enums.AcademicStanding.Warning
                                                                   || s.CurrentStanding == Domain.Enums.AcademicStanding.Probation);
        ViewData["DismissedCount"]          = studentList.Count(s => s.CurrentStanding == Domain.Enums.AcademicStanding.Dismissed);

        await PopulateDepartmentsFilterDropDownListAsync(universityId, departmentId);

        var paginated = PaginatedList<AppUser>.Create(studentList, page, PageSize);

        ViewData["PageIndex"]  = paginated.PageIndex;
        ViewData["TotalPages"] = paginated.TotalPages;
        ViewData["TotalCount"] = paginated.TotalCount;
        ViewData["PageSize"]   = paginated.PageSize;
        ViewData["PagingAction"] = nameof(Students);
        ViewData["PagingRouteValues"] = new Dictionary<string, string?>
        {
            ["searchTerm"]   = searchTerm,
            ["departmentId"] = departmentId?.ToString()
        };

        return View("Students/Index", paginated);
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

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Profile()
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        var adminUser = await _userManager.GetUserAsync(User);
        var email = adminUser?.Email ?? User.Identity?.Name ?? "admin";
        var userName = adminUser?.UserName ?? email;
        var userId = adminUser?.Id ?? string.Empty;

        var namePart = userName.Split('@')[0];
        var nameParts = namePart.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var displayName = nameParts.Length > 0
            ? string.Join(" ", nameParts.Select(p => p.Length > 0 ? char.ToUpper(p[0]) + p[1..].ToLower() : p))
            : userName;

        var initials = nameParts.Length >= 2
            ? $"{char.ToUpper(nameParts[0][0])}{char.ToUpper(nameParts[1][0])}"
            : displayName.Length >= 2 ? displayName[..2].ToUpper() : displayName.ToUpper();

        var dashboard = await _adminDashboardService.GetAdminDashboardAsync(universityId);
        var standing = await _studentManagementService.GetStandingSummaryAsync(universityId);

        var vm = new AdminProfileViewModel
        {
            DisplayName = displayName,
            Email = email,
            UserId = userId,
            Initials = initials,
            EmailConfirmed = adminUser?.EmailConfirmed ?? false,
            TotalStudents = standing.Total,
            TotalCourses = dashboard.TotalCourses,
            ActiveCourses = dashboard.ActiveCourses,
            InactiveCourses = dashboard.InactiveCourses,
            TotalDepartments = dashboard.TotalDepartments,
            ActiveDepartments = dashboard.ActiveDepartments,
            TotalUniversities = dashboard.TotalUniversities,
            GoodStanding = standing.Good,
            WarningOrProbation = standing.WarningOrProbation,
            Dismissed = standing.Dismissed
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AddStudent()
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        var vm = new CreateStudentViewModel
        {
            AcademicYear = $"{DateTime.Today.Year}-{DateTime.Today.Year + 1}",
            EnrollmentDate = DateTime.Today,
            CurrentSemester = SemesterType.Fall
        };
        await PopulateCreateStudentFormAsync(vm, universityId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AddStudent(CreateStudentViewModel vm)
    {
        var (universityId, scopeError) = await RequireUniversityIdAsync();
        if (scopeError is not null)
            return scopeError;

        if (!ModelState.IsValid)
        {
            await PopulateCreateStudentFormAsync(vm, universityId);
            return View(vm);
        }

        var result = await _studentManagementService.CreateStudentAsync(new CreateStudentRequest
        {
            Email = vm.Email,
            Password = vm.Password,
            DepartmentId = vm.DepartmentId,
            AcademicYear = vm.AcademicYear,
            CurrentSemester = vm.CurrentSemester,
            EnrollmentDate = vm.EnrollmentDate
        }, universityId);

        if (!result.IsSuccess)
        {
            if (!string.IsNullOrEmpty(result.Field))
            {
                var modelField = result.Field switch
                {
                    nameof(CreateStudentRequest.Email) => nameof(vm.Email),
                    nameof(CreateStudentRequest.DepartmentId) => nameof(vm.DepartmentId),
                    _ => result.Field
                };
                foreach (var error in result.Errors)
                    ModelState.AddModelError(modelField, error);
            }
            else
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);
            }

            await PopulateCreateStudentFormAsync(vm, universityId);
            return View(vm);
        }

        TempData["StatusMessage"] = $"Student \u201c{result.DisplayName}\u201d created successfully.";
        return RedirectToAction(nameof(Students));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteStudent(string id)
    {
        var (universityId, scopeError) = await RequireUniversityIdAsync();
        if (scopeError is not null)
            return scopeError;

        var result = await _studentManagementService.DeleteStudentAsync(id, universityId);

        if (result.IsSuccess)
        {
            TempData["StatusMessage"] = $"Student \u201c{result.DisplayName}\u201d deleted successfully.";
        }
        else if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
        {
            return NotFound();
        }
        else
        {
            TempData["ErrorMessage"] = "Unable to delete student: " + string.Join(", ", result.Errors);
        }

        return RedirectToAction(nameof(Students));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> EditStudent(string? id)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        if (string.IsNullOrEmpty(id))
            return RedirectToAction(nameof(Students));

        var student = await _context.Users
            .Include(u => u.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.UniversityId == universityId);

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

        await PopulateEditStudentFormAsync(vm, universityId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> EditStudent(EditStudentViewModel vm)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        if (!ModelState.IsValid)
        {
            await PopulateEditStudentFormAsync(vm, universityId);
            return View(vm);
        }

        var student = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == vm.Id && u.UniversityId == universityId);

        if (student is null)
            return NotFound();

        if (!await _courseService.DepartmentBelongsToUniversityAsync(vm.DepartmentId, universityId))
        {
            ModelState.AddModelError(nameof(vm.DepartmentId), "Please select a valid department.");
            await PopulateEditStudentFormAsync(vm, universityId);
            return View(vm);
        }

        student.DepartmentId = vm.DepartmentId;
        student.UniversityId = universityId;
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
            await PopulateEditStudentFormAsync(vm, universityId);
            return View(vm);
        }
    }

    // ── Student Detail ────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> StudentDetail(string? id)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        if (string.IsNullOrEmpty(id))
            return RedirectToAction(nameof(Students));

        var student = await _studentManagementService.GetStudentDetailAsync(id, universityId);
        if (student is null)
            return NotFound();

        return View("Students/Detail", student);
    }

    // ── StudentAddCourse ──────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> StudentAddCourse(string? id)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        if (string.IsNullOrEmpty(id))
            return RedirectToAction(nameof(Students));

        var student = await _studentManagementService.GetStudentDetailAsync(id, universityId);
        if (student is null)
            return NotFound();

        var vm = new AddCourseRecordViewModel
        {
            StudentId = id,
            StudentName = student.DisplayName,
            AcademicYear = DateTime.Today.Year + "-" + (DateTime.Today.Year + 1)
        };

        await PopulateStudentCourseFormAsync(vm, student.DepartmentId, universityId);
        return View("Students/AddCourse", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> StudentAddCourse(AddCourseRecordViewModel vm)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        var scopedStudent = await _studentManagementService.GetStudentDetailAsync(vm.StudentId, universityId);
        if (scopedStudent is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(vm.Grade) && !IsKnownGrade(vm.Grade))
            ModelState.AddModelError(nameof(vm.Grade), "Grade must be one of: A+, A, A-, B+, B, B-, C+, C, C-, D+, D, D-, F.");

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
            await PopulateStudentCourseFormAsync(vm, scopedStudent.DepartmentId, universityId);
            return View("Students/AddCourse", vm);
        }

        try
        {
            await _studentManagementService.AddCourseRecordAsync(
                vm.StudentId,
                vm.CourseId,
                vm.Grade,
                StudentCourseStatus.InProgress,
                vm.Semester,
                vm.AcademicYear.Trim());

            TempData["StatusMessage"] = "Course record added successfully.";
            return RedirectToAction(nameof(StudentDetail), new { id = vm.StudentId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Unable to add course record.";
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateStudentCourseFormAsync(vm, scopedStudent.DepartmentId, universityId);
            return View("Students/AddCourse", vm);
        }
    }

    // ── StudentEditCourse ─────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> StudentEditCourse(int? id)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        if (id is null)
            return RedirectToAction(nameof(Students));

        var record = await _context.StudentCourses
            .Include(sc => sc.Course)
            .Include(sc => sc.Student)
            .AsNoTracking()
            .FirstOrDefaultAsync(sc =>
                sc.Id == id.Value && sc.Student != null && sc.Student.UniversityId == universityId);

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

        await PopulateStudentCourseFormAsync(vm, record.Student?.DepartmentId, universityId);
        return View("Students/EditCourse", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> StudentEditCourse(EditCourseRecordViewModel vm)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        var scopedStudent = await _studentManagementService.GetStudentDetailAsync(vm.StudentId, universityId);
        if (scopedStudent is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(vm.Grade) && !IsKnownGrade(vm.Grade))
            ModelState.AddModelError(nameof(vm.Grade), "Grade must be one of: A+, A, A-, B+, B, B-, C+, C, C-, D+, D, D-, F.");

        if (ModelState.IsValid)
        {
            var existingRecord = await _context.StudentCourses
                .AsNoTracking()
                .FirstOrDefaultAsync(sc => sc.Id == vm.RecordId && sc.StudentId == vm.StudentId);

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
            await PopulateStudentCourseFormAsync(vm, scopedStudent.DepartmentId, universityId);
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
            await PopulateStudentCourseFormAsync(vm, scopedStudent.DepartmentId, universityId);
            return View("Students/EditCourse", vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> StudentDeleteCourse(int id)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        var record = await _context.StudentCourses
            .Include(sc => sc.Student)
            .FirstOrDefaultAsync(sc =>
                sc.Id == id && sc.Student != null && sc.Student.UniversityId == universityId);
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
        var (scope, error) = await ResolveScopeAsync();
        if (error is not null)
            return error;

        if (scope!.IsSuperAdmin)
        {
            var platformDashboard = await _adminDashboardService.GetAdminDashboardAsync();
            ViewData["UniversityAdminCount"] = await _universityAdminService.CountAsync();
            return View("SuperAdminIndex", platformDashboard);
        }

        if (!scope.CanManageCatalog)
            return Forbid();

        var dashboard = await _adminDashboardService.GetAdminDashboardAsync(scope.UniversityId);
        return View(dashboard);
    }

    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> UniversityAdmins(int? universityId, int page = 1)
    {
        ViewData["SelectedUniversityId"] = universityId;

        var admins = await _universityAdminService.GetAdminsAsync(universityId);
        var paginated = PaginatedList<UniversityAdminDto>.Create(admins, page, PageSize);

        ViewData["PageIndex"] = paginated.PageIndex;
        ViewData["TotalPages"] = paginated.TotalPages;
        ViewData["TotalCount"] = paginated.TotalCount;
        ViewData["PageSize"] = paginated.PageSize;
        ViewData["PagingAction"] = nameof(UniversityAdmins);
        ViewData["PagingRouteValues"] = new Dictionary<string, string?>
        {
            ["universityId"] = universityId?.ToString()
        };

        await PopulateUniversityFilterDropDownListAsync(universityId);
        return View(paginated);
    }

    [HttpGet]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> UniversityAdminCreate(int? universityId)
    {
        var vm = new CreateUniversityAdminViewModel
        {
            UniversityId = universityId ?? 0
        };
        await PopulateUniversityAdminFormAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> UniversityAdminCreate(CreateUniversityAdminViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUniversityAdminFormAsync(vm);
            return View(vm);
        }

        var result = await _universityAdminService.CreateAdminAsync(new CreateUniversityAdminRequest
        {
            Email = vm.Email,
            Password = vm.Password,
            UniversityId = vm.UniversityId
        });

        if (!result.IsSuccess)
        {
            if (!string.IsNullOrEmpty(result.Field))
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(result.Field, err);
            }
            else
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err);
            }

            await PopulateUniversityAdminFormAsync(vm);
            return View(vm);
        }

        TempData["StatusMessage"] = $"University admin \u201c{result.DisplayName}\u201d created successfully.";
        return RedirectToAction(nameof(UniversityAdmins), new { universityId = vm.UniversityId });
    }

    [HttpGet]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> UniversityAdminEdit(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return RedirectToAction(nameof(UniversityAdmins));

        var admin = (await _universityAdminService.GetAdminsAsync())
            .FirstOrDefault(a => a.Id == id);

        if (admin is null)
            return NotFound();

        var vm = new EditUniversityAdminViewModel
        {
            Id = admin.Id,
            Email = admin.Email,
            DisplayName = admin.DisplayName,
            UniversityId = admin.UniversityId
        };
        await PopulateUniversityAdminEditFormAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> UniversityAdminEdit(EditUniversityAdminViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUniversityAdminEditFormAsync(vm);
            return View(vm);
        }

        var result = await _universityAdminService.UpdateUniversityAsync(vm.Id, vm.UniversityId);
        if (!result.IsSuccess)
        {
            if (!string.IsNullOrEmpty(result.Field))
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(result.Field, err);
            }
            else
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err);
            }

            await PopulateUniversityAdminEditFormAsync(vm);
            return View(vm);
        }

        TempData["StatusMessage"] = $"University assignment updated for \u201c{result.DisplayName}\u201d.";
        return RedirectToAction(nameof(UniversityAdmins), new { universityId = vm.UniversityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> UniversityAdminDelete(string id)
    {
        var result = await _universityAdminService.DeleteAdminAsync(id);

        if (result.IsSuccess)
            TempData["StatusMessage"] = $"University admin \u201c{result.DisplayName}\u201d deleted.";
        else if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            return NotFound();
        else
            TempData["ErrorMessage"] = string.Join(", ", result.Errors);

        return RedirectToAction(nameof(UniversityAdmins));
    }

    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> UniversityIndex(int page = 1)
    {
        var universities = await _universityService.GetAllAsync();
        var paginated    = PaginatedList<UniversityDto>.Create(universities, page, PageSize);

        ViewData["PageIndex"]  = paginated.PageIndex;
        ViewData["TotalPages"] = paginated.TotalPages;
        ViewData["TotalCount"] = paginated.TotalCount;
        ViewData["PageSize"]   = paginated.PageSize;
        ViewData["PagingAction"]      = nameof(UniversityIndex);
        ViewData["PagingRouteValues"] = new Dictionary<string, string?>();

        return View(paginated);
    }

    [HttpGet]
    [Authorize(Roles = Roles.SuperAdmin)]
    public IActionResult UniversityCreate()
    {
        return View(new CreateUniversityDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SuperAdmin)]
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

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DepartmentIndex(int page = 1)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        var departments = await _departmentService.GetAllAsync(universityId);
        var deptList    = departments.ToList();
        ViewData["ActiveCount"]   = deptList.Count(d => d.IsActive);
        ViewData["InactiveCount"] = deptList.Count(d => !d.IsActive);

        var paginated   = PaginatedList<DepartmentDto>.Create(deptList, page, PageSize);

        ViewData["PageIndex"]  = paginated.PageIndex;
        ViewData["TotalPages"] = paginated.TotalPages;
        ViewData["TotalCount"] = paginated.TotalCount;
        ViewData["PageSize"]   = paginated.PageSize;
        ViewData["PagingAction"]      = nameof(DepartmentIndex);
        ViewData["PagingRouteValues"] = new Dictionary<string, string?>();

        return View(paginated);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DepartmentCreate()
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        return View(new CreateDepartmentDto { UniversityId = universityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DepartmentCreate([Bind("Name,UniversityId,TotalCreditsRequired,MinGpaForGraduation,IsActive")] CreateDepartmentDto department)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        department.Name = department.Name?.Trim() ?? string.Empty;
        department.UniversityId = universityId;

        if (await IsDepartmentNameDuplicateAsync(department.UniversityId, department.Name))
        {
            ModelState.AddModelError(nameof(Department.Name), "Department name must be unique within the selected university.");
        }

        if (!ModelState.IsValid)
        {
            return View(department);
        }

        try
        {
            await _departmentService.AddAsync(department, universityId);
            TempData["StatusMessage"] = "Department created successfully.";
            return RedirectToAction(nameof(DepartmentIndex));
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Unable to create department.";
            ModelState.AddModelError(string.Empty, "Unable to save department. Ensure department name is unique within the selected university.");
            return View(department);
        }
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DepartmentEdit(int id)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        var department = await _departmentService.GetByIdAsync(id, universityId);
        if (department is null)
        {
            return NotFound();
        }

        var model = new EditDepartmentDto()
        {
            Id = department.Id,
            Name = department.Name,
            UniversityId = universityId,
            TotalCreditsRequired = department.TotalCreditsRequired,
            MinGpaForGraduation = department.MinGpaForGraduation,
            IsActive = department.IsActive
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DepartmentEdit(int id, [Bind("Id,Name,UniversityId,TotalCreditsRequired,MinGpaForGraduation,IsActive")] EditDepartmentDto department)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        if (id != department.Id)
        {
            return NotFound();
        }

        department.Name = department.Name?.Trim() ?? string.Empty;
        department.UniversityId = universityId;

        if (await IsDepartmentNameDuplicateAsync(department.UniversityId, department.Name, department.Id))
        {
            ModelState.AddModelError(nameof(Department.Name), "Department name must be unique within the selected university.");
        }

        if (!ModelState.IsValid)
        {
            return View(department);
        }

        try
        {
            await _departmentService.UpdateAsync(department, universityId);
            TempData["StatusMessage"] = "Department updated successfully.";
            return RedirectToAction(nameof(DepartmentIndex));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await _departmentService.ExistsAsync(department.Id, universityId);
            if (!exists)
                return NotFound();

            throw;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Unable to update department.";
            ModelState.AddModelError(string.Empty, "Unable to save department. Ensure department name is unique within the selected university.");
            return View(department);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DepartmentDeactivate(int id)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        var department = await _departmentService.GetByIdAsync(id, universityId);

        if (department is null)
        {
            return NotFound();
        }

        if (!department.IsActive)
        {
            TempData["StatusMessage"] = "Department is already inactive.";
            return RedirectToAction(nameof(DepartmentIndex));
        }

        await _departmentService.ToggleActiveAsync(id, universityId);

        TempData["StatusMessage"] = "Department deactivated successfully.";
        return RedirectToAction(nameof(DepartmentIndex));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CourseCreate()
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        await PopulateDepartmentsDropDownListAsync(universityId);
        return View(new CreateCourseDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CourseCreate([Bind("Code,Name,CreditHours,CourseType,SemesterAvailability,PassingGradeThreshold,DepartmentId,IsActive")] CreateCourseDto course)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

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
            await PopulateDepartmentsDropDownListAsync(universityId, course.DepartmentId);
            return View(course);
        }

        try
        {
            await _courseService.AddAsync(course, universityId);
            TempData["StatusMessage"] = "Course created successfully.";
            return RedirectToAction(nameof(Courses));
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            ModelState.AddModelError(nameof(Course.DepartmentId), ex.Message);
            await PopulateDepartmentsDropDownListAsync(universityId, course.DepartmentId);
            return View(course);
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Unable to create course.";
            ModelState.AddModelError(string.Empty, "Unable to save course. Ensure course code is unique within the selected department.");
            await PopulateDepartmentsDropDownListAsync(universityId, course.DepartmentId);
            return View(course);
        }
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CourseEdit(int id)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        var course = await _courseService.GetByIdAsync(id, universityId);
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

        await PopulateDepartmentsDropDownListAsync(universityId, course.DepartmentId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CourseEdit(int id, [Bind("Id,Code,Name,CreditHours,CourseType,SemesterAvailability,PassingGradeThreshold,DepartmentId,IsActive")] EditCourseDto course)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

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
            await PopulateDepartmentsDropDownListAsync(universityId, course.DepartmentId);
            return View(course);
        }

        try
        {
            await _courseService.UpdateAsync(course, universityId);
            TempData["StatusMessage"] = "Course updated successfully.";
            return RedirectToAction(nameof(Courses));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(Course.DepartmentId), ex.Message);
            await PopulateDepartmentsDropDownListAsync(universityId, course.DepartmentId);
            return View(course);
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await _courseService.ExistsAsync(course.Id, universityId);
            if (!exists)
                return NotFound();

            throw;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Unable to update course.";
            ModelState.AddModelError(string.Empty, "Unable to save course. Ensure course code is unique within the selected department.");
            await PopulateDepartmentsDropDownListAsync(universityId, course.DepartmentId);
            return View(course);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CourseDeactivate(int id, string? searchTerm, int? departmentId, bool includeInactive = false)
    {
        var (universityId, error) = await RequireUniversityIdAsync();
        if (error is not null)
            return error;

        var course = await _courseService.GetByIdAsync(id, universityId);

        if (course is null)
        {
            return NotFound();
        }

        await _courseService.ToggleActiveAsync(id, universityId);

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
        CourseRecordFormBase vm, int? departmentId = null, int? universityId = null)
    {
        var coursesQuery = _context.Courses
            .Include(c => c.Department)
            .Where(c => c.IsActive)
            .AsNoTracking()
            .AsQueryable();

        if (universityId.HasValue)
            coursesQuery = coursesQuery.Where(c => c.Department!.UniversityId == universityId.Value);

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

        vm.GradeOptions =
        [
            new SelectListItem("— No grade (In Progress) —", ""),
            .. KnownGrades.Select(g => new SelectListItem(g, g))
        ];

        vm.SemesterOptions = Enum.GetValues<SemesterType>()
            .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString()));
    }

    private async Task PopulateEditStudentFormAsync(EditStudentViewModel vm, int universityId)
    {
        var departments = await _context.Departments
            .Include(d => d.University)
            .Where(d => d.UniversityId == universityId)
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

    private async Task PopulateCreateStudentFormAsync(CreateStudentViewModel vm, int universityId)
    {
        var departments = await _context.Departments
            .Include(d => d.University)
            .Where(d => d.IsActive && d.UniversityId == universityId)
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();

        vm.DepartmentOptions = departments.Select(d => new SelectListItem(
            d.University is null ? d.Name : $"{d.Name} ({d.University.Name})",
            d.Id.ToString()));

        vm.SemesterOptions = Enum.GetValues<SemesterType>()
            .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString()));
    }

    private async Task PopulateDepartmentsDropDownListAsync(int universityId, int? selectedDepartment = null)
    {
        var departments = (await _departmentService.GetAllAsync(universityId, isActive: true)).ToList();

        if (selectedDepartment.HasValue && selectedDepartment.Value > 0 &&
            !departments.Any(d => d.Id == selectedDepartment.Value))
        {
            var inactiveDepartment = await _departmentService.GetByIdAsync(selectedDepartment.Value, universityId);

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

    private async Task PopulateDepartmentsFilterDropDownListAsync(int universityId, int? selectedDepartment = null)
    {
        var departments = (await _departmentService.GetAllAsync(universityId)).ToList();

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

    private async Task PopulateUniversityAdminFormAsync(CreateUniversityAdminViewModel vm)
    {
        var universities = await _universityService.GetAllAsync();
        vm.UniversityOptions = universities.Select(u => new SelectListItem(u.Name, u.Id.ToString()));
    }

    private async Task PopulateUniversityAdminEditFormAsync(EditUniversityAdminViewModel vm)
    {
        var universities = await _universityService.GetAllAsync();
        vm.UniversityOptions = universities.Select(u => new SelectListItem(u.Name, u.Id.ToString()));
    }

    private async Task PopulateUniversityFilterDropDownListAsync(int? selectedUniversity = null)
    {
        var universities = await _universityService.GetAllAsync();
        ViewData["FilterUniversityId"] = new SelectList(universities, "Id", "Name", selectedUniversity);
    }
}
