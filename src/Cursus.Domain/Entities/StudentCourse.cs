using System.ComponentModel.DataAnnotations;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Entities
{
    public class StudentCourse
    {
        public int Id { get; set; }
        public StudentCourseStatus Status { get; set; }
        public string? Grade { get; set; }
        public SemesterType Semester { get; set; }
        public string AcademicYear { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public AppUser? Student { get; set; }
        public int CourseId { get; set; }
        public Course? Course { get; set; }
    }
}
