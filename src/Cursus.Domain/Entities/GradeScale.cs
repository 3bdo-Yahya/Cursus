using System.ComponentModel.DataAnnotations.Schema;

namespace Cursus.Domain.Entities
{
    public class GradeScale
    {
        public int Id { get; set; }
        public int UniversityId { get; set; }
        public string LetterGrade { get; set; } = string.Empty;
        public decimal PointValue { get; set; }
        public University? University { get; set; }
    }
}
