using MedicalPractice.Status;

namespace MedicalPractice.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}