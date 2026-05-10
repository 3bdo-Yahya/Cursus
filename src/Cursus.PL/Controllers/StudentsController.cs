using Cursus.BLL;
using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.PL.Constants;
using Cursus.PL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Cursus.PL.Controllers;

[Authorize(Roles = Roles.Admin)]
[Route("Admin/[controller]")]
public class StudentsController : Controller
{
    private const int DefaultPageSize = 10;

    private readonly IStudentManagementService _studentManagementService;
    private readonly ApplicationDbContext _context;

    public StudentsController(IStudentManagementService studentManagementService, ApplicationDbContext context)
    {
        _studentManagementService = studentManagementService;
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? searchTerm, int? departmentId, int pageNumber = 1)
    {
        await PopulateDepartmentsFilterDropDownListAsync(departmentId);

        var students = await _studentManagementService.GetAllStudentsAsync(departmentId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim();
            students = students
                .Where(student =>
                    student.FullName.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    student.Email.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        const int pageSize = DefaultPageSize;
        var totalCount = students.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var currentPage = Math.Clamp(pageNumber, 1, totalPages);

        var pagedStudents = students
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new StudentListIndexViewModel
        {
            Students = pagedStudents,
            SearchTerm = searchTerm,
            SelectedDepartmentId = departmentId,
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return View("~/Views/Admin/Students/Index.cshtml", model);
    }

    [HttpGet("Details/{studentId}")]
    public async Task<IActionResult> Details(string studentId)
    {
        var student = await _studentManagementService.GetStudentDetailAsync(studentId);
        if (student is null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Students/Detail.cshtml", student);
    }

    [HttpGet("AddCourse/{studentId}")]
    public async Task<IActionResult> AddCourse(string studentId)
    {
        var student = await _studentManagementService.GetStudentDetailAsync(studentId);
        if (student is null)
        {
            return NotFound();
        }

        var model = new StudentCourseFormViewModel
        {
            StudentId = student.StudentId,
            StudentName = student.FullName,
            DepartmentId = student.DepartmentId,
            DepartmentName = student.DepartmentName,
            Semester = student.CurrentSemester,
            AcademicYear = student.AcademicYear ?? string.Empty,
            CourseOptions = await GetCourseOptionsAsync(student.DepartmentId, null),
            GradeOptions = GetGradeOptions(null)
        };

        return View("~/Views/Admin/Students/AddCourse.cshtml", model);
    }

    [HttpPost("AddCourse/{studentId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCourse(string studentId, StudentCourseFormViewModel model)
    {
        var student = await _studentManagementService.GetStudentDetailAsync(studentId);
        if (student is null)
        {
            return NotFound();
        }

        model.StudentId = student.StudentId;
        model.StudentName = student.FullName;
        model.DepartmentId = student.DepartmentId;
        model.DepartmentName = student.DepartmentName;

        if (student.DepartmentId is null)
        {
            ModelState.AddModelError(string.Empty, "Student must belong to a department before course records can be added.");
        }

        if (!model.CourseId.HasValue)
        {
            ModelState.AddModelError(nameof(model.CourseId), "Please select a course.");
        }

        if (!model.Semester.HasValue)
        {
            ModelState.AddModelError(nameof(model.Semester), "Please select a semester.");
        }

        if (model.CourseId.HasValue && student.DepartmentId.HasValue)
        {
            var courseExists = await _context.Courses.AsNoTracking().AnyAsync(course =>
                course.Id == model.CourseId.Value &&
                course.DepartmentId == student.DepartmentId);

            if (!courseExists)
            {
                ModelState.AddModelError(nameof(model.CourseId), "Please select a course from the student's department catalog.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.CourseOptions = await GetCourseOptionsAsync(student.DepartmentId, model.CourseId);
            model.GradeOptions = GetGradeOptions(model.Grade);
            return View("~/Views/Admin/Students/AddCourse.cshtml", model);
        }

        var result = await _studentManagementService.AddCourseRecordAsync(
            studentId,
            model.CourseId!.Value,
            model.Grade,
            model.Status ?? StudentCourseStatus.InProgress,
            model.Semester!.Value,
            model.AcademicYear);

        if (!result.Succeeded)
        {
            ApplyMutationErrors(result, model);
            model.CourseOptions = await GetCourseOptionsAsync(student.DepartmentId, model.CourseId);
            model.GradeOptions = GetGradeOptions(model.Grade);
            return View("~/Views/Admin/Students/AddCourse.cshtml", model);
        }

        TempData["StatusMessage"] = result.Message ?? "Course record added successfully.";
        return RedirectToAction(nameof(Details), new { studentId });
    }

    [HttpGet("EditCourse/{recordId:int}")]
    public async Task<IActionResult> EditCourse(int recordId)
    {
        var record = await _context.StudentCourses
            .AsNoTracking()
            .Include(studentCourse => studentCourse.Student)
                .ThenInclude(student => student!.Department)
            .Include(studentCourse => studentCourse.Course)
            .FirstOrDefaultAsync(studentCourse => studentCourse.Id == recordId);

        if (record is null || record.Student is null)
        {
            return NotFound();
        }

        var model = new StudentCourseFormViewModel
        {
            RecordId = record.Id,
            StudentId = record.StudentId,
            StudentName = BuildDisplayName(record.Student),
            CourseDisplayName = record.Course is null ? null : $"{record.Course.Code} - {record.Course.Name}",
            DepartmentId = record.Student.DepartmentId,
            DepartmentName = record.Student.Department?.Name,
            CourseId = record.CourseId,
            Semester = record.Semester,
            AcademicYear = record.AcademicYear,
            Grade = record.Grade,
            Status = record.Status,
            CourseOptions = await GetCourseOptionsAsync(record.Student.DepartmentId, record.CourseId),
            GradeOptions = GetGradeOptions(record.Grade)
        };

        return View("~/Views/Admin/Students/EditCourse.cshtml", model);
    }

    [HttpPost("EditCourse/{recordId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCourse(int recordId, StudentCourseFormViewModel model)
    {
        var existingRecord = await _context.StudentCourses
            .AsNoTracking()
            .Include(studentCourse => studentCourse.Student)
                .ThenInclude(student => student!.Department)
            .FirstOrDefaultAsync(studentCourse => studentCourse.Id == recordId);

        if (existingRecord is null || existingRecord.Student is null)
        {
            return NotFound();
        }

        model.RecordId = recordId;
        model.StudentId = existingRecord.StudentId;
        model.StudentName = BuildDisplayName(existingRecord.Student);
        model.DepartmentId = existingRecord.Student.DepartmentId;
        model.DepartmentName = existingRecord.Student.Department?.Name;
        model.CourseId = existingRecord.CourseId;
        model.Semester = existingRecord.Semester;
        model.AcademicYear = existingRecord.AcademicYear;

        if (!ModelState.IsValid)
        {
            model.CourseOptions = await GetCourseOptionsAsync(existingRecord.Student.DepartmentId, existingRecord.CourseId);
            model.GradeOptions = GetGradeOptions(model.Grade ?? existingRecord.Grade);
            return View("~/Views/Admin/Students/EditCourse.cshtml", model);
        }

        var result = await _studentManagementService.UpdateCourseRecordAsync(
            recordId,
            model.Grade,
            model.Status ?? existingRecord.Status);

        if (!result.Succeeded)
        {
            ApplyMutationErrors(result, model);
            model.CourseOptions = await GetCourseOptionsAsync(existingRecord.Student.DepartmentId, existingRecord.CourseId);
            model.GradeOptions = GetGradeOptions(model.Grade ?? existingRecord.Grade);
            return View("~/Views/Admin/Students/EditCourse.cshtml", model);
        }

        TempData["StatusMessage"] = result.Message ?? "Course record updated successfully.";
        return RedirectToAction(nameof(Details), new { studentId = existingRecord.StudentId });
    }

    [HttpPost("DeleteCourse/{recordId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(int recordId, string studentId)
    {
        var result = await _studentManagementService.DeleteCourseRecordAsync(recordId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message ?? "Unable to delete the course record.";
            return RedirectToAction(nameof(Details), new { studentId });
        }

        TempData["StatusMessage"] = result.Message ?? "Course record deleted successfully.";
        return RedirectToAction(nameof(Details), new { studentId = result.StudentId ?? studentId });
    }

    private async Task PopulateDepartmentsFilterDropDownListAsync(int? selectedDepartment = null)
    {
        var departments = await _context.Departments
            .Include(department => department.University)
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .ToListAsync();

        var departmentOptions = departments
            .Select(department => new
            {
                department.Id,
                DisplayName = department.University is null
                    ? department.Name
                    : $"{department.Name} ({department.University.Name})"
            })
            .ToList();

        ViewData["DepartmentOptions"] = new SelectList(departmentOptions, "Id", "DisplayName", selectedDepartment);
    }

    private async Task<IEnumerable<SelectListItem>> GetCourseOptionsAsync(int? departmentId, int? selectedCourseId)
    {
        if (!departmentId.HasValue)
        {
            return [];
        }

        var courses = await _context.Courses
            .AsNoTracking()
            .Where(course => course.DepartmentId == departmentId.Value && course.IsActive)
            .OrderBy(course => course.Code)
            .ToListAsync();

        return courses.Select(course => new SelectListItem
        {
            Value = course.Id.ToString(),
            Text = $"{course.Code} - {course.Name}",
            Selected = selectedCourseId.HasValue && selectedCourseId.Value == course.Id
        });
    }

    private static IEnumerable<SelectListItem> GetGradeOptions(string? selectedGrade)
    {
        var options = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "In Progress", Selected = string.IsNullOrWhiteSpace(selectedGrade) }
        };

        options.AddRange(GradeScaleCatalog.LetterGrades.Select(grade => new SelectListItem
        {
            Value = grade,
            Text = grade,
            Selected = string.Equals(selectedGrade, grade, StringComparison.OrdinalIgnoreCase)
        }));

        return options;
    }

    private static string BuildDisplayName(AppUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.UserName) && user.UserName.Contains('@'))
        {
            return user.UserName.Split('@')[0];
        }

        return user.UserName ?? user.Email ?? user.Id;
    }

    private void ApplyMutationErrors(StudentCourseMutationResult result, StudentCourseFormViewModel model)
    {
        switch (result.Error)
        {
            case StudentCourseMutationError.DuplicateRecord:
                ModelState.AddModelError(nameof(model.CourseId), result.Message ?? "The course record already exists.");
                break;
            case StudentCourseMutationError.InvalidGrade:
                ModelState.AddModelError(nameof(model.Grade), result.Message ?? "Invalid grade selected.");
                break;
            case StudentCourseMutationError.CourseNotFound:
            case StudentCourseMutationError.CourseNotInStudentDepartment:
                ModelState.AddModelError(nameof(model.CourseId), result.Message ?? "Invalid course selected.");
                break;
            case StudentCourseMutationError.InvalidAcademicYear:
                ModelState.AddModelError(nameof(model.AcademicYear), result.Message ?? "Academic year is required.");
                break;
            case StudentCourseMutationError.StudentNotFound:
            case StudentCourseMutationError.RecordNotFound:
                ModelState.AddModelError(string.Empty, result.Message ?? "The requested record was not found.");
                break;
        }
    }
}
