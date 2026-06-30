using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Cursus.Domain.Entities;
using Cursus.Domain.Constants;
using Cursus.PL.Models;
using Cursus.BLL.Interfaces;

namespace Cursus.PL.Controllers;

[Authorize(Roles = Roles.Student)]
public class StudentController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IStudentPortalService _studentPortalService;

    public StudentController(UserManager<AppUser> userManager, IStudentPortalService studentPortalService)
    {
        _userManager = userManager;
        _studentPortalService = studentPortalService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        var snapshot = await _studentPortalService.GetSnapshotAsync(user.Id);
        if (snapshot is null)
        {
            return NotFound();
        }

        var model = StudentPortalViewModelMapper.ToDashboard(snapshot);
        return View(model);
    }

    public async Task<IActionResult> CourseMap()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var snapshot = await _studentPortalService.GetSnapshotAsync(user.Id);
        if (snapshot is null) return NotFound();

        var model = StudentPortalViewModelMapper.ToPageContext(snapshot, includeCourseMap: true);
        return View(model);
    }

    public async Task<IActionResult> Planner()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var snapshot = await _studentPortalService.GetSnapshotAsync(user.Id);
        if (snapshot is null) return NotFound();

        var model = StudentPortalViewModelMapper.ToPageContext(snapshot);
        return View(model);
    }

    public async Task<IActionResult> Progress()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var snapshot = await _studentPortalService.GetSnapshotAsync(user.Id);
        if (snapshot is null) return NotFound();

        var model = StudentPortalViewModelMapper.ToProgress(snapshot);
        return View(model);
    }

    public async Task<IActionResult> AiAdvisor()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var snapshot = await _studentPortalService.GetSnapshotAsync(user.Id);
        if (snapshot is null) return NotFound();

        var model = StudentPortalViewModelMapper.ToPageContext(snapshot);
        return View(model);
    }

    public async Task<IActionResult> GpaSimulator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var snapshot = await _studentPortalService.GetSnapshotAsync(user.Id);
        if (snapshot is null) return NotFound();

        var model = StudentPortalViewModelMapper.ToGpaSimulator(snapshot);
        return View(model);
    }

    public async Task<IActionResult> ImpactAnalyzer()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var snapshot = await _studentPortalService.GetSnapshotAsync(user.Id);
        if (snapshot is null) return NotFound();

        var model = StudentPortalViewModelMapper.ToPageContext(snapshot);
        return View(model);
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

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }
}
