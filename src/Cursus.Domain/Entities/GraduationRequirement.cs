using System.ComponentModel.DataAnnotations;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Entities
{
    public class GraduationRequirement
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public CourseType CategoryType { get; set; }
        public int RequiredCredits { get; set; }
        public Department? Department { get; set; }
        public ICollection<GraduationRequirementCourse> GraduationRequirementCourses { get; set; } = [];
    }
}