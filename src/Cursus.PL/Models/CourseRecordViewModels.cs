using System.ComponentModel.DataAnnotations;
using Cursus.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cursus.PL.Models;

// ── Shared dropdown data ─────────────────────────────────────────────────────

/// <summary>
/// Base class carrying all SelectLists used by both Add and Edit forms.
/// </summary>
public abstract class CourseRecordFormBase
{
    public IEnumerable<SelectListItem> CourseOptions  { get; set; } = [];
    public IEnumerable<SelectListItem> GradeOptions   { get; set; } = [];
    public IEnumerable<SelectListItem> SemesterOptions { get; set; } = [];
    public string StudentId   { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
}

// ── AddCourseRecord ──────────────────────────────────────────────────────────

public class AddCourseRecordViewModel : CourseRecordFormBase
{
    [Required(ErrorMessage = "Please select a course.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid course.")]
    public int CourseId { get; set; }

    /// <summary>
    /// Optional letter grade (A+ … F).
    /// When blank, status defaults to InProgress.
    /// </summary>
    [StringLength(2, ErrorMessage = "Grade must be at most 2 characters.")]
    public string? Grade { get; set; }

    [Required(ErrorMessage = "Please select a semester.")]
    public SemesterType Semester { get; set; }

    [Required(ErrorMessage = "Academic year is required.")]
    [RegularExpression(@"^\d{4}[-–]\d{4}$",
        ErrorMessage = "Academic year must be in the format YYYY-YYYY (e.g. 2024-2025).")]
    [StringLength(10)]
    public string AcademicYear { get; set; } = string.Empty;
}

// ── EditCourseRecord ─────────────────────────────────────────────────────────

public class EditCourseRecordViewModel : CourseRecordFormBase
{
    public int RecordId   { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Optional letter grade (A+ … F).
    /// When blank, status is set to InProgress.
    /// </summary>
    [StringLength(2, ErrorMessage = "Grade must be at most 2 characters.")]
    public string? Grade { get; set; }

    [Required(ErrorMessage = "Please select a status.")]
    public StudentCourseStatus Status { get; set; }

    [Required(ErrorMessage = "Please select a semester.")]
    public SemesterType Semester { get; set; }

    [Required(ErrorMessage = "Academic year is required.")]
    [RegularExpression(@"^\d{4}[-–]\d{4}$",
        ErrorMessage = "Academic year must be in the format YYYY-YYYY (e.g. 2024-2025).")]
    [StringLength(10)]
    public string AcademicYear { get; set; } = string.Empty;
}
