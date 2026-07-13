using System.ComponentModel.DataAnnotations;
using Cursus.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cursus.PL.Models;

public class StudentOnboardingViewModel
{
    [Required(ErrorMessage = "Please select a university.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a university.")]
    [Display(Name = "University")]
    public int UniversityId { get; set; }

    [Required(ErrorMessage = "Please select a department.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a department.")]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Display(Name = "Current Semester")]
    public SemesterType CurrentSemester { get; set; } = SemesterType.Fall;

    [Display(Name = "Enrollment Date")]
    public DateTime? EnrollmentDate { get; set; }

    public IEnumerable<SelectListItem> UniversityOptions { get; set; } = [];
    public IEnumerable<SelectListItem> SemesterOptions { get; set; } = [];
}
