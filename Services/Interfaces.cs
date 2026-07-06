using MedicalPractice.Status;

namespace MedicalPractice.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }


    public interface INotificationService
    {
        // ── Original methods (unchanged) ───────────────────────────
        Task CreateAsync(int employeeId, string message, string? link = null, string type = "info", string icon = "bi-bell");
        Task CreateForRoleAsync(string role, string message, string? link = null, string type = "info", string icon = "bi-bell");

        // ── Category overloads (unchanged) ─────────────────────────
        Task CreateAsync(int employeeId, string message, NotificationCategory category, string? link = null, string type = "info", string icon = "bi-bell");
        Task CreateForRoleAsync(string role, string message, NotificationCategory category, string? link = null, string type = "info", string icon = "bi-bell");

        // ── Priority overloads (unchanged) ─────────────────────────
        Task CreateAsync(int employeeId, string message, NotificationCategory category, NotificationPriority priority, string? link = null, string type = "info", string icon = "bi-bell");
        Task CreateForRoleAsync(string role, string message, NotificationCategory category, NotificationPriority priority, string? link = null, string type = "info", string icon = "bi-bell");

        // ── NEW: Expiry overloads ─────────────────────────────────
        Task CreateAsync(int employeeId, string message, NotificationCategory category, NotificationPriority priority, DateTime? expiresAt, string? link = null, string type = "info", string icon = "bi-bell");
        Task CreateForRoleAsync(string role, string message, NotificationCategory category, NotificationPriority priority, DateTime? expiresAt, string? link = null, string type = "info", string icon = "bi-bell");

        // ── Readers / markers (unchanged) ──────────────────────────
        Task<int> GetUnreadCountAsync(int employeeId);
        Task<List<NotificationDto>> GetRecentAsync(int employeeId, int count = 15);
        Task MarkAsReadAsync(int notificationId, int employeeId);
        Task MarkAllAsReadAsync(int employeeId);
    }


    public interface ITwoFactorService
    {
        string GenerateSecretKey();
        string GetQrCodeUri(string secretKey, string email, string issuer);
        byte[] GenerateQrCodePng(string uri);
        bool VerifyCode(string secretKey, string code);
        List<string> GenerateRecoveryCodes();
        bool VerifyRecoveryCode(string storedJson, string inputCode, out string updatedJson);
    }
}
