using MedicalPractice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedicalPractice.Controllers
{
    /// <summary>
    /// Lightweight JSON API consumed by the notification bell in _Layout.cshtml.
    /// All routes are under /Notifications/...
    /// </summary>
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _svc;

        public NotificationsController(INotificationService svc) => _svc = svc;

        // ── GET /Notifications/Count ─────────────────────────────────────────
        // Returns { "count": N }  – polled every 60 s by the bell script.
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var id = CurrentEmployeeId();
            if (id is null) return Json(new { count = 0 });

            var count = await _svc.GetUnreadCountAsync(id.Value);
            return Json(new { count });
        }

        // ── GET /Notifications/Recent ────────────────────────────────────────
        // Returns JSON array of the 15 most-recent notifications.
        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            var id = CurrentEmployeeId();
            if (id is null) return Json(Array.Empty<object>());

            var list = await _svc.GetRecentAsync(id.Value, 15);
            return Json(list);
        }

        // ── POST /Notifications/MarkRead/5 ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var empId = CurrentEmployeeId();
            if (empId is not null)
                await _svc.MarkAsReadAsync(id, empId.Value);

            return Json(new { ok = true });
        }

        // ── POST /Notifications/MarkAllRead ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var empId = CurrentEmployeeId();
            if (empId is not null)
                await _svc.MarkAllAsReadAsync(empId.Value);

            return Json(new { ok = true });
        }

        // ── HELPER ────────────────────────────────────────────────────────────
        private int? CurrentEmployeeId()
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(s, out int id) ? id : null;
        }
    }
}