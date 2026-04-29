using Cursus.Domain.Enums;

namespace Cursus.Domain.Entities
{
    public class Course
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty!;
        public string Name { get; set; } = string.Empty!;
        public int CreditHours { get; set; }
        public CourseType CourseType { get; set; }
        public SemesterAvailability SemesterAvailability { get; set; }
        public string PassingGradeThreshold { get; set; } = string.Empty!;
        public bool IsActive { get; set; } = true;
        public int DepartmentId { set; get; }
        public Department? Department { get; set; }
        public ICollection<CoursePrerequisite> Prerequisites { get; set; } = [];
        public ICollection<CoursePrerequisite> IsPrerequisiteFor { get; set; } = [];
        public ICollection<StudentCourse> StudentCourses { get; set; } = [];
        public ICollection<GraduationRequirementCourse> GraduationRequirementCourses { get; set; } = [];
    }
}
