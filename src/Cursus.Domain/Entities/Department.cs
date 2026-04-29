using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cursus.Domain.Entities
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int UniversityId { get; set; }
        [Range(1, 400)]
        public int TotalCreditsRequired { get; set; }
        [Range(typeof(decimal), "0.00", "4.00")]
        [Column(TypeName = "decimal(3,2)")]
        public decimal MinGpaForGraduation { get; set; } = 2.00m;
        public bool IsActive { get; set; } = true;
        public University? University { get; set; }
        public ICollection<AppUser> Students { get; set; } = [];
        public ICollection<Course> Courses { get; set; } = [];
        public ICollection<GraduationRequirement> GraduationRequirements { get; set; } = [];
        public ICollection<CreditHourRule> CreditHourRules { get; set; } = [];
    }
}
