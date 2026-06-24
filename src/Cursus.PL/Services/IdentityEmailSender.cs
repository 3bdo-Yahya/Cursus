using Cursus.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Cursus.PL.Services
{
    public class IdentityEmailSender : IEmailSender
    {
        private readonly IEmailService _emailService;

        public IdentityEmailSender(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return _emailService.SendEmailAsync(email, subject, htmlMessage);
        }
    }
}
