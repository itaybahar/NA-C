using Domain_Project.Interfaces;
using System.Net;
using System.Net.Mail;

namespace API_Project.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpHost = _configuration["Email:Smtp:Host"];
            var smtpPortString = _configuration["Email:Smtp:Port"];
            var smtpUser = _configuration["Email:Smtp:Username"];
            var smtpPass = _configuration["Email:Smtp:Password"];
            var fromEmail = _configuration["Email:From"];

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPortString) ||
                string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass) ||
                string.IsNullOrEmpty(fromEmail))
            {
                throw new InvalidOperationException("Email configuration is missing or invalid.");
            }

            if (!int.TryParse(smtpPortString, out var smtpPort))
            {
                throw new InvalidOperationException("SMTP port configuration is invalid.");
            }

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
