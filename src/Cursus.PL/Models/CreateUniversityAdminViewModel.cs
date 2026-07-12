using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cursus.PL.Models;

public class CreateUniversityAdminViewModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
        ErrorMessage = "Password needs upper, lower, digit, and a non-alphanumeric character.")]
    [DataType(DataType.Password)]
    [Display(Name = "Temporary Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a university.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a university.")]
    [Display(Name = "University")]
    public int UniversityId { get; set; }

    public IEnumerable<SelectListItem> UniversityOptions { get; set; } = [];
}

public class EditUniversityAdminViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a university.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a university.")]
    [Display(Name = "University")]
    public int UniversityId { get; set; }

    public IEnumerable<SelectListItem> UniversityOptions { get; set; } = [];
}
