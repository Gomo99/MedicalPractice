using MedicalPractice.Data;
using MedicalPractice.Models;
using MedicalPractice.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedicalPractice.Controllers
{
    public class PatientAuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientAuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── REGISTER (GET) ─────────────────────────────────────
        [HttpGet]
        public IActionResult Register()
        {
            return View(new PatientRegisterViewModel());
        }

        // ── REGISTER (POST) ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(PatientRegisterViewModel model)
        {
            if (model.MedicalAid && string.IsNullOrWhiteSpace(model.MedicalAidCompany))
                ModelState.AddModelError("MedicalAidCompany", "Please select a medical aid provider.");

            if (!ModelState.IsValid)
                return View(model);

            if (await _context.Patients.AnyAsync(p => p.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View(model);
            }

            var patient = new Patient
            {
                Name = model.Name,
                Surname = model.Surname,
                Username = model.Username,
                Gender = model.Gender,
                HomeAddress = model.HomeAddress,
                EmailAddress = model.EmailAddress,
                PhoneNumber = model.PhoneNumber,
                MedicalAid = model.MedicalAid,
                MedicalAidCompany = model.MedicalAid ? model.MedicalAidCompany : null,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                IsActive = true,
                MustChangePassword = false
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Automatically sign in the new patient
            await SignInPatient(patient, isPersistent: true);

            TempData["Success"] = "Registration successful. Welcome!";
            return RedirectToAction("Dashboard", "Patient");
        }

        // ── LOGIN (GET) ────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Dashboard", "Patient");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ── LOGIN (POST) ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(PatientLoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Username == model.Username);

            if (patient == null || !patient.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            // Lockout check
            if (patient.LockoutEnd.HasValue && patient.LockoutEnd.Value > DateTime.UtcNow)
            {
                int remaining = (int)(patient.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes + 1;
                ModelState.AddModelError(string.Empty, $"Account locked. Try again in {remaining} minute(s).");
                return View(model);
            }

            // Password verification
            bool validPassword = false;
            if (!string.IsNullOrEmpty(patient.PasswordHash))
            {
                validPassword = BCrypt.Net.BCrypt.Verify(model.Password, patient.PasswordHash);
            }

            if (!validPassword)
            {
                patient.FailedLoginAttempts++;
                if (patient.FailedLoginAttempts >= 5)
                {
                    patient.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    patient.FailedLoginAttempts = 0;
                    await _context.SaveChangesAsync();
                    ModelState.AddModelError(string.Empty, "Too many failed attempts. Account locked for 15 minutes.");
                }
                else
                {
                    await _context.SaveChangesAsync();
                    int attemptsLeft = 5 - patient.FailedLoginAttempts;
                    ModelState.AddModelError(string.Empty, $"Invalid password. {attemptsLeft} attempt(s) remaining.");
                }
                return View(model);
            }

            // Success – reset counters
            patient.FailedLoginAttempts = 0;
            patient.LockoutEnd = null;
            await _context.SaveChangesAsync();

            await SignInPatient(patient, model.RememberMe);

            if (patient.MustChangePassword)
                return RedirectToAction("ChangePassword", "Patient");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Dashboard", "Patient");
        }

        // ── LOGOUT ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ── FORGOT PASSWORD (GET) ──────────────────────────────
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        // ── FORGOT PASSWORD (POST) ─────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Email is required.");
                return View();
            }

            string pin = new Random().Next(100000, 999999).ToString();
            string pinHash = BCrypt.Net.BCrypt.HashPassword(pin);

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.EmailAddress == email);
            if (patient != null && patient.IsActive)
            {
                patient.ResetPin = pinHash;
                patient.ResetPinExpiration = DateTime.Now.AddMinutes(15);
                await _context.SaveChangesAsync();

                // Send email with PIN (reuse IEmailService – ensure it is injected into this controller)
                // For brevity, the email sending code is omitted. You can copy the pattern from AccountController.
            }

            TempData["Info"] = "If that email is registered, a reset PIN has been sent.";
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        // ── RESET PASSWORD (GET) ───────────────────────────────
        [HttpGet]
        public IActionResult ResetPassword(string? email = null) =>
            View(new ResetPasswordViewModel { Email = email ?? string.Empty });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.EmailAddress == model.Email);
            if (patient == null || patient.ResetPin == null || patient.ResetPinExpiration < DateTime.Now
                || !BCrypt.Net.BCrypt.Verify(model.Pin, patient.ResetPin))
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired PIN.");
                return View(model);
            }

            patient.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            patient.ResetPin = null;
            patient.ResetPinExpiration = null;
            patient.FailedLoginAttempts = 0;
            patient.LockoutEnd = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password changed. Please log in.";
            return RedirectToAction("Login");
        }

        // ── PRIVATE SIGN‑IN HELPER ────────────────────────────
        private async Task SignInPatient(Patient patient, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, patient.PatientId.ToString()),
                new Claim(ClaimTypes.Name, patient.Username),
                new Claim(ClaimTypes.Email, patient.EmailAddress),
                new Claim(ClaimTypes.GivenName, $"{patient.Name} {patient.Surname}"),
                new Claim(ClaimTypes.Role, "Patient")   // Role for authorization
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProps = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
        }
    }
}