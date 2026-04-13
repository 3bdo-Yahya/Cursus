using System.ComponentModel.DataAnnotations;
using Cursus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cursus.Domain.Entities
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, 6)]
        public int CreditHours { get; set; }

        [Required]
        public CourseType CourseType { get; set; }

        // خريف ربيع او في الخريف و الربيع
        [Required]
        public SemesterAvailability SemesterAvailability { get; set; }

        [Required]
        [StringLength(2)]
        public string PassingGradeThreshold { get; set; } = "D";
        
        [Required]
        public int DepartmentId { set; get; }

        public bool IsActive { get; set; } = true;

        // Link to a department
        // each course is belong to a departments and department have many courses
        // many to many this will lead to a new junction table

        
        public Department? Department { get; set; }

        // الكورس هنا بيبقي عنده متطلبات عشان يتفتح
        [DeleteBehavior(DeleteBehavior.Restrict)]
        public ICollection<CoursePrerequisite> Prerequisites { get; set; } = new List<CoursePrerequisite>();

        // في نفس الوقت هوا في حد ذاته متطلب لمجموعة من الكورسات
        [DeleteBehavior(DeleteBehavior.Restrict)]
        public ICollection<CoursePrerequisite> IsPrerequisiteFor { get; set; } = new List<CoursePrerequisite>();

        public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
        public ICollection<GraduationRequirementCourse> GraduationRequirementCourses { get; set; } = new List<GraduationRequirementCourse>();





    }
}
