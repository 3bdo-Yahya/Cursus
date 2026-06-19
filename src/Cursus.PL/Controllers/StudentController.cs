using Cursus.Domain.Constants;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.PL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Cursus.DAL.Database;
using Microsoft.EntityFrameworkCore;

namespace Cursus.PL.Controllers;

[Authorize(Roles = Roles.Student)]
public class StudentController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ApplicationDbContext _db;
    public StudentController(
    UserManager<AppUser> userManager,
    ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);
        var username = user?.UserName?.Split('@').FirstOrDefault() ?? "Student";

        var model = new StudentDashboardViewModel
        {
            StudentName = username,
            Initials = GetInitials(username),
            Department = "Computer Science",
            Year = 3,
            Semester = "Spring 2026",
            AcademicStanding = "Good Standing",

            Cgpa = 3.24,
            CgpaChange = +0.12,

            CreditsEarned = 84,
            CreditsRequired = 132,

            CoursesRemaining = 16,
            CoreCoursesRemaining = 12,
            ElectiveCoursesRemaining = 4,

            GraduationSemester = "Spring 2027",
            SemestersCompleted = 5,
            TotalSemesters = 8,

            CurrentCourses =
            [
                new() { Code = "CS301", Name = "Operating Systems", Schedule = "Mon, Wed 10:00 · Room 402", CreditHours = 3 },
                new() { Code = "CS304", Name = "Database Systems", Schedule = "Tue, Thu 14:00 · Online", CreditHours = 4 },
                new() { Code = "MATH301", Name = "Linear Algebra", Schedule = "Sun, Tue 09:00 · Room 210", CreditHours = 3 },
                new() { Code = "CS3XX", Name = "Free Elective", Schedule = "Wed 13:00 · TBD", CreditHours = 3, IsElective = true }
            ]
        };

        return View(model);
    }

    public IActionResult CourseMap() => View();
    public IActionResult Planner() => View();
    public IActionResult Progress() => View();
    public IActionResult AiAdvisor() => View();
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

        // Resolve Grade Scale for their university
        var gradeScale = await _db.GradeScales
            .AsNoTracking()
            .Where(gs => gs.UniversityId == student.Department.UniversityId)
            .ToDictionaryAsync(gs => gs.LetterGrade.ToUpper(), gs => (double)gs.PointValue);

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

        // Completed Courses
        var completedCourses = studentCourses
            .Where(sc => sc.Status == StudentCourseStatus.Completed && sc.Course is not null)
            .ToList();

        var completedCredits = completedCourses.Sum(sc => sc.Course!.CreditHours);

        // Calculate completed Quality Points (QP)
        double completedQp = completedCourses
            .Sum(sc => (gradeScale.TryGetValue(sc.Grade?.Trim().ToUpper() ?? "", out var pts) ? pts : 0.0) * sc.Course!.CreditHours);

        // Current In-Progress Courses
        var currentCourses = studentCourses
            .Where(sc => sc.Status == StudentCourseStatus.InProgress && sc.Course is not null)
            .Select(sc => new SimulatedCourseViewModel
            {
                Id = sc.Course!.Code,
                Name = sc.Course.Name,
                Credits = sc.Course.CreditHours
            })
            .ToList();

        // Improvable Courses (Original Grade <= D+ or F)
        var improvableCourses = studentCourses
            .Where(sc => (sc.Status == StudentCourseStatus.Failed || sc.Grade == "D" || sc.Grade == "D+") && sc.Course is not null)
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
            CurrentCourses = currentCourses,
            ImprovableCourses = improvableCourses,
            GradeScale = gradeScale
        };

        return View(model);
    }
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

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }
}
