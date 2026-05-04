using MedicalPractice.Data;
using MedicalPractice.Models;
using MedicalPractice.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedicalPractice.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── DASHBOARD ────────────────────────────────────────────
        public IActionResult DashBoard()
        {
            return View();
        }

        // ── PATIENT LIST (with search) ──────────────────────────
        public async Task<IActionResult> PatientList(string? searchString)
        {
            ViewData["CurrentFilter"] = searchString ?? string.Empty;

            var patients = from p in _context.Patients select p;

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                // Search by patient number (PatientId), name, surname, or username
                patients = patients.Where(p =>
                    p.PatientId.ToString().Contains(searchString) ||
                    p.Name.Contains(searchString) ||
                    p.Surname.Contains(searchString) ||
                    p.Username.Contains(searchString));
            }

            return View(await patients.AsNoTracking().OrderBy(p => p.Surname).ToListAsync());
        }

        // ── PATIENT HISTORY (all visits for a patient) ──────────
        public async Task<IActionResult> PatientHistory(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            var visits = await _context.Visits
                .Where(v => v.PatientId == id)
                .Include(v => v.Doctor)
                .OrderByDescending(v => v.VisitDateTime)
                .ToListAsync();

            ViewBag.Patient = patient;    // to show patient name in the view header
            return View(visits);
        }

        // ── ADD VISIT (GET) ─────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> AddVisit(int? patientId)
        {
            if (patientId == null) return NotFound();

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            ViewBag.PatientName = $"{patient.Name} {patient.Surname}";

            var model = new VisitViewModel
            {
                PatientId = patientId.Value,
                VisitDateTime = DateTime.Now
            };
            return View(model);
        }

        // ── ADD VISIT (POST) ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVisit(VisitViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Retain patient name in case of error
                var patient = await _context.Patients.FindAsync(model.PatientId);
                ViewBag.PatientName = patient != null ? $"{patient.Name} {patient.Surname}" : "Unknown";
                return View(model);
            }

            // Get current doctor ID from claims
            var doctorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(doctorIdStr, out int doctorId))
                return RedirectToAction("Login", "Account");

            var visit = new Visit
            {
                PatientId = model.PatientId,
                DoctorId = doctorId,
                VisitDateTime = model.VisitDateTime,
                Diagnosis = model.Diagnosis,
                Treatment = model.Treatment
            };

            _context.Visits.Add(visit);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Visit recorded.";
            return RedirectToAction("PatientHistory", new { id = model.PatientId });
        }

        // ── EDIT VISIT (GET) ────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> EditVisit(int? id)
        {
            if (id == null) return NotFound();

            var visit = await _context.Visits
                .Include(v => v.Patient)
                .FirstOrDefaultAsync(v => v.VisitId == id);
            if (visit == null) return NotFound();

            // Optional: ensure the logged-in doctor is the one who created the visit
            var doctorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(doctorIdStr, out int doctorId) || visit.DoctorId != doctorId)
                return Forbid();

            var model = new VisitViewModel
            {
                VisitId = visit.VisitId,
                PatientId = visit.PatientId,
                VisitDateTime = visit.VisitDateTime,
                Diagnosis = visit.Diagnosis,
                Treatment = visit.Treatment
            };

            ViewBag.PatientName = $"{visit.Patient.Name} {visit.Patient.Surname}";
            return View(model);
        }

        // ── EDIT VISIT (POST) ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVisit(int id, VisitViewModel model)
        {
            if (id != model.VisitId) return NotFound();

            if (!ModelState.IsValid)
            {
                var tempPatient = await _context.Patients.FindAsync(model.PatientId);
                ViewBag.PatientName = tempPatient != null ? $"{tempPatient.Name} {tempPatient.Surname}" : "Unknown";
                return View(model);
            }

            var visit = await _context.Visits.FindAsync(id);
            if (visit == null) return NotFound();

            // Again, verify ownership
            var doctorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(doctorIdStr, out int doctorId) || visit.DoctorId != doctorId)
                return Forbid();

            visit.VisitDateTime = model.VisitDateTime;
            visit.Diagnosis = model.Diagnosis;
            visit.Treatment = model.Treatment;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Visit updated.";
            return RedirectToAction("PatientHistory", new { id = model.PatientId });
        }
    }
}