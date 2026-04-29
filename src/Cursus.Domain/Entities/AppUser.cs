using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Entities
{
    public class AppUser : IdentityUser
    {
        public int? DepartmentId { get; set; }

        public string? AcademicYear { get; set; }

        public SemesterType CurrentSemester { get; set; }

        public AcademicStanding CurrentStanding { get; set; }

        public Department? Department { get; set; }

        public ICollection<StudentCourse> StudentCourses { get; set; } = [];
        public ICollection<StandingHistory> StandingHistories { get; set; } = [];
    }
}