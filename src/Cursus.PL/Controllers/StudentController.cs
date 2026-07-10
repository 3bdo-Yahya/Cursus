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
    private readonly IAcademicMetricsService _academicMetricsService;
    private readonly IPlannerService _plannerService;

    public StudentController(
        UserManager<AppUser> userManager,
        ApplicationDbContext db,
        IProgressService progressService,
        IStudentDashboardService dashboardService,
        IImpactAnalysisService impactAnalysisService,
        IGeminiService geminiService,
        IAcademicMetricsService academicMetricsService,
        IPlannerService plannerService)
    {
        _userManager = userManager;
        _db = db;
        _progressService = progressService;
        _dashboardService = dashboardService;
        _impactAnalysisService = impactAnalysisService;
        _geminiService = geminiService;
        _academicMetricsService = academicMetricsService;
        _plannerService = plannerService;
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
    public async Task<IActionResult> Planner()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        var student = await _db.Users
            .Include(u => u.Department)
            .Include(u => u.StudentCourses)
                .ThenInclude(sc => sc.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (student is null)
            return NotFound();

        var gradeScaleDecimal = await _academicMetricsService.GetGradeScaleAsync(student.Department?.UniversityId);
        var studentCourses = student.StudentCourses.ToList();
        var bestAttempts = _academicMetricsService.ResolveBestAttempts(studentCourses);
        var cgpa = _academicMetricsService.CalculateCgpa(bestAttempts, gradeScaleDecimal);
        var termGpas = _academicMetricsService.CalculateSgpaByTerm(studentCourses, gradeScaleDecimal);

        var creditLimit = _academicMetricsService.GetCreditLimits(student.CurrentStanding, cgpa);
        var isOverloadEligible = student.CurrentStanding == AcademicStanding.Good && cgpa >= 3.0m;
        var overloadLimit = isOverloadEligible ? 21 : creditLimit;

        var completedCourses = bestAttempts
            .Where(sc => sc.Status == StudentCourseStatus.Completed && sc.Course is not null)
            .Select(sc => sc.Course!.Code)
            .ToList();

        var completedCredits = bestAttempts
            .Where(sc => sc.Status == StudentCourseStatus.Completed && sc.Course is not null)
            .Sum(sc => sc.Course!.CreditHours);

        var inProgressCourses = studentCourses
            .Where(sc => sc.Status == StudentCourseStatus.InProgress && sc.Course is not null)
            .Select(sc => sc.Course!.Code)
            .ToList();

        var currentlyEnrolled = studentCourses
            .Where(sc => sc.Status == StudentCourseStatus.InProgress && sc.Course is not null)
            .Select(sc =>
            {
                var (type, typeClass) = MapPlannerCourseType(sc.Course!.CourseType);
                return new PlannerEnrolledCourseViewModel
                {
                    Id = sc.Course.Code,
                    Name = sc.Course.Name,
                    Credits = sc.Course.CreditHours,
                    Type = type,
                    TypeClass = typeClass
                };
            })
            .ToList();

        var planningTerms = await _plannerService.GetPlanningTermsAsync(student.Id, creditLimit);
        var primaryTerm = planningTerms.FirstOrDefault(t => t.IsPrimary) ?? planningTerms.FirstOrDefault();
        if (primaryTerm is null)
            return NotFound();

        var primaryCapacity = await _plannerService.GetTermCapacityAsync(
            student.Id,
            primaryTerm.AcademicYear,
            primaryTerm.Semester,
            creditLimit);

        var allPlannedCourses = await _plannerService.GetAllPlansAsync(student.Id);
        var primaryPlannedCourses = allPlannedCourses
            .Where(pc => pc.AcademicYear == primaryTerm.AcademicYear && pc.Semester == primaryTerm.Semester)
            .ToList();
        var plannedCodes = allPlannedCourses
            .Select(pc => pc.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var catalogCourses = await _db.Courses
            .Include(c => c.Prerequisites)
                .ThenInclude(p => p.Prerequisite)
            .Where(c => (c.DepartmentId == student.DepartmentId || c.CourseType == CourseType.UniversityReq) && c.IsActive)
            .Where(c =>
                c.SemesterAvailability == SemesterAvailability.All
                || c.SemesterAvailability == SemesterAvailability.FallSpring
                || (primaryTerm.Semester == SemesterType.Fall && c.SemesterAvailability == SemesterAvailability.Fall)
                || (primaryTerm.Semester == SemesterType.Spring && c.SemesterAvailability == SemesterAvailability.Spring))
            .AsNoTracking()
            .ToListAsync();

        var catalog = catalogCourses
            .Select(c =>
            {
                var (type, typeClass) = MapPlannerCourseType(c.CourseType);
                return new PlannerCourseViewModel
                {
                    CourseId = c.Id,
                    Id = c.Code,
                    Name = c.Name,
                    Credits = c.CreditHours,
                    Type = type,
                    TypeClass = typeClass,
                    Category = c.CourseType,
                    Prereqs = c.Prerequisites
                        .Where(p => p.Prerequisite is not null)
                        .Select(p => p.Prerequisite!.Code)
                        .ToList()
                };
            })
            .Where(c => !completedCourses.Contains(c.Id))
            .Where(c => !inProgressCourses.Contains(c.Id))
            .Where(c => !plannedCodes.Contains(c.Id))
            .GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var model = new PlannerViewModel
        {
            StudentId = student.Id,
            StudentName = student.DisplayName,
            Department = student.Department?.Name ?? "Not assigned",
            Year = int.TryParse(student.AcademicYear, out var plannerYear) ? plannerYear : (termGpas.Count / 2) + 1,
            Semester = FormatSemester(student.CurrentSemester, student.AcademicYear),
            CurrentCgpa = (double)cgpa,
            AcademicStanding = FormatStanding(student.CurrentStanding),
            StandingCssClass = GetStandingCssClass(student.CurrentStanding),
            CompletedCredits = completedCredits,
            TotalCreditsRequired = student.Department?.TotalCreditsRequired ?? 132,
            CreditLimit = creditLimit,
            OverloadLimit = overloadLimit,
            IsOverloadEligible = isOverloadEligible,
            CompletedCourses = completedCourses,
            InProgressCourses = inProgressCourses,
            CurrentlyEnrolledCourses = currentlyEnrolled,
            Terms = planningTerms
                .Select(t => new PlannerTermViewModel
                {
                    AcademicYear = t.AcademicYear,
                    Semester = t.Semester,
                    Label = FormatSemester(t.Semester, t.AcademicYear),
                    IsPrimary = t.IsPrimary
                })
                .ToList(),
            PrimaryTermCapacity = new PlannerTermCapacityViewModel
            {
                AcademicYear = primaryCapacity.AcademicYear,
                Semester = primaryCapacity.Semester,
                ForcedInProgressCredits = primaryCapacity.ForcedInProgressCredits,
                PlannedCredits = primaryCapacity.PlannedCredits,
                RemainingRoom = primaryCapacity.RemainingRoom
            },
            PlannedCourses = primaryPlannedCourses
                .Select(pc =>
                {
                    var (type, typeClass) = MapPlannerCourseType(pc.CourseType);
                    return new PlannerPlannedCourseViewModel
                    {
                        CourseId = pc.CourseId,
                        Code = pc.Code,
                        Name = pc.Name,
                        Credits = pc.CreditHours,
                        Type = type,
                        TypeClass = typeClass
                    };
                })
                .ToList(),
            Catalog = catalog
        };

        return View(model);
    }

    [HttpGet]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlannerPlan(string academicYear, SemesterType semester)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
            return BadRequest(new { error = "Academic year is required." });

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var student = await _db.Users
            .Include(u => u.Department)
            .Include(u => u.StudentCourses)
                .ThenInclude(sc => sc.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (student is null)
            return NotFound();

        var gradeScaleDecimal = await _academicMetricsService.GetGradeScaleAsync(student.Department?.UniversityId);
        var cgpa = _academicMetricsService.CalculateCgpa(
            _academicMetricsService.ResolveBestAttempts(student.StudentCourses),
            gradeScaleDecimal);
        var creditLimit = _academicMetricsService.GetCreditLimits(student.CurrentStanding, cgpa);

        var plannedCourses = await _plannerService.GetPlanAsync(student.Id, academicYear, semester);
        var capacity = await _plannerService.GetTermCapacityAsync(student.Id, academicYear, semester, creditLimit);

        return Json(new
        {
            term = new { academicYear, semester = semester.ToString() },
            capacity = new
            {
                forcedInProgressCredits = capacity.ForcedInProgressCredits,
                plannedCredits = capacity.PlannedCredits,
                remainingRoom = capacity.RemainingRoom
            },
            plannedCourses = plannedCourses.Select(pc => new
            {
                courseId = pc.CourseId,
                code = pc.Code,
                name = pc.Name,
                credits = pc.CreditHours,
                type = pc.CourseType.ToString()
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPlannedCourse([FromBody] PlannerCourseMutationRequest request)
    {
        if (request is null || request.CourseId <= 0 || string.IsNullOrWhiteSpace(request.AcademicYear))
            return BadRequest(new { error = "Invalid request payload." });

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var student = await _db.Users
            .Include(u => u.Department)
            .Include(u => u.StudentCourses)
                .ThenInclude(sc => sc.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (student is null)
            return NotFound();

        var course = await _db.Courses
            .Include(c => c.Prerequisites)
                .ThenInclude(p => p.Prerequisite)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CourseId && c.IsActive);

        if (course is null)
            return NotFound(new { error = "Course not found." });

        if (course.CourseType != CourseType.UniversityReq && course.DepartmentId != student.DepartmentId)
            return BadRequest(new { error = "Course is out of your department scope." });

        if (student.StudentCourses.Any(sc => sc.CourseId == request.CourseId))
            return BadRequest(new { error = "Course is already completed/in progress/recorded." });

        var existingPlan = await _plannerService.GetPlanAsync(student.Id, request.AcademicYear, request.Semester);
        if (existingPlan.Any(pc => pc.CourseId == request.CourseId))
            return BadRequest(new { error = "Course is already planned for this term." });

        var allPlans = await _plannerService.GetAllPlansAsync(student.Id);
        var availableCodes = student.StudentCourses
            .Where(sc => sc.Course is not null
                         && (sc.Status == StudentCourseStatus.Completed
                             || sc.Status == StudentCourseStatus.InProgress))
            .Select(sc => sc.Course!.Code)
            .Concat(allPlans.Select(pc => pc.Code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingPrereq = course.Prerequisites
            .Where(p => p.Prerequisite is not null)
            .Select(p => p.Prerequisite!.Code)
            .FirstOrDefault(code => !availableCodes.Contains(code));

        if (!string.IsNullOrWhiteSpace(missingPrereq))
            return BadRequest(new { error = $"Prerequisite {missingPrereq} is not satisfied." });

        var gradeScaleDecimal = await _academicMetricsService.GetGradeScaleAsync(student.Department?.UniversityId);
        var cgpa = _academicMetricsService.CalculateCgpa(
            _academicMetricsService.ResolveBestAttempts(student.StudentCourses),
            gradeScaleDecimal);
        var creditLimit = _academicMetricsService.GetCreditLimits(student.CurrentStanding, cgpa);

        var capacity = await _plannerService.GetTermCapacityAsync(
            student.Id,
            request.AcademicYear,
            request.Semester,
            creditLimit);

        if (capacity.RemainingRoom < course.CreditHours)
            return BadRequest(new { error = "Adding this course exceeds term credit capacity." });

        var added = await _plannerService.AddPlannedCourseAsync(
            student.Id,
            request.CourseId,
            request.AcademicYear,
            request.Semester);

        if (!added)
            return BadRequest(new { error = "Unable to add planned course." });

        var updatedCapacity = await _plannerService.GetTermCapacityAsync(
            student.Id,
            request.AcademicYear,
            request.Semester,
            creditLimit);

        return Json(new
        {
            success = true,
            capacity = new
            {
                forcedInProgressCredits = updatedCapacity.ForcedInProgressCredits,
                plannedCredits = updatedCapacity.PlannedCredits,
                remainingRoom = updatedCapacity.RemainingRoom
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePlannedCourse([FromBody] PlannerCourseMutationRequest request)
    {
        if (request is null || request.CourseId <= 0 || string.IsNullOrWhiteSpace(request.AcademicYear))
            return BadRequest(new { error = "Invalid request payload." });

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var removed = await _plannerService.RemovePlannedCourseAsync(
            user.Id,
            request.CourseId,
            request.AcademicYear,
            request.Semester);

        if (!removed)
            return NotFound(new { error = "Planned course was not found for this term." });

        var student = await _db.Users
            .Include(u => u.Department)
            .Include(u => u.StudentCourses)
                .ThenInclude(sc => sc.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (student is null)
            return NotFound();

        var gradeScaleDecimal = await _academicMetricsService.GetGradeScaleAsync(student.Department?.UniversityId);
        var cgpa = _academicMetricsService.CalculateCgpa(
            _academicMetricsService.ResolveBestAttempts(student.StudentCourses),
            gradeScaleDecimal);
        var creditLimit = _academicMetricsService.GetCreditLimits(student.CurrentStanding, cgpa);
        var capacity = await _plannerService.GetTermCapacityAsync(
            student.Id,
            request.AcademicYear,
            request.Semester,
            creditLimit);

        return Json(new
        {
            success = true,
            capacity = new
            {
                forcedInProgressCredits = capacity.ForcedInProgressCredits,
                plannedCredits = capacity.PlannedCredits,
                remainingRoom = capacity.RemainingRoom
            }
        });
    }
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
    public async Task<IActionResult> AiAdvisor()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        var dto = await _dashboardService.GetDashboardDataAsync(user.Id);
        if (dto is null)
            return RedirectToAction("Login", "Account");

        var model = new AiAdvisorViewModel
        {
            StudentName = dto.DisplayName,
            Initials = GetInitials(dto.DisplayName),
            Department = dto.DepartmentName,
            Year = int.TryParse(dto.AcademicYear, out var advisorYear) ? advisorYear : (dto.SemestersCompleted / 2) + 1,
            Cgpa = (double)dto.Cgpa,
            AcademicStanding = FormatStanding(dto.Standing),
            StandingCssClass = GetStandingCssClass(dto.Standing)
        };

        return View(model);
    }

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
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (student is null)
            return NotFound();

        var gradeScaleDecimal = await _academicMetricsService.GetGradeScaleAsync(student.Department?.UniversityId);
        var gradeScale = gradeScaleDecimal.ToDictionary(kvp => kvp.Key, kvp => (double)kvp.Value);

        var studentCourses = student.StudentCourses.ToList();

        var bestAttempts = _academicMetricsService.ResolveBestAttempts(studentCourses);
        var studentCourseMap = bestAttempts.ToDictionary(sc => sc.CourseId);

        // Completed Courses (only status == Completed)
        var completedCourses = bestAttempts
            .Where(sc => sc.Status == StudentCourseStatus.Completed && sc.Course is not null)
            .ToList();

        var completedCredits = completedCourses.Sum(sc => sc.Course!.CreditHours);

        var completedCourseCodes = completedCourses
            .Select(sc => sc.Course!.Code)
            .ToList();

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

        var cgpa = _academicMetricsService.CalculateCgpa(bestAttempts, gradeScaleDecimal);
        var termGpas = _academicMetricsService.CalculateSgpaByTerm(studentCourses, gradeScaleDecimal);

        var latestGraded = _academicMetricsService.GetLatestGradedTerms(termGpas, 1);
        var lastSgpa = latestGraded.Count > 0 ? latestGraded[^1].SemesterGpa : 0m;

        var creditLimit = _academicMetricsService.GetCreditLimits(student.CurrentStanding, cgpa);
        var planningTerms = await _plannerService.GetPlanningTermsAsync(student.Id, creditLimit);
        var primaryTerm = planningTerms.FirstOrDefault(t => t.IsPrimary) ?? planningTerms.FirstOrDefault();

        var blockedPlannedCodes = completedCourseCodes
            .Concat(currentCourses.Select(c => c.Id))
            .Concat(improvableCourses.Select(c => c.Id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var plannedCourses = new List<SimulatedCourseViewModel>();
        if (primaryTerm is not null)
        {
            var primaryPlan = await _plannerService.GetPlanAsync(
                student.Id,
                primaryTerm.AcademicYear,
                primaryTerm.Semester);

            plannedCourses = primaryPlan
                .Where(pc => !blockedPlannedCodes.Contains(pc.Code))
                .Select(pc => new SimulatedCourseViewModel
                {
                    Id = pc.Code,
                    Name = pc.Name,
                    Credits = pc.CreditHours
                })
                .ToList();
        }

        var model = new GpaSimulatorViewModel
        {
            StudentId = student.Id,
            StudentName = student.DisplayName,
            Department = student.Department?.Name ?? "Not assigned",
            Year = int.TryParse(student.AcademicYear, out var simYear) ? simYear : (termGpas.Count / 2) + 1,
            Semester = FormatSemester(student.CurrentSemester, student.AcademicYear),
            CurrentCgpa = (double)cgpa,
            LastSgpa = (double)lastSgpa,
            AcademicStanding = FormatStanding(student.CurrentStanding),
            StandingCssClass = GetStandingCssClass(student.CurrentStanding),
            CompletedCredits = completedCredits,
            CompletedQp = completedQp,
            GpaHours = gpaHours,
            CurrentCourses = currentCourses,
            PlannedCourses = plannedCourses,
            ImprovableCourses = improvableCourses,
            CompletedCourses = completedCourseCodes,
            GradeScale = gradeScale
        };

        return View(model);
    }
    public IActionResult ImpactAnalyzer() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulateFailure([FromBody] SimulateFailureRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        if (user.DepartmentId is null)
            return BadRequest(new { error = "No department assigned to your account." });

        var department = await _db.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == user.DepartmentId.Value);

        var studentCourses = await _db.StudentCourses
            .Include(sc => sc.Course)
            .Where(sc => sc.StudentId == user.Id)
            .AsNoTracking()
            .ToListAsync();

        var gradeScale = await _academicMetricsService.GetGradeScaleAsync(department?.UniversityId);
        var bestAttempts = _academicMetricsService.ResolveBestAttempts(studentCourses);
        var cgpa = _academicMetricsService.CalculateCgpa(bestAttempts, gradeScale);

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
        if (user is null)
            return RedirectToAction("Login", "Account");

        var dto = await _dashboardService.GetDashboardDataAsync(user.Id);
        if (dto is null)
            return RedirectToAction("Login", "Account");

        if (dto.DepartmentName == "Not assigned")
            TempData["Warning"] = "Please contact your admin to assign your department.";
        else if (!dto.HasAcademicRecords)
            TempData["Warning"] = "No academic records found yet. Your profile will populate once your admin enters your course history.";

        ViewData["StudentEmail"] = user.Email ?? "";
        return View(MapToViewModel(dto));
    }

    private static StudentDashboardViewModel MapToViewModel(StudentDashboardDto dto)
    {
        return new StudentDashboardViewModel
        {
            StudentName = dto.DisplayName,
            Initials = GetInitials(dto.DisplayName),
            Department = dto.DepartmentName,
            Year = int.TryParse(dto.AcademicYear, out var dashYear) ? dashYear : (dto.SemestersCompleted / 2) + 1,
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
                .ToList(),

            UniversityName = dto.UniversityName,
            EnrollmentDate = dto.EnrollmentDate,
            HighestSgpa = (double)dto.HighestSgpa,
            GpaHistory = dto.GpaHistory
                .Select(h => new GpaHistoryPointViewModel
                {
                    SemLabel = h.SemLabel,
                    Sgpa = (double)h.Sgpa
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
            return $"{semesterName} — Year {academicYear}";

        return semesterName;
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

    private static (string Type, string TypeClass) MapPlannerCourseType(CourseType courseType) =>
        courseType switch
        {
            CourseType.Core => ("Core", "type-core"),
            CourseType.DeptElective => ("Dept. Elective", "type-elec"),
            CourseType.FreeElective => ("Free Elective", "type-free"),
            CourseType.UniversityReq => ("University Req.", "type-univ"),
            _ => ("Core", "type-core")
        };
}






