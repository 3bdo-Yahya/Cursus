using System.ComponentModel.DataAnnotations;
using Cursus.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cursus.PL.Models;

public class StudentCourseFormViewModel
{
    public int? RecordId { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public string? CourseDisplayName { get; set; }

    public string? DepartmentName { get; set; }

    public int? DepartmentId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a course.")]
    public int? CourseId { get; set; }

    [StringLength(2)]
    public string? Grade { get; set; }

    public StudentCourseStatus? Status { get; set; } = StudentCourseStatus.InProgress;

    [Required]
    public SemesterType? Semester { get; set; }

    [Required]
    [StringLength(10)]
    public string AcademicYear { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> CourseOptions { get; set; } = [];

    public IEnumerable<SelectListItem> GradeOptions { get; set; } = [];
}
