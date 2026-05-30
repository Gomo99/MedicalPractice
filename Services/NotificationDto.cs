namespace MedicalPractice.Services
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Message { get; set; }
        public string? Link { get; set; }
        public string Icon { get; set; }
        public string Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }

        // ─── NEW ───────────────────────────────────────
        public DateTime? ExpiresAt { get; set; }
    }
}