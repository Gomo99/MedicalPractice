using MedicalPractice.Data;
using MedicalPractice.Models;
using MedicalPractice.Services;
using MedicalPractice.Status;
using MedicalPractice.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalPractice.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;
        private readonly INotificationService _notif;

        public AdminController(ApplicationDbContext context,
                               IEmailService email,
                               INotificationService notif)
        {
            _context = context;
            _email = email;
            _notif = notif;
        }

        // ── DASHBOARD – List all employees ─────────────────────────────────
        public async Task<IActionResult> DashBoard()
        {
            var employees = await _context.Employees
                .OrderBy(e => e.Role)
                .ThenBy(e => e.UserName)
                .ToListAsync();
            return View(employees);
        }

        public async Task<IActionResult> Index()
            => await Task.FromResult(RedirectToAction(nameof(DashBoard)));

        // ── CREATE (GET) ────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Roles = Enum.GetValues(typeof(UserRole))
                .Cast<UserRole>()
                .Where(r => r != UserRole.Admin)
                .ToList();
            return View(new CreateEmployeeViewModel());
        }

        // ── CREATE (POST) ───────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = RolesExceptAdmin();
                return View(model);
            }

            bool userExists = await _context.Employees.AnyAsync(
                e => e.UserName == model.UserName || e.Email == model.Email);

            if (userExists)
            {
                ModelState.AddModelError(string.Empty,
                    "A user with this username or email already exists.");
                ViewBag.Roles = RolesExceptAdmin();
                return View(model);
            }

            string generatedPassword = GenerateRandomPassword(12);

            var employee = new Employee
            {
                UserName = model.UserName,
                Email = model.Email,
                FullName = model.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(generatedPassword),
                Role = model.Role,
                IsActive = AccountStatus.Active
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // ── Email the new employee their credentials ──────────────────
            string subject = "Your Medical Practice Account";
            string body = $@"
                <p>Hello {employee.FullName},</p>
                <p>Your account has been created. Use the following credentials to log in:</p>
                <p><strong>Username:</strong> {employee.UserName}<br/>
                   <strong>Password:</strong> {generatedPassword}</p>
                <p>Please change your password after logging in.</p>
                <p>Regards,<br/>Medical Practice</p>";

            await _email.SendAsync(employee.Email, subject, body);

            // ── Notify all admins that a new account was created ──────────
            await _notif.CreateForRoleAsync(
                "Admin",
                $"New {employee.Role} account '{employee.UserName}' ({employee.FullName}) was created.",
                "/Admin/DashBoard",
                "success",
                "bi-person-check-fill");

            TempData["Success"] =
                $"Account '{employee.UserName}' created. Password sent to {employee.Email}.";
            return RedirectToAction(nameof(DashBoard));
        }

        // ── EDIT (GET) ──────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            var model = new EditEmployeeViewModel
            {
                EmployeeID = employee.EmployeeID,
                UserName = employee.UserName,
                Email = employee.Email,
                FullName = employee.FullName,
                Role = employee.Role
            };
            ViewBag.Roles = AllRoles();
            return View(model);
        }

        // ── EDIT (POST) ─────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditEmployeeViewModel model)
        {
            if (id != model.EmployeeID) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = AllRoles();
                return View(model);
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            bool duplicate = await _context.Employees.AnyAsync(e =>
                e.EmployeeID != id &&
                (e.UserName == model.UserName || e.Email == model.Email));

            if (duplicate)
            {
                ModelState.AddModelError(string.Empty,
                    "Another user already has this username or email.");
                ViewBag.Roles = AllRoles();
                return View(model);
            }

            employee.UserName = model.UserName;
            employee.Email = model.Email;
            employee.FullName = model.FullName;
            employee.Role = model.Role;
            await _context.SaveChangesAsync();

            // ── Notify all admins about the edit ──────────────────────────
            await _notif.CreateForRoleAsync(
                "Admin",
                $"Account '{employee.UserName}' was updated by an admin.",
                "/Admin/DashBoard",
                "info",
                "bi-pencil-square");

            TempData["Success"] = $"Account '{employee.UserName}' updated.";
            return RedirectToAction(nameof(DashBoard));
        }

        // ── TOGGLE STATUS ────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            employee.IsActive = employee.IsActive == AccountStatus.Active
                ? AccountStatus.Inactive
                : AccountStatus.Active;

            await _context.SaveChangesAsync();

            string statusLabel = employee.IsActive == AccountStatus.Active
                ? "activated" : "deactivated";

            // ── Notify all admins about the status change ─────────────────
            string notifType = employee.IsActive == AccountStatus.Active
                ? "success" : "warning";
            string notifIcon = employee.IsActive == AccountStatus.Active
                ? "bi-person-check-fill" : "bi-person-dash-fill";

            await _notif.CreateForRoleAsync(
                "Admin",
                $"Account '{employee.UserName}' was {statusLabel}.",
                "/Admin/DashBoard",
                notifType,
                notifIcon);

            // ── Notify the affected employee themselves ────────────────────
            await _notif.CreateAsync(
                employee.EmployeeID,
                $"Your account has been {statusLabel} by an administrator.",
                null,
                notifType,
                notifIcon);

            TempData["Success"] = $"Account '{employee.UserName}' {statusLabel}.";
            return RedirectToAction(nameof(DashBoard));
        }

        // ── PRIVATE HELPERS ──────────────────────────────────────────────────
        private static List<UserRole> RolesExceptAdmin()
            => Enum.GetValues(typeof(UserRole))
                   .Cast<UserRole>()
                   .Where(r => r != UserRole.Admin)
                   .ToList();

        private static List<UserRole> AllRoles()
            => Enum.GetValues(typeof(UserRole)).Cast<UserRole>().ToList();

        private static string GenerateRandomPassword(int length = 12)
        {
            const string chars =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%&*";
            var rng = new Random();
            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }
    }
}