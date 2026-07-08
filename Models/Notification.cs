using MedicalPractice.Status;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalPractice.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int EmployeeId { get; set; }
        public string Message { get; set; }
        public string? Link { get; set; }
        public string Icon { get; set; } = "bi-bell";
        public string Type { get; set; } = "info";
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public NotificationCategory Category { get; set; } = NotificationCategory.General;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        // ─── NEW ───────────────────────────────────────
        public DateTime? ExpiresAt { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }
    }
}