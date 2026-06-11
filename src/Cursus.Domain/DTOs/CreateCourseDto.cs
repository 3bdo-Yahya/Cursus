using System.ComponentModel.DataAnnotations;
using Cursus.Domain.Enums;

namespace Cursus.Domain.DTOs
{
    public class CreateCourseDto
    {
        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int DepartmentId { set; get; }

        [Required]
        [Range(1, 6)]
        public int CreditHours { get; set; }

        [Required]
        [StringLength(2)]
        public string PassingGradeThreshold { get; set; } = "D";

        [Required]
        public CourseType CourseType { get; set; }

        [Required]
        public SemesterAvailability SemesterAvailability { get; set; }

        public bool IsActive { get; set; } = true;
    }
}