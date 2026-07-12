using System.ComponentModel.DataAnnotations;
using Cursus.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cursus.Domain.Constants;

namespace Cursus.PL.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in.");

            var rootUrl = Url.Content("~/");
            if (!string.IsNullOrEmpty(returnUrl)
                && Url.IsLocalUrl(returnUrl)
                && !PathsEquivalent(returnUrl, rootUrl))
            {
                return LocalRedirect(returnUrl);
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user is not null)
            {
                if (await _userManager.IsInRoleAsync(user, Roles.SuperAdmin))
                    return RedirectToAction("Index", "Admin");

                if (await _userManager.IsInRoleAsync(user, Roles.Admin))
                    return RedirectToAction("Index", "Admin");

                if (await _userManager.IsInRoleAsync(user, Roles.Student))
                {
                    if (user.DepartmentId is null)
                        return RedirectToAction("Onboarding", "Student");

                    return RedirectToAction("Dashboard", "Student");
                }
            }

            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out.");
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return Page();
    }

    private static bool PathsEquivalent(string returnUrl, string rootUrl)
    {
        var destPath = returnUrl.Split('?', 2)[0].TrimEnd('/');
        var rootPath = rootUrl.TrimEnd('/');
        if (string.IsNullOrEmpty(destPath))
            destPath = "/";
        if (string.IsNullOrEmpty(rootPath))
            rootPath = "/";
        return string.Equals(destPath, rootPath, StringComparison.OrdinalIgnoreCase);
    }
}


