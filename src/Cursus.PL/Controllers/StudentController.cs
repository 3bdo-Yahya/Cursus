using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Cursus.Domain.Entities;
using Cursus.PL.Constants;
using Cursus.PL.Models;

namespace Cursus.PL.Controllers;

[Authorize(Roles = Roles.Student)]
public class StudentController : Controller
{
    private readonly UserManager<AppUser> _userManager;

    public StudentController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
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
    public IActionResult GpaSimulator() => View();
    public IActionResult ImpactAnalyzer() => View();

    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var username = user.UserName?.Split('@').FirstOrDefault() ?? "Student";
        var claims = await _userManager.GetClaimsAsync(user);
        var fullNameClaim = claims.FirstOrDefault(c => c.Type == "FullName")?.Value;
        if (!string.IsNullOrWhiteSpace(fullNameClaim))
        {
            username = fullNameClaim;
        }

        ViewData["StudentName"] = username;
        ViewData["StudentEmail"] = user.Email ?? "";
        ViewData["Initials"] = GetInitials(username);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        // Update phone only (email is readonly)
        if (!string.Equals(user.PhoneNumber ?? string.Empty, model.PhoneNumber ?? string.Empty, StringComparison.Ordinal))
        {
            var setPhone = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
            if (!setPhone.Succeeded)
                return BadRequest(setPhone.Errors);
        }

        // Store a display name in a claim so we don't change login behavior
        var claims = await _userManager.GetClaimsAsync(user);
        var existing = claims.FirstOrDefault(c => c.Type == "FullName");
        if (existing != null)
        {
            var removeClaimResult = await _userManager.RemoveClaimAsync(user, existing);
            if (!removeClaimResult.Succeeded)
                return BadRequest(removeClaimResult.Errors);
        }
        if (!string.IsNullOrWhiteSpace(model.FullName))
        {
            var addClaimResult = await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("FullName", model.FullName));
            if (!addClaimResult.Succeeded)
                return BadRequest(addClaimResult.Errors);
        }

        // Persist any remaining changes (some methods already updated store)
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors);

        return Json(new { success = true });
    }

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }
}
