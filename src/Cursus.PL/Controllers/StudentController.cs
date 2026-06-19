using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Constants;
using Cursus.Domain.DTOs;
using Cursus.Domain.Interfaces.Services;
using Cursus.BLL.Services;
using Cursus.PL.Models;

namespace Cursus.PL.Controllers;

[Authorize(Roles = Roles.Student)]
public class StudentController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IProgressService _progressService;
    private readonly IStudentDashboardService _dashboardService;
    private readonly IAiAdvisorService _aiAdvisorService;

    public StudentController(
        UserManager<AppUser> userManager,
        IProgressService progressService,
        IStudentDashboardService dashboardService,
        IAiAdvisorService aiAdvisorService)
    {
        _userManager      = userManager;
        _progressService  = progressService;
        _dashboardService = dashboardService;
        _aiAdvisorService = aiAdvisorService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        var dto = await _dashboardService.GetDashboardDataAsync(user.Id);
        if (dto is null)
            return RedirectToAction("Login", "Account");

        if (dto.DepartmentName == "Not assigned")
            TempData["Warning"] = "Please contact your admin to assign your department.";

        var model = MapToViewModel(dto);
        return View(model);
    }

    public IActionResult CourseMap() => View();
    public IActionResult Planner() => View();
    public async Task<IActionResult> Progress()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToPage("/Identity/Account/Login");

        var audit = await _progressService.GetGraduationAuditAsync(user.Id);
        if (audit is null)
        {
            TempData["Error"] = "Your academic record could not be loaded. Please contact your advisor.";
            return RedirectToAction(nameof(Dashboard));
        }

        return View(new ProgressViewModel { Audit = audit });
    }
    public IActionResult AiAdvisor() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AiAdvisorChat(
        [FromBody] AiAdvisorChatRequest? request,
        CancellationToken cancellationToken)
    {
        var message = request?.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message) || message.Length > 2000)
        {
            return BadRequest(AiAdvisorResponseDto.Failure(
                "invalid_message",
                "Enter a message between 1 and 2000 characters."));
        }

        var studentId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized(AiAdvisorResponseDto.Failure(
                "student_not_authenticated",
                "Sign in with a student account to use the AI advisor."));
        }

        var audit = await _progressService.GetGraduationAuditAsync(studentId);
        if (audit is null)
        {
            return UnprocessableEntity(AiAdvisorResponseDto.Failure(
                "student_context_unavailable",
                "Your academic profile could not be loaded. Please contact your faculty advisor."));
        }

        var studentContext = AiAdvisorContextFactory.Create(audit);
        var response = await _aiAdvisorService.GetAdvisorResponseAsync(
            studentContext,
            message,
            cancellationToken);

        return response.Succeeded
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }

    public IActionResult GpaSimulator() => View();
    public IActionResult ImpactAnalyzer() => View();

    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        var username = user?.UserName?.Split('@').FirstOrDefault() ?? "Student";
        ViewData["StudentName"] = username;
        ViewData["StudentEmail"] = user?.Email ?? "";
        ViewData["Initials"] = GetInitials(username);
        return View();
    }

    private static StudentDashboardViewModel MapToViewModel(StudentDashboardDto dto)
    {
        return new StudentDashboardViewModel
        {
            StudentName = dto.DisplayName,
            Initials = GetInitials(dto.DisplayName),
            Department = dto.DepartmentName,
            Year = (dto.SemestersCompleted / 2) + 1,
            Semester = FormatSemester(dto.CurrentSemester, dto.AcademicYear),
            AcademicStanding = FormatStanding(dto.Standing),

            Cgpa = (double)dto.Cgpa,
            MaxGpa = 4.0,
            CgpaChange = (double)dto.CgpaChange,

            CreditsEarned = dto.CreditsCompleted,
            CreditsRequired = dto.CreditsRequired,

            CoursesRemaining = dto.CoursesRemaining,
            CoreCoursesRemaining = dto.CoreCoursesRemaining,
            ElectiveCoursesRemaining = dto.ElectiveCoursesRemaining,

            GraduationSemester = dto.ProjectedGraduation,
            SemestersCompleted = dto.SemestersCompleted,
            TotalSemesters = dto.TotalSemesters,

            CurrentCourses = dto.CurrentCourses
                .Select(c => new EnrolledCourseViewModel
                {
                    Code = c.Code,
                    Name = c.Name,
                    Schedule = $"{c.CreditHours} credit hours",
                    CreditHours = c.CreditHours,
                    IsElective = c.IsElective
                })
                .ToList()
        };
    }

    private static string FormatSemester(SemesterType semester, string? academicYear)
    {
        var semesterName = semester switch
        {
            SemesterType.Fall => "Fall",
            SemesterType.Spring => "Spring",
            _ => "Summer"
        };

        if (!string.IsNullOrWhiteSpace(academicYear))
        {
            var year = academicYear.Split('-').FirstOrDefault() ?? DateTime.UtcNow.Year.ToString();
            return $"{semesterName} {year}";
        }

        return $"{semesterName} {DateTime.UtcNow.Year}";
    }

    private static string FormatStanding(AcademicStanding standing) => standing switch
    {
        AcademicStanding.Good => "Good Standing",
        AcademicStanding.Warning => "Academic Warning",
        AcademicStanding.Probation => "Probation",
        AcademicStanding.Dismissed => "Dismissed",
        _ => "Good Standing"
    };


    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }
}
