using System.Net;
using System.Net.Mail;
using Cursus.Domain.DTOs;
using Microsoft.Extensions.Options;

namespace Cursus.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MailMessage(_settings.From, toEmail, subject, body)
            {
                IsBodyHtml = true
            };

            using var client = new SmtpClient(_settings.SmtpServer, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }
    }
}