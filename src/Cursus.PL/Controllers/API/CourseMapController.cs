using System.Security.Claims;
using Cursus.Domain.Constants;
using Cursus.Domain.Entities;
using Cursus.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cursus.PL.Controllers.API
{
    [Authorize(Roles = Roles.Student)]
    [ApiController]
    [Route("api/[controller]")]
    public class CourseMapController : ControllerBase
    {
        private readonly ICourseMapService _courseMapService;
        private readonly UserManager<AppUser> _userManager;

        public CourseMapController(
            ICourseMapService courseMapService,
            UserManager<AppUser> userManager)
        {
            _courseMapService = courseMapService;
            _userManager = userManager;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Unauthorized();

            if (user.DepartmentId is null)
                return BadRequest(new { error = "Please contact your admin to assign your department." });

            var result = await _courseMapService.GetCourseGraphForStudentAsync(user.Id, user.DepartmentId.Value);
            return Ok(result);
        }
    }
}