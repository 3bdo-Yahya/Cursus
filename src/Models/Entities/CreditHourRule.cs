using System.ComponentModel.DataAnnotations;
using Cursus.Models.Enums;

namespace Cursus.Models.Entities
{
    public class CreditHourRule
    {
        public int Id { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public AcademicStanding Standing { get; set; }

        [Required]
        public int MinCredits { get; set; }

        [Required]
        public int MaxCredits { get; set; }

        public Department? Department { get; set; }
    }
}