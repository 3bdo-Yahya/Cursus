using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cursus.Models.Enums;

namespace Cursus.Models.Entities
{
    public class StandingHistory
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public SemesterType Semester { get; set; }

        [Required]
        [StringLength(10)]
        public string AcademicYear { get; set; } = string.Empty;

        [Column(TypeName = "decimal(3,2)")]
        public decimal SemesterGpa { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal CumulativeGpa { get; set; }

        [Required]
        public AcademicStanding Standing { get; set; }

        public AppUser? Student { get; set; }
    }
}