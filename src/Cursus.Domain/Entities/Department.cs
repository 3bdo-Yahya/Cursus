using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cursus.Domain.Entities
{
    public class Department
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int UniversityId { get; set; }

        [Required]
        [Range(1, 400)]
        public int TotalCreditsRequired { get; set; }

        [Required]
        [Range(typeof(decimal), "0.00", "4.00")]
        [Column(TypeName = "decimal(3,2)")]
        public decimal MinGpaForGraduation { get; set; } = 2.00m;

        public bool IsActive { get; set; } = true;

        public University? University { get; set; }

        public ICollection<AppUser> Students { get; set; } = new List<AppUser>();
        public ICollection<Course> Courses { get; set; } = new List<Course>();
        public ICollection<GraduationRequirement> GraduationRequirements { get; set; } = new List<GraduationRequirement>();
        public ICollection<CreditHourRule> CreditHourRules { get; set; } = new List<CreditHourRule>();
    }
}
