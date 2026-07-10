using System.ComponentModel.DataAnnotations;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Entities;

public class PlannedCourse
{
    public int Id { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    public int CourseId { get; set; }

    [Required]
    public SemesterType Semester { get; set; }

    [Required]
    [StringLength(10)]
    public string AcademicYear { get; set; } = string.Empty;

    public AppUser? Student { get; set; }
    public Course? Course { get; set; }
}
