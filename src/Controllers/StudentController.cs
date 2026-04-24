using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Cursus.Models.ViewModels;

namespace Cursus.Controllers  
{  
    [Authorize]  
    public class StudentController : Controller  
    {  
        public IActionResult Dashboard()  
        {  
            var model = new DashboardViewModel  
            {  
                StudentName = "Mohamed Hazem",  
                Department = "Computer Science",  
                CGPA = 3.2,  
                CreditsCompleted = 90,  
                TotalCreditsRequired = 120,  
                CoursesRemaining = 10,  
                AcademicStanding = AcademicStanding.Good,  
                ProjectedGraduationDate = new DateOnly(2027, 1, 1)  
            };  

            return View(model);  
        }  

        public IActionResult CourseMap()  
        {  
            return View();  
        }  

        public IActionResult Progress()  
        {  
            return View();  
        }  

        public IActionResult AiAdvisor()  
        {  
            return View();  
        }
    }
}