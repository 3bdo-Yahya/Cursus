using System.ComponentModel.DataAnnotations;
using Cursus.Models.Enums;

namespace Cursus.Models.Entities
{
    public class StudentCourse
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public int CourseId { get; set; }

        [Required]
        public StudentCourseStatus Status { get; set; }

        
        [StringLength(2)]
        public string? Grade { get; set; }

        [Required]
        public SemesterType Semester { get; set; }

        [Required]
        [StringLength(10)]
        public string AcademicYear { get; set; } = string.Empty;

        // Relations
        public AppUser? Student { get; set; }
        public Course? Course { get; set; }
    }
}
