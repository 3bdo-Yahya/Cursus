using System.ComponentModel.DataAnnotations;
using Cursus.Models.Enums;

namespace Cursus.Models.Entities
{
    public class GraduationRequirement
    {
        public int Id { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public CourseType CategoryType { get; set; }

        [Required]
        public int RequiredCredits { get; set; }

        public Department? Department { get; set; }

        public ICollection<GraduationRequirementCourse> GraduationRequirementCourses { get; set; } = new List<GraduationRequirementCourse>();


    }
}