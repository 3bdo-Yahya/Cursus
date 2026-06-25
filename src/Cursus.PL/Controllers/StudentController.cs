using System.Linq;
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
using Cursus.BLL.Services;
using Cursus.PL.Models;

namespace Cursus.PL.Controllers;

[Authorize(Roles = Roles.Student)]
public class StudentController : Controller
{
    private const int MaxProviderHistoryMessages = 12;
    private const int MaxDisplayedHistoryMessages = 80;

    private readonly UserManager<AppUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IProgressService _progressService;
    private readonly IStudentDashboardService _dashboardService;
    private readonly IAiAdvisorService _aiAdvisorService;
    private readonly IAiAdvisorHistoryService _aiAdvisorHistoryService;
    private readonly IImpactAnalysisService _impactAnalysisService;

    public StudentController(
        UserManager<AppUser> userManager,
        ApplicationDbContext db,
        IProgressService progressService,
        IStudentDashboardService dashboardService,
        IAiAdvisorService aiAdvisorService,
        IAiAdvisorHistoryService aiAdvisorHistoryService,
        IImpactAnalysisService impactAnalysisService)
    {
        _userManager = userManager;
        _db = db;
        _progressService = progressService;
        _dashboardService = dashboardService;
        _aiAdvisorService = aiAdvisorService;
        _aiAdvisorHistoryService = aiAdvisorHistoryService;
        _impactAnalysisService = impactAnalysisService;
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

    [HttpGet]
    public async Task<IActionResult> AiAdvisorHistory(CancellationToken cancellationToken)
    {
        var studentId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized(AiAdvisorResponseDto.Failure(
                "student_not_authenticated",
                "Sign in with a student account to use the AI advisor."));
        }

        var messages = await _aiAdvisorHistoryService.GetRecentMessagesAsync(
            studentId,
            MaxDisplayedHistoryMessages,
            cancellationToken);

        return Ok(messages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AiAdvisorClearHistory(CancellationToken cancellationToken)
    {
        var studentId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized(AiAdvisorResponseDto.Failure(
                "student_not_authenticated",
                "Sign in with a student account to use the AI advisor."));
        }

        await _aiAdvisorHistoryService.ClearAsync(studentId, cancellationToken);
        return NoContent();
    }

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

        var conversationHistory = await _aiAdvisorHistoryService.GetRecentMessagesAsync(
            studentId,
            MaxProviderHistoryMessages,
            cancellationToken);

        var studentContext = AiAdvisorContextFactory.Create(audit);
        var response = await _aiAdvisorService.GetAdvisorResponseAsync(
            studentContext,
            message,
            cancellationToken,
            conversationHistory);

        if (!response.Succeeded)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, response);

        await _aiAdvisorHistoryService.SaveExchangeAsync(
            studentId,
            message,
            response.Message,
            cancellationToken);

        return Ok(AiAdvisorResponseDto.Success(
            response.Message,
            BuildSuggestedQuestions(message, response.Message)));
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

    /// <summary>
    /// AJAX endpoint for the Impact Analyzer.
    /// Accepts a course ID and returns the list of courses blocked
    /// by simulating that course as failed.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SimulateFailure([FromBody] SimulateFailureRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        if (user.DepartmentId is null)
            return BadRequest(new { error = "No department assigned to your account." });

        var blocked = await _impactAnalysisService
            .GetBlockedCoursesAsync(request.CourseId, user.DepartmentId.Value);

        return Json(blocked);
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

    private static IReadOnlyList<string> BuildSuggestedQuestions(
        string userMessage,
        string advisorMessage)
    {
        var combined = $"{userMessage} {advisorMessage}".ToLowerInvariant();
        var questions = new List<string>();

        void Add(string question)
        {
            if (!questions.Contains(question, StringComparer.OrdinalIgnoreCase))
                questions.Add(question);
        }

        if (combined.Contains("gpa") || combined.Contains("cgpa") || combined.Contains("grade"))
        {
            Add("Which courses can raise my GPA the most?");
            Add("What grades should I target this semester?");
        }

        if (combined.Contains("fail") ||
            combined.Contains("drop") ||
            combined.Contains("withdraw") ||
            combined.Contains("impact"))
        {
            Add("Which courses would be delayed if this goes wrong?");
            Add("How should I recover if I fail this course?");
        }

        if (combined.Contains("next semester") ||
            combined.Contains("available") ||
            combined.Contains("take next"))
        {
            Add("Can you rank my available courses for next semester?");
            Add("What is a balanced course load for me?");
        }

        if (combined.Contains("locked") ||
            combined.Contains("prerequisite") ||
            combined.Contains("unlock"))
        {
            Add("Which prerequisites should I clear first?");
        }

        Add("Am I still on track to graduate?");
        Add("What should I focus on this week?");
        Add("What should I ask my faculty advisor?");

        return questions.Take(4).ToList();
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
