using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cursus.Models.Entities
{
    public class GradeScale
    {
        public int Id { get; set; }

        [Required]
        public int UniversityId { get; set; }
        
        [Required]
        [StringLength(2)]
        public string LetterGrade { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(3,2)")]
        public decimal PointValue { get; set; }

        public University? University { get; set; }
    }
}
