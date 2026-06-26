using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Cursus.Domain.Entities;
using Cursus.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Cursus.PL.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailService _emailService;

    public ForgotPasswordModel(UserManager<AppUser> userManager, IEmailService emailService)
    {
        _userManager = userManager;
        _emailService = emailService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);

        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var resetLink = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code, email = user.Email },
                protocol: Request.Scheme);

            var userName = user.UserName ?? user.Email ?? "User";
            var emailBody = $@"
                <!DOCTYPE html>
                <html>
                <body style='margin:0; padding:0; background:#F3F4F6; font-family: Arial, sans-serif;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='background:#F3F4F6; padding: 32px 16px;'>
                    <tr><td align='center'>
                    <table width='560' cellpadding='0' cellspacing='0' style='border-radius:12px; overflow:hidden; border:1px solid #E5E7EB;'>

                        <!-- Header -->
                        <tr>
                        <td style='background: linear-gradient(135deg, #312E81 0%, #4F46E5 60%, #6366F1 100%); padding: 36px 32px;'>
                            <h1 style='color:white; font-size:26px; font-weight:700; margin:0 0 4px;'>Cursus</h1>
                            <p style='color:rgba(255,255,255,0.7); font-size:13px; margin:0;'>Your AI-Powered Academic Advisor</p>
                        </td>
                        </tr>

                        <!-- Body -->
                        <tr>
                        <td style='background:#ffffff; padding: 36px 32px;'>
                            <h2 style='color:#111827; font-size:20px; font-weight:700; margin:0 0 8px;'>Password Reset Request</h2>
                            <p style='color:#6B7280; font-size:15px; line-height:1.6; margin:0 0 24px;'>
                            Hello <strong style='color:#111827;'>{HtmlEncoder.Default.Encode(userName)}</strong>,
                            we received a request to reset your Cursus account password.
                            Click the button below to set a new password.
                            </p>

                            <!-- CTA -->
                            <table width='100%' cellpadding='0' cellspacing='0'>
                            <tr><td align='center' style='padding: 8px 0 24px;'>
                                <a target='_blank' href='{HtmlEncoder.Default.Encode(resetLink!)}' 
                                style='display:inline-block; background:linear-gradient(135deg,#4F46E5,#6366F1); color:white; text-decoration:none; padding:14px 36px; border-radius:8px; font-size:15px; font-weight:600;'>
                                Reset Your Password
                                </a>
                            </td></tr>
                            </table>

                            <!-- Warning -->
                            <div style='background:#FEF3C7; border-left:3px solid #F59E0B; border-radius:4px; padding:12px 16px; margin-bottom:24px;'>
                            <p style='color:#92400E; font-size:13px; margin:0; font-weight:500;'>
                                &#9203; This link will expire in <strong>5 minutes</strong>. Request a new one if it expires.
                            </p>
                            </div>

                            <hr style='border:none; border-top:1px solid #F3F4F6; margin:0 0 20px;'/>

                            <p style='color:#9CA3AF; font-size:12px; line-height:1.6; margin:0 0 16px;'>
                            If the button doesn't work, copy and paste this link:<br/>
                            <span style='color:#6366F1; word-break:break-all; font-size:11px;'>{HtmlEncoder.Default.Encode(resetLink!)}</span>
                            </p>
                            <p style='color:#9CA3AF; font-size:12px; margin:0;'>
                            If you did not request a password reset, you can safely ignore this email.
                            </p>
                        </td>
                        </tr>

                        <!-- Footer -->
                        <tr>
                        <td style='background:#F9FAFB; padding:20px 32px; border-top:1px solid #F3F4F6; text-align:center;'>
                            <p style='color:#9CA3AF; font-size:11px; margin:0 0 4px;'>© 2026 Cursus · DEPI Graduation Project</p>
                            <p style='color:#D1D5DB; font-size:11px; margin:0;'>This is an automated email — please do not reply.</p>
                        </td>
                        </tr>

                    </table>
                    </td></tr>
                </table>
                </body>
                </html>";

            try
            {
                await _emailService.SendEmailAsync(user.Email ?? Input.Email, "Reset Your Password", emailBody);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email sending failed: {ex.Message}");
            }
        }

        return RedirectToPage("./ForgotPasswordConfirmation");
    }
}