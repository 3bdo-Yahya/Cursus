using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Cursus.Domain.DTOs;
using Cursus.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cursus.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            bool isSmtpConfigured = !string.IsNullOrWhiteSpace(_settings.SmtpServer)
                                    && !string.IsNullOrWhiteSpace(_settings.Username)
                                    && !string.IsNullOrWhiteSpace(_settings.Password)
                                    && _settings.Password != "YOUR_SMTP_PASSWORD"
                                    && _settings.Username != "YOUR_SMTP_USERNAME";

            if (isSmtpConfigured)
            {
                try
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
                    _logger.LogInformation("Email sent successfully to {Email} via SMTP.", toEmail);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SMTP email delivery to {Email} failed. Falling back to Console.", toEmail);
                }
            }
            else
            {
                _logger.LogInformation("SMTP is not configured or using placeholders. Falling back to Console.");
            }

            // Console Fallback
            WriteEmailToConsole(toEmail, subject, body);
        }

        private void WriteEmailToConsole(string toEmail, string subject, string body)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================================================================");
            Console.WriteLine("📧 [LOCAL EMAIL FALLBACK - SMTP NOT CONFIGURED OR FAILED]");
            Console.WriteLine($"To:      {toEmail}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine(body);
            Console.WriteLine("================================================================================");
            Console.ResetColor();
        }
    }
}