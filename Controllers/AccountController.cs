using MedicalPractice.Data;
using MedicalPractice.Models;
using MedicalPractice.Services;
using MedicalPractice.Status;
using MedicalPractice.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedicalPractice.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;

        public AccountController(ApplicationDbContext context, IEmailService email)
        {
            _context = context;
            _email = email;
        }

        // ── LOGIN ────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard();

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var employee = _context.Employees
                .FirstOrDefault(e => e.UserName == model.UserName);

            if (employee == null || employee.IsActive != AccountStatus.Active)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            bool validPassword = false;

            // Determine if the stored password is already a BCrypt hash
            if (!string.IsNullOrEmpty(employee.PasswordHash) &&
                employee.PasswordHash.StartsWith("$2"))
            {
                // Standard BCrypt verification
                validPassword = BCrypt.Net.BCrypt.Verify(model.Password, employee.PasswordHash);
            }
            else
            {
                // Plain‑text fallback (e.g., seeded data)
                validPassword = (model.Password == employee.PasswordHash);

                // If valid, upgrade the password to BCrypt immediately
                if (validPassword)
                {
                    string newHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    employee.PasswordHash = newHash;
                    await _context.SaveChangesAsync();
                }
            }

            if (!validPassword)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            // If 2FA is enabled, redirect to challenge
            if (employee.IsTwoFactorEnabled && !string.IsNullOrEmpty(employee.TwoFactorSecretKey))
            {
                TempData["2fa_pending_id"] = employee.EmployeeID.ToString();
                TempData["2fa_pending_type"] = "Employee";
                TempData["2fa_remember_me"] = model.RememberMe.ToString();
                TempData["2fa_return_url"] = returnUrl ?? string.Empty;
                return RedirectToAction("TwoFactorChallenge");
            }

            // Sign in directly (no 2FA)
            await SignInEmployeeAsync(employee, model.RememberMe);

            if (employee.MustChangePassword)
                return RedirectToAction("ChangePassword");

            return RedirectToSavedUrl(returnUrl, employee.Role);
        }
        // ── LOGOUT ───────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ── VIEW PROFILE ─────────────────────────────────────────

        [Authorize]
        [HttpGet]
        public IActionResult Profile()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out int id))
                return RedirectToAction("Login");

            var emp = _context.Employees.Find(id);
            if (emp == null)
                return RedirectToAction("Login");

            var model = new ProfileViewModel
            {
                UserName = emp.UserName,
                FullName = emp.FullName,
                Email = emp.Email,
                Role = emp.Role.ToString(),
                Status = emp.IsActive.ToString()
            };

            return View(model);
        }

        // ── CHANGE PASSWORD ─────────────────────────────────────

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out int id))
                return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return RedirectToAction("Login");

            string newHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            employee.PasswordHash = newHash;
            employee.MustChangePassword = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password changed successfully.";
            return RedirectToDashboard(employee.Role);
        }

        // ── FORGOT PASSWORD ──────────────────────────────────────

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string? firstName = null;
            string? email = null;

            string pin = new Random().Next(100000, 999999).ToString();
            string pinHash = BCrypt.Net.BCrypt.HashPassword(pin);

            var employee = _context.Employees.FirstOrDefault(e => e.Email == model.Email);

            if (employee != null && employee.IsActive == AccountStatus.Active)
            {
                employee.ResetPin = pinHash;
                employee.ResetPinExpiration = DateTime.Now.AddMinutes(15);
                firstName = employee.FirstName ?? employee.FullName;
                email = employee.Email;
                await _context.SaveChangesAsync();

                if (email != null)
                {
                    var body = $@"
                        <p>Hi {firstName},</p>
                        <p>Your password reset PIN is:</p>
                        <h2 style='letter-spacing:4px'>{pin}</h2>
                        <p>This PIN expires in <strong>15 minutes</strong>.</p>
                        <p>If you did not request this, please ignore this email.</p>";

                    await _email.SendAsync(email, "Medical Practice - Password Reset PIN", body);
                }
            }

            TempData["Info"] = "If that email is registered, a reset PIN has been sent.";
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        // ── RESET PASSWORD ───────────────────────────────────────

        [HttpGet]
        public IActionResult ResetPassword(string? email = null) =>
            View(new ResetPasswordViewModel { Email = email ?? string.Empty });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var employee = _context.Employees.FirstOrDefault(e => e.Email == model.Email);

            if (employee == null ||
                employee.ResetPin == null ||
                employee.ResetPinExpiration < DateTime.Now ||
                !BCrypt.Net.BCrypt.Verify(model.Pin, employee.ResetPin))
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired PIN.");
                return View(model);
            }

            string newHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            employee.PasswordHash = newHash;
            employee.ResetPin = null;
            employee.ResetPinExpiration = null;
            employee.FailedLoginAttempts = 0;
            employee.LockoutEnd = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password changed successfully. Please log in.";
            return RedirectToAction("Login");
        }

        // ── DEACTIVATE ACCOUNT ───────────────────────────────────

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out int id))
                return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.IsActive = AccountStatus.Inactive;
                await _context.SaveChangesAsync();
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Info"] = "Your account has been deactivated.";
            return RedirectToAction("Login");
        }

        // ── TWO‑FACTOR CHALLENGE ──────────────────────────────────

        [HttpGet]
        public IActionResult TwoFactorChallenge()
        {
            if (TempData["2fa_pending_id"] == null)
                return RedirectToAction("Login");

            // Keep TempData for the POST
            TempData.Keep("2fa_pending_id");
            TempData.Keep("2fa_pending_type");
            TempData.Keep("2fa_remember_me");
            TempData.Keep("2fa_return_url");

            return View(new TwoFactorChallengeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactorChallenge(
            TwoFactorChallengeViewModel model,
            [FromServices] ITwoFactorService tfService)
        {
            var pendingId = TempData["2fa_pending_id"]?.ToString();
            var pendingType = TempData["2fa_pending_type"]?.ToString();
            var rememberMe = bool.Parse(TempData["2fa_remember_me"]?.ToString() ?? "false");
            var returnUrl = TempData["2fa_return_url"]?.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(pendingId) || string.IsNullOrEmpty(pendingType))
                return RedirectToAction("Login");

            if (!int.TryParse(pendingId, out int id))
                return RedirectToAction("Login");

            if (pendingType == "Employee")
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return RedirectToAction("Login");

                var (verified, updatedCodes) = VerifyTwoFactor(
                    model, employee.TwoFactorSecretKey, employee.TwoFactorRecoveryCodes, tfService);

                if (!verified)
                {
                    TempData.Keep();
                    return View(model);
                }

                // Save updated recovery codes (if a recovery code was used)
                employee.TwoFactorRecoveryCodes = updatedCodes;
                await _context.SaveChangesAsync();

                // Sign in now
                await SignInEmployeeAsync(employee, rememberMe);

                if (employee.MustChangePassword)
                    return RedirectToAction("ChangePassword");

                return RedirectToSavedUrl(returnUrl, employee.Role);
            }

            // If other user types exist, handle similarly
            TempData.Keep();
            ModelState.AddModelError("", "Invalid user type for 2FA.");
            return View(model);
        }

        // ── TWO‑FACTOR SETUP ─────────────────────────────────────

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TwoFactorSetup(
            [FromServices] ITwoFactorService tfService)
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out int id))
                return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return RedirectToAction("Login");

            // If already enabled, redirect to manage page
            if (employee.IsTwoFactorEnabled)
                return RedirectToAction("TwoFactorManage");

            string email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var newSecret = tfService.GenerateSecretKey();
            var uri = tfService.GetQrCodeUri(newSecret, email, "Medical Practice");
            var qrPng = tfService.GenerateQrCodePng(uri);

            // We store the new secret temporarily, but do NOT persist yet.
            // The view will contain it in a hidden field.
            return View(new TwoFactorSetupViewModel
            {
                SecretKey = newSecret,
                QrCodeBase64 = Convert.ToBase64String(qrPng)
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactorSetup(
            TwoFactorSetupViewModel vm,
            [FromServices] ITwoFactorService tfService)
        {
            string emailAddr = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            if (!ModelState.IsValid)
            {
                vm.QrCodeBase64 = RegenerateQr(vm.SecretKey, emailAddr, tfService);
                return View(vm);
            }

            if (!tfService.VerifyCode(vm.SecretKey, vm.VerificationCode))
            {
                ModelState.AddModelError("VerificationCode",
                    "Code incorrect. Please ensure your device time is correct and try again.");
                vm.QrCodeBase64 = RegenerateQr(vm.SecretKey, emailAddr, tfService);
                return View(vm);
            }

            // Generate recovery codes (plain text shown to user once)
            var plainCodes = tfService.GenerateRecoveryCodes();
            var hashedCodes = plainCodes
                .Select(c => BCrypt.Net.BCrypt.HashPassword(c.ToUpper()))
                .ToList();
            var codesJson = System.Text.Json.JsonSerializer.Serialize(hashedCodes);

            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out int id)) return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.IsTwoFactorEnabled = true;
                employee.TwoFactorSecretKey = vm.SecretKey;
                employee.TwoFactorRecoveryCodes = codesJson;
                await _context.SaveChangesAsync();
            }

            // Store plain recovery codes in TempData for display
            TempData["RecoveryCodes"] = System.Text.Json.JsonSerializer.Serialize(plainCodes);

            return RedirectToAction("TwoFactorRecoveryCodes");
        }

        // ── TWO‑FACTOR RECOVERY CODES (after setup) ─────────────

        [Authorize]
        [HttpGet]
        public IActionResult TwoFactorRecoveryCodes()
        {
            var json = TempData["RecoveryCodes"]?.ToString();
            if (string.IsNullOrEmpty(json))
                return RedirectToAction("TwoFactorManage");

            var codes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json)
                        ?? new List<string>();

            return View(new TwoFactorRecoveryCodesViewModel { PlainCodes = codes });
        }

        // ── TWO‑FACTOR MANAGE ────────────────────────────────────

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TwoFactorManage()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out int id)) return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return RedirectToAction("Login");

            bool isEnabled = employee.IsTwoFactorEnabled;
            int codesLeft = CountRecoveryCodes(employee.TwoFactorRecoveryCodes);

            ViewData["IsEnabled"] = isEnabled;
            ViewData["CodesLeft"] = codesLeft;
            return View();
        }

        // ── TWO‑FACTOR DISABLE ───────────────────────────────────

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactorDisable()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out int id)) return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.IsTwoFactorEnabled = false;
                employee.TwoFactorSecretKey = null;
                employee.TwoFactorRecoveryCodes = null;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Two‑factor authentication has been disabled.";
            return RedirectToAction("TwoFactorManage");
        }

        // ── TWO‑FACTOR REGENERATE CODES ──────────────────────────

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactorRegenerateCodes(
            [FromServices] ITwoFactorService tfService)
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out int id)) return RedirectToAction("Login");

            var plainCodes = tfService.GenerateRecoveryCodes();
            var hashedCodes = plainCodes
                .Select(c => BCrypt.Net.BCrypt.HashPassword(c.ToUpper()))
                .ToList();
            var codesJson = System.Text.Json.JsonSerializer.Serialize(hashedCodes);

            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.TwoFactorRecoveryCodes = codesJson;
                await _context.SaveChangesAsync();
            }

            TempData["RecoveryCodes"] = System.Text.Json.JsonSerializer.Serialize(plainCodes);
            return RedirectToAction("TwoFactorRecoveryCodes");
        }

        // ── ACCESS DENIED ─────────────────────────────────────────

        public IActionResult AccessDenied() => View();

        // ── PRIVATE HELPERS ───────────────────────────────────────

        private async Task SignInEmployeeAsync(Employee employee, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, employee.EmployeeID.ToString()),
                new Claim(ClaimTypes.Name, employee.UserName),
                new Claim(ClaimTypes.Email, employee.Email),
                new Claim(ClaimTypes.GivenName, employee.FullName ?? employee.UserName),
                new Claim(ClaimTypes.Role, employee.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProps = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
        }

        private IActionResult RedirectToDashboard(UserRole? role = null)
        {
            if (role == null && User.Identity?.IsAuthenticated == true)
            {
                var roleStr = User.FindFirstValue(ClaimTypes.Role);
                Enum.TryParse<UserRole>(roleStr, out var parsed);
                role = parsed;
            }

            return role switch
            {
                UserRole.Admin => RedirectToAction("DashBoard", "Admin"),
                UserRole.Doctor => RedirectToAction("DashBoard", "Doctors"),
                UserRole.Assistant => RedirectToAction("DashBoard", "Assistance"),
                _ => RedirectToAction("Login", "Account")
            };
        }

        private IActionResult RedirectToSavedUrl(string? returnUrl, UserRole? role = null)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToDashboard(role);
        }

        private (bool verified, string? updatedCodes) VerifyTwoFactor(
            TwoFactorChallengeViewModel model,
            string? secretKey,
            string? storedCodes,
            ITwoFactorService tfService)
        {
            if (model.UseRecoveryCode)
            {
                if (string.IsNullOrWhiteSpace(model.RecoveryCode))
                {
                    ModelState.AddModelError("RecoveryCode", "Please enter a recovery code.");
                    return (false, storedCodes);
                }

                if (storedCodes == null ||
                    !tfService.VerifyRecoveryCode(storedCodes, model.RecoveryCode, out string updatedCodes))
                {
                    ModelState.AddModelError("RecoveryCode", "Invalid recovery code.");
                    return (false, storedCodes);
                }

                return (true, updatedCodes);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.Code))
                {
                    ModelState.AddModelError("Code", "Please enter your authentication code.");
                    return (false, storedCodes);
                }

                if (secretKey == null || !tfService.VerifyCode(secretKey, model.Code))
                {
                    ModelState.AddModelError("Code", "Invalid or expired code. Try again.");
                    return (false, storedCodes);
                }

                return (true, storedCodes);
            }
        }

        private static int CountRecoveryCodes(string? json)
        {
            if (string.IsNullOrEmpty(json)) return 0;
            try
            {
                var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                return list?.Count ?? 0;
            }
            catch { return 0; }
        }

        private string RegenerateQr(string secretKey, string email, ITwoFactorService tfService)
        {
            var uri = tfService.GetQrCodeUri(secretKey, email, "Medical Practice");
            var qrPng = tfService.GenerateQrCodePng(uri);
            return Convert.ToBase64String(qrPng);
        }
    }
}