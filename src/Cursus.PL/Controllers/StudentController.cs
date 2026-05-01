using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cursus.PL.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }
}
