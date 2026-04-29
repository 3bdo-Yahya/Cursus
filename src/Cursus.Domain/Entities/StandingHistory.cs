using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Entities
{
    public class StandingHistory
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public SemesterType Semester { get; set; }
        public string AcademicYear { get; set; } = string.Empty;
        public decimal SemesterGpa { get; set; }
        public decimal CumulativeGpa { get; set; }
        public AcademicStanding Standing { get; set; }
        public AppUser? Student { get; set; }
    }
}