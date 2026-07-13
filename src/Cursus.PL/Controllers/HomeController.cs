using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cursus.PL.Models;
using Cursus.Domain.Constants;

namespace Cursus.PL.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(Roles.SuperAdmin) || User.IsInRole(Roles.Admin))
                return RedirectToAction("Index", "Admin");

            if (User.IsInRole(Roles.Student))
                return RedirectToAction("Dashboard", "Student");
        }

        return Redirect("/Identity/Account/Login");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

