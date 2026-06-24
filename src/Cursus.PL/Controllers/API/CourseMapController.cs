using System.Security.Claims;
using Cursus.Domain.Constants;
using Cursus.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cursus.PL.Controllers.API
{
    [Authorize(Roles = Roles.Student)]
    [ApiController]
    [Route("api/[controller]")]
    public class CourseMapController : ControllerBase
    {
        private readonly ICourseMapService _courseMapService;
        public CourseMapController(ICourseMapService courseMapService)
        {
            _courseMapService = courseMapService;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll([FromQuery] int departmentId)
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized();

            var result = await _courseMapService.GetCourseGraphForStudentAsync(studentId, departmentId);
            return Ok(result);
        }
    }
}