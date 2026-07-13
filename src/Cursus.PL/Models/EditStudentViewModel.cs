using System.ComponentModel.DataAnnotations;
using Cursus.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cursus.PL.Models;

public class EditStudentViewModel
{
    public string Id          { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email      { get; set; }

    // ── Academic assignment ──────────────────────────────────────────────

    [Required(ErrorMessage = "Please select a department.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid department.")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Academic year is required.")]
    [StringLength(10)]
    [RegularExpression(@"^\d{1,2}$",
        ErrorMessage = "Academic year must be a single number (1–6) representing the year of study.")]
    public string AcademicYear { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a semester.")]
    public SemesterType CurrentSemester { get; set; }

    [Required(ErrorMessage = "Please select a standing.")]
    public AcademicStanding CurrentStanding { get; set; }

    public DateTime? EnrollmentDate { get; set; }

    // ── Dropdown data ────────────────────────────────────────────────────
    public IEnumerable<SelectListItem> DepartmentOptions  { get; set; } = [];
    public IEnumerable<SelectListItem> SemesterOptions    { get; set; } = [];
    public IEnumerable<SelectListItem> StandingOptions    { get; set; } = [];
}
