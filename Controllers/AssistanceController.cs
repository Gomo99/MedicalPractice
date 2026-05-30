using MedicalPractice.Data;
using MedicalPractice.Models;
using MedicalPractice.Services;
using MedicalPractice.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalPractice.Controllers
{
    [Authorize(Roles = "Assistant")]
    public class AssistanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notif;

        private static readonly string[] MedicalAidCompanies = { "Discovery", "Momentum", "Bonitas" };

        public AssistanceController(ApplicationDbContext context, INotificationService notif)
        {
            _context = context;
            _notif = notif;
        }

        // ── DASHBOARD ────────────────────────────────────────────────────────
        public IActionResult DashBoard() => View();

        // ── INDEX – List & Search Patients ───────────────────────────────────
        public async Task<IActionResult> Index(string? searchString)
        {
            ViewData["CurrentFilter"] = searchString ?? string.Empty;

            var patients = from p in _context.Patients select p;

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                patients = patients.Where(p =>
                    p.Name.Contains(searchString) ||
                    p.Surname.Contains(searchString) ||
                    p.Username.Contains(searchString));
            }

            return View(await patients.AsNoTracking().ToListAsync());
        }

        // ── CREATE (GET) ──────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.MedicalAidCompanies = MedicalAidCompanies;
            return View(new PatientViewModel());
        }

        // ── CREATE (POST) ─────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientViewModel model)
        {
            if (model.MedicalAid && string.IsNullOrWhiteSpace(model.MedicalAidCompany))
                ModelState.AddModelError("MedicalAidCompany",
                    "Please select a medical aid provider.");

            if (!ModelState.IsValid)
            {
                ViewBag.MedicalAidCompanies = MedicalAidCompanies;
                return View(model);
            }

            if (await _context.Patients.AnyAsync(p => p.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                ViewBag.MedicalAidCompanies = MedicalAidCompanies;
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
                MedicalAidCompany = model.MedicalAid ? model.MedicalAidCompany : null
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // ── Notify all doctors that a new patient was registered ──────
            await _notif.CreateForRoleAsync(
                "Doctor",
                $"New patient '{patient.Name} {patient.Surname}' was added to the system.",
                "/Doctors/PatientList",
                "info",
                "bi-person-plus-fill");

            TempData["Success"] = $"Patient '{patient.Name} {patient.Surname}' added.";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT (GET) ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            var model = new PatientViewModel
            {
                PatientId = patient.PatientId,
                Name = patient.Name,
                Surname = patient.Surname,
                Username = patient.Username,
                Gender = patient.Gender,
                HomeAddress = patient.HomeAddress,
                EmailAddress = patient.EmailAddress,
                PhoneNumber = patient.PhoneNumber,
                MedicalAid = patient.MedicalAid,
                MedicalAidCompany = patient.MedicalAidCompany
            };

            ViewBag.MedicalAidCompanies = MedicalAidCompanies;
            return View(model);
        }

        // ── EDIT (POST) ───────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PatientViewModel model)
        {
            if (id != model.PatientId) return NotFound();

            if (model.MedicalAid && string.IsNullOrWhiteSpace(model.MedicalAidCompany))
                ModelState.AddModelError("MedicalAidCompany",
                    "Please select a medical aid provider.");

            if (!ModelState.IsValid)
            {
                ViewBag.MedicalAidCompanies = MedicalAidCompanies;
                return View(model);
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            if (await _context.Patients.AnyAsync(p =>
                    p.Username == model.Username && p.PatientId != id))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                ViewBag.MedicalAidCompanies = MedicalAidCompanies;
                return View(model);
            }

            patient.Name = model.Name;
            patient.Surname = model.Surname;
            patient.Username = model.Username;
            patient.Gender = model.Gender;
            patient.HomeAddress = model.HomeAddress;
            patient.EmailAddress = model.EmailAddress;
            patient.PhoneNumber = model.PhoneNumber;
            patient.MedicalAid = model.MedicalAid;
            patient.MedicalAidCompany = model.MedicalAid ? model.MedicalAidCompany : null;

            await _context.SaveChangesAsync();

            // ── Notify all doctors that a patient record was updated ──────
            await _notif.CreateForRoleAsync(
                "Doctor",
                $"Patient record for '{patient.Name} {patient.Surname}' was updated.",
                "/Doctors/PatientList",
                "info",
                "bi-pencil-square");

            TempData["Success"] = $"Patient '{patient.Name} {patient.Surname}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE (GET) – confirmation page ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        // ── DELETE (POST) ─────────────────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                string fullName = $"{patient.Name} {patient.Surname}";
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();

                // ── Notify all doctors that a patient was removed ─────────
                await _notif.CreateForRoleAsync(
                    "Doctor",
                    $"Patient '{fullName}' has been removed from the system.",
                    null,
                    "warning",
                    "bi-person-dash-fill");

                TempData["Success"] = $"Patient '{fullName}' deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}