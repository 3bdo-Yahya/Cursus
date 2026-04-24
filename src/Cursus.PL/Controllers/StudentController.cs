using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cursus.PL.Controllers;

[Authorize]
public class StudentController : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }
}
