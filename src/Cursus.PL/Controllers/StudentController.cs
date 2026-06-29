using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Constants;
using Cursus.Domain.DTOs;
using Cursus.Domain.Interfaces.Services;
using Cursus.PL.Models;
using System.Text;

namespace Cursus.PL.Controllers;

[Authorize(Roles = Roles.Student)]
public class StudentController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IProgressService _progressService;
    private readonly IStudentDashboardService _dashboardService;
    private readonly IImpactAnalysisService _impactAnalysisService;
    private readonly IGeminiService _geminiService;

    public StudentController(
        UserManager<AppUser> userManager,
        ApplicationDbContext db,
        IProgressService progressService,
        IStudentDashboardService dashboardService,
        IImpactAnalysisService impactAnalysisService,
        IGeminiService geminiService)
    {
        _userManager = userManager;
        _db = db;
        _progressService = progressService;
        _dashboardService = dashboardService;
        _impactAnalysisService = impactAnalysisService;
        _geminiService = geminiService;
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

        else if (!dto.HasAcademicRecords)
            TempData["Warning"] = "No academic records found yet. Your dashboard will populate once your admin enters your course history.";

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
    public async Task<IActionResult> AiAdvisorChat([FromBody] ChatRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Message))
            return BadRequest(new { error = "Message is required." });

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var audit = await _progressService.GetGraduationAuditAsync(user.Id);
        if (audit is null)
            return BadRequest(new { error = "Could not load student academic record." });

        try
        {
            var reply = await _geminiService.AskGeminiAsync(audit, request, cancellationToken);
            return Json(new { reply });
        }
        catch
        {
            return StatusCode(500, new { error = "AI Advisor is temporarily unavailable." });
        }
    }
    public async Task<IActionResult> GpaSimulator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        var student = await _db.Users
            .Include(u => u.Department)
            .Include(u => u.StudentCourses)
                .ThenInclude(sc => sc.Course)
            .Include(u => u.StandingHistories)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (student is null)
            return NotFound();

        // Resolve Grade Scale for their university, safe from null department
        var gradeScale = student.Department is not null
            ? await _db.GradeScales
                .AsNoTracking()
                .Where(gs => gs.UniversityId == student.Department.UniversityId)
                .ToDictionaryAsync(gs => gs.LetterGrade.ToUpper(), gs => (double)gs.PointValue)
            : new Dictionary<string, double>();

        if (gradeScale.Count == 0) // Default fallback
        {
            gradeScale = new Dictionary<string, double>
            {
                ["A+"] = 4.0,
                ["A"] = 4.0,
                ["A-"] = 3.7,
                ["B+"] = 3.3,
                ["B"] = 3.0,
                ["B-"] = 2.7,
                ["C+"] = 2.3,
                ["C"] = 2.0,
                ["C-"] = 1.7,
                ["D+"] = 1.3,
                ["D"] = 1.0,
                ["F"] = 0.0
            };
        }

        var studentCourses = student.StudentCourses.ToList();

        // Group by CourseId to filter out duplicates / retakes (Completed > InProgress > Failed)
        var studentCourseMap = studentCourses
            .GroupBy(sc => sc.CourseId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(sc => sc.Status switch
                {
                    StudentCourseStatus.Completed => 0,
                    StudentCourseStatus.Failed => 1,
                    StudentCourseStatus.InProgress => 2,
                    _ => 3
                }).First());

        var bestAttempts = studentCourseMap.Values.ToList();

        // Completed Courses (only status == Completed)
        var completedCourses = bestAttempts
            .Where(sc => sc.Status == StudentCourseStatus.Completed && sc.Course is not null)
            .ToList();

        var completedCredits = completedCourses.Sum(sc => sc.Course!.CreditHours);

        // Graded Courses (Completed or Failed best attempts with grades)
        var gradedCourses = bestAttempts
            .Where(sc => (sc.Status == StudentCourseStatus.Completed || sc.Status == StudentCourseStatus.Failed)
                         && !string.IsNullOrWhiteSpace(sc.Grade) && sc.Course is not null)
            .ToList();

        var gpaHours = gradedCourses.Sum(sc => sc.Course!.CreditHours);

        // Calculate completed Quality Points (QP)
        double completedQp = gradedCourses
            .Sum(sc => (gradeScale.TryGetValue(sc.Grade?.Trim().ToUpper() ?? "", out var pts) ? pts : 0.0) * sc.Course!.CreditHours);

        // Current In-Progress Courses
        var currentCourses = studentCourses
            .Where(sc => sc.Status == StudentCourseStatus.InProgress && sc.Course is not null)
            .Select(sc =>
            {
                bool isRetake = studentCourseMap.TryGetValue(sc.CourseId, out var bestAttempt)
                                && (bestAttempt.Status == StudentCourseStatus.Completed || bestAttempt.Status == StudentCourseStatus.Failed)
                                && !string.IsNullOrWhiteSpace(bestAttempt.Grade);
                return new SimulatedCourseViewModel
                {
                    Id = sc.Course!.Code,
                    Name = sc.Course.Name,
                    Credits = sc.Course.CreditHours,
                    IsRetake = isRetake,
                    OriginalGrade = isRetake ? bestAttempt!.Grade! : string.Empty,
                    OriginalPoints = isRetake && gradeScale.TryGetValue(bestAttempt!.Grade!.ToUpper(), out var pts) ? pts : 0.0
                };
            })
            .ToList();

        // Improvable Courses
        var inProgressCourseIds = studentCourses
            .Where(sc => sc.Status == StudentCourseStatus.InProgress)
            .Select(sc => sc.CourseId)
            .ToHashSet();


        var improvableCourses = bestAttempts
            .Where(sc => (sc.Status == StudentCourseStatus.Failed || sc.Grade == "D" || sc.Grade == "D+")
                && sc.Course is not null
                && !inProgressCourseIds.Contains(sc.CourseId))

            .Select(sc => new ImprovableCourseViewModel
            {
                Id = sc.Course!.Code,
                Name = sc.Course.Name,
                Credits = sc.Course.CreditHours,
                OriginalGrade = sc.Grade!,
                OriginalPoints = gradeScale.TryGetValue(sc.Grade!.ToUpper(), out var pts) ? pts : 0.0
            })
            .ToList();

        var latestStanding = student.StandingHistories
            .OrderByDescending(h => h.AcademicYear)
            .ThenByDescending(h => h.Semester)
            .FirstOrDefault();

        var lastSgpa = latestStanding?.SemesterGpa ?? 0m;
        var currentCgpa = latestStanding?.CumulativeGpa ?? 0m;

        var model = new GpaSimulatorViewModel
        {
            StudentName = student.DisplayName,
            Department = student.Department?.Name ?? "Not assigned",
            Year = (student.StandingHistories.Count / 2) + 1,
            Semester = $"{student.CurrentSemester} {student.AcademicYear}",
            CurrentCgpa = (double)currentCgpa,
            LastSgpa = (double)lastSgpa,
            AcademicStanding = student.CurrentStanding.ToString(),
            CompletedCredits = completedCredits,
            CompletedQp = completedQp,
            GpaHours = gpaHours,
            CurrentCourses = currentCourses,
            ImprovableCourses = improvableCourses,
            GradeScale = gradeScale
        };

        return View(model);
    }
    public IActionResult ImpactAnalyzer() => View();

    [HttpPost]
    public async Task<IActionResult> SimulateFailure([FromBody] SimulateFailureRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        if (user.DepartmentId is null)
            return BadRequest(new { error = "No department assigned to your account." });

        var cgpa = await _db.StandingHistories
            .AsNoTracking()
            .Where(sh => sh.StudentId == user.Id)
            .OrderByDescending(sh => sh.AcademicYear)
            .ThenByDescending(sh => sh.Semester)
            .Select(sh => sh.CumulativeGpa)
            .FirstOrDefaultAsync();

        var result = await _impactAnalysisService
            .GetBlockedCoursesAsync(
                request.CourseId,
                user.DepartmentId.Value,
                user.CurrentSemester,
                user.AcademicYear,
                user.CurrentStanding,
                cgpa);

        if (result is null)
            return NotFound(new { error = "Selected course was not found in your department's curriculum." });

        return Json(result);
    }

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
            StandingCssClass = GetStandingCssClass(dto.Standing),
            StandingAlertMessage = dto.StandingAlert,
            ShowStandingAlert = dto.Standing != AcademicStanding.Good,

            Cgpa = (double)dto.Cgpa,
            MaxGpa = 4.0,
            CgpaChange = (double)dto.CgpaChange,

            CreditsEarned = dto.CreditsCompleted,
            CreditsRequired = dto.CreditsRequired,

            CoursesRemaining = dto.CoursesRemaining,
            CoreCoursesRemaining = dto.CoreCoursesRemaining,
            ElectiveCoursesRemaining = dto.ElectiveCoursesRemaining,
            UniversityRequiredCoursesRemaining = dto.UniReqCoursesRemaining,

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

    private static string GetStandingCssClass(AcademicStanding standing) => standing switch
    {
        AcademicStanding.Good => "good",
        AcademicStanding.Warning => "warning",
        AcademicStanding.Probation => "danger",
        AcademicStanding.Dismissed => "danger",
        _ => "good"
    };

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }
}
