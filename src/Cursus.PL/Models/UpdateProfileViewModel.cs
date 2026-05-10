using System.ComponentModel.DataAnnotations;

namespace Cursus.PL.Models
{
    public class UpdateProfileViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [EmailAddress]
        public string? Email { get; set; } // Optional: readonly/disabled in view, not posted from client

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }
    }
}
