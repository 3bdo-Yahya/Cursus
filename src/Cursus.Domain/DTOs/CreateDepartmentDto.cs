using System.ComponentModel.DataAnnotations;

namespace Cursus.Domain.DTOs
{
    public class CreateDepartmentDto
    {
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
        public decimal MinGpaForGraduation { get; set; } = 2.00m;
        public bool IsActive { get; set; } = true;
    }
}