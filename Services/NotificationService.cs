using MedicalPractice.Data;
using MedicalPractice.Models;
using MedicalPractice.Status;
using Microsoft.EntityFrameworkCore;

namespace MedicalPractice.Services
{
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
}