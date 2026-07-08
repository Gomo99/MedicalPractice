using MedicalPractice.Services;
using System.Net;
using System.Net.Mail;

namespace MedicalPractice.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                // ── READ CONFIG (matching your JSON) ───────────────
                var smtp = _config["Email:Host"];
                var portStr = _config["Email:Port"];
                var user = _config["Email:Username"];
                var pass = _config["Email:Password"];
                var from = _config["Email:SenderEmail"];

                // ── VALIDATION (prevents crashes) ──────────────────
                if (string.IsNullOrEmpty(smtp))
                    throw new Exception("Email Host is not configured.");

                if (string.IsNullOrEmpty(portStr) || !int.TryParse(portStr, out int port))
                    throw new Exception("Email Port is not configured correctly.");

                if (string.IsNullOrEmpty(user))
                    throw new Exception("Email Username is missing.");

                if (string.IsNullOrEmpty(pass))
                    throw new Exception("Email Password is missing.");

                if (string.IsNullOrEmpty(from))
                    throw new Exception("Email Sender address is missing.");

                // ── SMTP CLIENT ───────────────────────────────────
                using var client = new SmtpClient(smtp, port)
                {
                    Credentials = new NetworkCredential(user, pass),
                    EnableSsl = true
                };

                // ── EMAIL MESSAGE ─────────────────────────────────
                var message = new MailMessage(from, toEmail, subject, htmlBody)
                {
                    IsBodyHtml = true
                };

                // ── SEND EMAIL ────────────────────────────────────
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                // Log error (VERY IMPORTANT for debugging)
                Console.WriteLine("EMAIL ERROR: " + ex.Message);

                // Re-throw so you see it in UI during development
                throw;
            }
        }
    }
}