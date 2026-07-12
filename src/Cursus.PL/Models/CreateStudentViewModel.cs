using System;
using System.ComponentModel.DataAnnotations;
using Cursus.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cursus.PL.Models;

public class CreateStudentViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Temporary Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a department.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a department.")]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Academic year is required (e.g. 2024-2025).")]
    [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "Academic year must be in YYYY-YYYY format.")]
    [StringLength(10)]
    [Display(Name = "Academic Year")]
    public string AcademicYear { get; set; } = string.Empty;

    [Display(Name = "Current Semester")]
    public SemesterType CurrentSemester { get; set; } = SemesterType.Fall;

    [Display(Name = "Enrollment Date")]
    public DateTime? EnrollmentDate { get; set; }

    // ── Dropdowns ─────────────────────────────────────────────────────────────
    public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = [];
    public IEnumerable<SelectListItem> SemesterOptions   { get; set; } = [];
}
