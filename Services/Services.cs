using MedicalPractice.Data;
using MedicalPractice.Models;
using MedicalPractice.Status;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using QRCoder;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

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





    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context) => _context = context;

        // ── 1) Original Create (delegates to full) ──────────────────
        public Task CreateAsync(int employeeId, string message, string? link = null, string type = "info", string icon = "bi-bell")
            => CreateAsync(employeeId, message, NotificationCategory.General, NotificationPriority.Normal, null, link, type, icon);

        // ── 2) Category Create (delegates) ──────────────────────────
        public Task CreateAsync(int employeeId, string message, NotificationCategory category, string? link = null, string type = "info", string icon = "bi-bell")
            => CreateAsync(employeeId, message, category, NotificationPriority.Normal, null, link, type, icon);

        // ── 3) Priority Create (delegates) ──────────────────────────
        public Task CreateAsync(int employeeId, string message, NotificationCategory category, NotificationPriority priority, string? link = null, string type = "info", string icon = "bi-bell")
            => CreateAsync(employeeId, message, category, priority, null, link, type, icon);

        // ── 4) FULL OVERLOAD (with ExpiresAt) ────────────────────────
        public async Task CreateAsync(int employeeId, string message,
                                      NotificationCategory category,
                                      NotificationPriority priority,
                                      DateTime? expiresAt,
                                      string? link = null,
                                      string type = "info",
                                      string icon = "bi-bell")
        {
            _context.Notifications.Add(new Notification
            {
                EmployeeId = employeeId,
                Message = message,
                Category = category,
                Priority = priority,
                ExpiresAt = expiresAt,
                Link = link,
                Type = type,
                Icon = icon,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        // ── Role broadcast chain ─────────────────────────────────────
        public Task CreateForRoleAsync(string role, string message, string? link = null, string type = "info", string icon = "bi-bell")
            => CreateForRoleAsync(role, message, NotificationCategory.General, NotificationPriority.Normal, null, link, type, icon);

        public Task CreateForRoleAsync(string role, string message, NotificationCategory category, string? link = null, string type = "info", string icon = "bi-bell")
            => CreateForRoleAsync(role, message, category, NotificationPriority.Normal, null, link, type, icon);

        public Task CreateForRoleAsync(string role, string message, NotificationCategory category, NotificationPriority priority, string? link = null, string type = "info", string icon = "bi-bell")
            => CreateForRoleAsync(role, message, category, priority, null, link, type, icon);

        public async Task CreateForRoleAsync(string role, string message,
                                             NotificationCategory category,
                                             NotificationPriority priority,
                                             DateTime? expiresAt,
                                             string? link = null,
                                             string type = "info",
                                             string icon = "bi-bell")
        {
            if (!Enum.TryParse<UserRole>(role, out var userRole)) return;

            var recipientIds = await _context.Employees
                .Where(e => e.Role == userRole && e.IsActive == AccountStatus.Active)
                .Select(e => e.EmployeeID)
                .ToListAsync();

            foreach (var id in recipientIds)
            {
                _context.Notifications.Add(new Notification
                {
                    EmployeeId = id,
                    Message = message,
                    Category = category,
                    Priority = priority,
                    ExpiresAt = expiresAt,
                    Link = link,
                    Type = type,
                    Icon = icon,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (recipientIds.Count > 0)
                await _context.SaveChangesAsync();
        }

        // ── Read queries (with expiry filter) ─────────────────────────
        public async Task<int> GetUnreadCountAsync(int employeeId)
            => await _context.Notifications
                .Where(n => n.EmployeeId == employeeId
                         && !n.IsRead
                         && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow))
                .CountAsync();

        public async Task<List<NotificationDto>> GetRecentAsync(int employeeId, int count = 15)
        {
            var now = DateTime.UtcNow;
            var rows = await _context.Notifications
                .Where(n => n.EmployeeId == employeeId
                         && (n.ExpiresAt == null || n.ExpiresAt > now))   // ← key expiry filter
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();

            return rows.Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Message = n.Message,
                Link = n.Link,
                Icon = n.Icon,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                TimeAgo = ToTimeAgo(n.CreatedAt),
                Category = n.Category.ToString(),
                Priority = n.Priority.ToString(),
                ExpiresAt = n.ExpiresAt   // now exposed to frontend
            }).ToList();
        }

        // ── Mark as read (unchanged) ──────────────────────────────────
        public async Task MarkAsReadAsync(int notificationId, int employeeId)
        {
            var n = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.EmployeeId == employeeId);
            if (n is { IsRead: false })
            {
                n.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int employeeId)
        {
            var unread = await _context.Notifications
                .Where(n => n.EmployeeId == employeeId && !n.IsRead)
                .ToListAsync();
            foreach (var n in unread) n.IsRead = true;
            if (unread.Count > 0) await _context.SaveChangesAsync();
        }

        private static string ToTimeAgo(DateTime utc)
        {
            var diff = DateTime.UtcNow - utc;
            if (diff.TotalSeconds < 60) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return utc.ToLocalTime().ToString("MMM d");
        }
    }






    public class TwoFactorService : ITwoFactorService
    {
        public string GenerateSecretKey()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        public string GetQrCodeUri(string secretKey, string email, string issuer)
        {
            // otpauth://totp/{issuer}:{email}?secret={key}&issuer={issuer}
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedEmail = Uri.EscapeDataString(email);
            return $"otpauth://totp/{encodedIssuer}:{encodedEmail}" +
                   $"?secret={secretKey}&issuer={encodedIssuer}&digits=6&period=30";
        }

        public byte[] GenerateQrCodePng(string uri)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(6);
        }

        public bool VerifyCode(string secretKey, string code)
        {
            try
            {
                var keyBytes = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(keyBytes);

                // Allow 1 step of clock drift in each direction
                return totp.VerifyTotp(
                    code.Trim(),
                    out _,
                    new VerificationWindow(previous: 1, future: 1));
            }
            catch
            {
                return false;
            }
        }

        public List<string> GenerateRecoveryCodes()
        {
            var rng = new Random();
            var codes = new List<string>();

            for (int i = 0; i < 8; i++)
            {
                // Format: XXXX-XXXX  (8 hex chars)
                var part1 = rng.Next(0x1000, 0xFFFF).ToString("X4");
                var part2 = rng.Next(0x1000, 0xFFFF).ToString("X4");
                codes.Add($"{part1}-{part2}");
            }

            return codes;
        }

        public bool VerifyRecoveryCode(string storedJson, string inputCode,
                                        out string updatedJson)
        {
            updatedJson = storedJson;

            var codes = JsonSerializer.Deserialize<List<string>>(storedJson)
                        ?? new List<string>();

            // Recovery codes are stored as BCrypt hashes
            var matched = codes.FirstOrDefault(c =>
                BCrypt.Net.BCrypt.Verify(inputCode.Trim().ToUpper(), c));

            if (matched == null) return false;

            // Remove the used code (one-time use)
            codes.Remove(matched);
            updatedJson = JsonSerializer.Serialize(codes);
            return true;
        }
    }
}
