using System.ComponentModel.DataAnnotations;

namespace Cursus.Domain.DTOs
{
    public class CreateUniversityDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}