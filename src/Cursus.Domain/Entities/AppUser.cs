using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Entities
{
    public class AppUser : IdentityUser
    {
        public int? UniversityId { get; set; }

        public int? DepartmentId { get; set; }

        [StringLength(10)]
        public string? AcademicYear { get; set; }

        public SemesterType CurrentSemester { get; set; }

        public AcademicStanding CurrentStanding { get; set; }

        public University? University { get; set; }
        public Department? Department { get; set; }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(UserName)) return "Student";
                var namePart = UserName.Split('@')[0];
                var parts = namePart.Split('.', StringSplitOptions.RemoveEmptyEntries);
                return string.Join(" ", parts.Select(p => p.Length > 0 ? char.ToUpper(p[0]) + p[1..].ToLower() : p));
            }
        }

        public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
        public ICollection<StandingHistory> StandingHistories { get; set; } = new List<StandingHistory>();
    }
}
