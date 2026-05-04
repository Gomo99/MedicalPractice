using MedicalPractice.Data;
using MedicalPractice.Models;
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
        private static readonly string[] MedicalAidCompanies = { "Discovery", "Momentum", "Bonitas" };

        public AssistanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── DASHBOARD ────────────────────────────────────────────
        public IActionResult DashBoard()
        {
            return View();  // Assistant dashboard with links to manage patients
        }

        // ── INDEX – List & Search Patients ─────────────────────
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

        // ── CREATE (GET) ────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.MedicalAidCompanies = MedicalAidCompanies;
            return View(new PatientViewModel());
        }

        // ── CREATE (POST) ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientViewModel model)
        {
            // Ensure medical aid company is selected if MedicalAid is true
            if (model.MedicalAid && string.IsNullOrWhiteSpace(model.MedicalAidCompany))
                ModelState.AddModelError("MedicalAidCompany", "Please select a medical aid provider.");

            if (!ModelState.IsValid)
            {
                ViewBag.MedicalAidCompanies = MedicalAidCompanies;
                return View(model);
            }

            // Check unique username
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

            TempData["Success"] = $"Patient '{patient.Name} {patient.Surname}' added.";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT (GET) ──────────────────────────────────────────
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

        // ── EDIT (POST) ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PatientViewModel model)
        {
            if (id != model.PatientId) return NotFound();

            if (model.MedicalAid && string.IsNullOrWhiteSpace(model.MedicalAidCompany))
                ModelState.AddModelError("MedicalAidCompany", "Please select a medical aid provider.");

            if (!ModelState.IsValid)
            {
                ViewBag.MedicalAidCompanies = MedicalAidCompanies;
                return View(model);
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            // Check unique username (exclude current patient)
            if (await _context.Patients.AnyAsync(p => p.Username == model.Username && p.PatientId != id))
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
            TempData["Success"] = $"Patient '{patient.Name} {patient.Surname}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE (GET) – confirmation page ───────────────────
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            return View(patient);
        }

        // ── DELETE (POST) ───────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Patient '{patient.Name} {patient.Surname}' deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}