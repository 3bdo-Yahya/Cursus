using System.ComponentModel.DataAnnotations;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Entities
{
    public class CreditHourRule
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public AcademicStanding Standing { get; set; }
        public int MinCredits { get; set; }
        public int MaxCredits { get; set; }
        public Department? Department { get; set; }
    }
}