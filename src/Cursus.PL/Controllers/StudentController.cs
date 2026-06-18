using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Cursus.Domain.Constants;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Interfaces.Services;
using Cursus.PL.Models;

namespace Cursus.PL.Controllers;

[Authorize(Roles = Roles.Student)]
public class StudentController : Controller
{
    private readonly UserManager<AppUser>     _userManager;
    private readonly IStudentDashboardService _dashboardService;

    public StudentController(
        UserManager<AppUser>     userManager,
        IStudentDashboardService dashboardService)
    {
        _userManager      = userManager;
        _dashboardService = dashboardService;
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

    public IActionResult CourseMap()      => View();
    public IActionResult Planner()        => View();
    public IActionResult Progress()       => View();
    public IActionResult AiAdvisor()      => View();
    public IActionResult GpaSimulator()   => View();
    public IActionResult ImpactAnalyzer() => View();

    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        var username = user?.UserName?.Split('@').FirstOrDefault() ?? "Student";
        ViewData["StudentName"]  = username;
        ViewData["StudentEmail"] = user?.Email ?? "";
        ViewData["Initials"]     = GetInitials(username);
        return View();
    }

    private static StudentDashboardViewModel MapToViewModel(StudentDashboardDto dto)
    {
        var semesterLabel = FormatSemester(dto.CurrentSemester, dto.AcademicYear);
        var standingLabel = FormatStanding(dto.Standing);

        return new StudentDashboardViewModel
        {
            StudentName      = dto.DisplayName,
            Initials         = GetInitials(dto.DisplayName),
            Department       = dto.DepartmentName,
            Year             = ResolveAcademicYearNumber(dto.AcademicYear),
            Semester         = semesterLabel,
            AcademicStanding = standingLabel,

            Cgpa       = (double)dto.Cgpa,
            MaxGpa     = 4.0,
            CgpaChange = (double)dto.CgpaChange,

            CreditsEarned   = dto.CreditsCompleted,
            CreditsRequired = dto.CreditsRequired,

            CoursesRemaining         = dto.CoursesRemaining,
            CoreCoursesRemaining     = dto.CoreCoursesRemaining,
            ElectiveCoursesRemaining = dto.ElectiveCoursesRemaining,
            UniReqCoursesRemaining   = dto.UniReqCoursesRemaining,

            GraduationSemester = dto.ProjectedGraduation,
            SemestersCompleted = dto.SemestersCompleted,
            TotalSemesters     = dto.TotalSemesters,

            CurrentCourses = dto.CurrentCourses
                .Select(c => new EnrolledCourseViewModel
                {
                    Code        = c.Code,
                    Name        = c.Name,
                    CreditHours = c.CreditHours,
                    IsElective  = c.IsElective,
                    Schedule    = $"{c.CreditHours} credit hours"
                })
                .ToList()
        };
    }

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }

    private static string FormatSemester(SemesterType semester, string? academicYear)
    {
        var sem = semester switch
        {
            SemesterType.Fall   => "Fall",
            SemesterType.Spring => "Spring",
            _                   => "Summer"
        };

        if (!string.IsNullOrWhiteSpace(academicYear))
        {
            var parts = academicYear.Split('-');
            var year  = semester == SemesterType.Fall && parts.Length > 0
                      ? parts[0]
                      : parts.Length > 1 ? parts[1] : parts[0];
            return $"{sem} {year}";
        }

        return $"{sem} {DateTime.UtcNow.Year}";
    }

    private static string FormatStanding(AcademicStanding standing) => standing switch
    {
        AcademicStanding.Good      => "Good Standing",
        AcademicStanding.Warning   => "Academic Warning",
        AcademicStanding.Probation => "Probation",
        AcademicStanding.Dismissed => "Dismissed",
        _                          => "Good Standing"
    };

    private static int ResolveAcademicYearNumber(string? academicYear)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
            return 1;

        var yearStr = academicYear.Split('-')[0];
        if (int.TryParse(yearStr, out var year))
        {
            var estimated = DateTime.UtcNow.Year - year + 1;
            return Math.Clamp(estimated, 1, 6);
        }

        return 1;
    }
}
