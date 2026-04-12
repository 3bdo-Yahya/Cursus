using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Cursus.Models.Enums;

namespace Cursus.Models.Entities
{
    public class AppUser : IdentityUser
    {
        public int? DepartmentId { get; set; }

        [StringLength(10)]
        public string? AcademicYear { get; set; }

        public SemesterType CurrentSemester { get; set; }

        public AcademicStanding CurrentStanding { get; set; }

        public Department? Department { get; set; }

        public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
        public ICollection<StandingHistory> StandingHistories { get; set; } = new List<StandingHistory>();
    }
}