using MedicalPractice.Data;
using MedicalPractice.Models;
using MedicalPractice.Status;
using MedicalPractice.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MedicalPractice.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── DASHBOARD ──────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            int patientId = GetCurrentPatientId();
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return RedirectToAction("Login", "PatientAuth");

            var appointments = await _context.Appointments
                .Where(a => a.PatientId == patientId && a.AppointmentDateTime >= DateTime.Now)
                .OrderBy(a => a.AppointmentDateTime)
                .Include(a => a.Doctor)
                .ToListAsync();

            ViewBag.Patient = patient;
            return View(appointments);
        }

        // ── VIEW ALL APPOINTMENTS (with date filter) ───────────
        [HttpGet]
        public async Task<IActionResult> Appointments(DateTime? startDate, DateTime? endDate)
        {
            int patientId = GetCurrentPatientId();
            if (patientId == 0) return RedirectToAction("Login", "PatientAuth");

            IQueryable<Appointment> query = _context.Appointments
                .Where(a => a.PatientId == patientId)
                .Include(a => a.Doctor);

            // Apply date filters if provided
            if (startDate.HasValue)
                query = query.Where(a => a.AppointmentDateTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(a => a.AppointmentDateTime <= endDate.Value);

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            // Store filter values for the view
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(appointments);
        }

        // ── CANCEL APPOINTMENT ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            int patientId = GetCurrentPatientId();
            var appointment = await _context.Appointments.FindAsync(appointmentId);

            if (appointment == null || appointment.PatientId != patientId)
                return NotFound();

            // Only allow cancellation if it's Booked and more than 24 hours ahead
            if (appointment.Status == AppointmentStatus.Booked &&
                appointment.AppointmentDateTime > DateTime.Now.AddHours(24))
            {
                appointment.Status = AppointmentStatus.Cancelled;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment cancelled.";
            }
            else
            {
                TempData["Error"] = "Appointment cannot be cancelled. It must be 'Booked' and at least 24 hours before the scheduled time.";
            }

            return RedirectToAction("Appointments");
        }

        // ── REQUEST RESCHEDULE ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReschedule(int appointmentId)
        {
            int patientId = GetCurrentPatientId();
            var appointment = await _context.Appointments.FindAsync(appointmentId);

            if (appointment == null || appointment.PatientId != patientId)
                return NotFound();

            // Allow reschedule request only if status is Booked
            if (appointment.Status == AppointmentStatus.Booked)
            {
                appointment.Status = AppointmentStatus.RescheduleRequested;
                appointment.RescheduleRequestedAt = DateTime.Now;
                // Optional: Keep the original reason, the receptionist can see it.
                await _context.SaveChangesAsync();

                TempData["Success"] = "Reschedule request sent to the receptionist.";
            }
            else
            {
                TempData["Error"] = "Only booked appointments can be rescheduled.";
            }

            return RedirectToAction("Appointments");
        }

        // ── EDIT PROFILE / CHANGE PASSWORD (unchanged) ────────
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            int patientId = GetCurrentPatientId();
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return RedirectToAction("Login", "PatientAuth");

            var model = new PatientProfileViewModel
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
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(PatientProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            int patientId = GetCurrentPatientId();
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return RedirectToAction("Login", "PatientAuth");

            patient.HomeAddress = model.HomeAddress;
            patient.EmailAddress = model.EmailAddress;
            patient.PhoneNumber = model.PhoneNumber;
            if (patient.MedicalAid)
                patient.MedicalAidCompany = model.MedicalAidCompany;
            else
                patient.MedicalAidCompany = null;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated.";
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            int patientId = GetCurrentPatientId();
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return RedirectToAction("Login", "PatientAuth");

            patient.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            patient.MustChangePassword = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password changed.";
            return RedirectToAction("Dashboard");
        }



        [HttpGet]
        public async Task<IActionResult> RequestAppointment()
        {
            var doctors = await _context.Employees
                .Where(e => e.Role == UserRole.Doctor && e.IsActive == AccountStatus.Active)
                .OrderBy(e => e.FullName)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeID.ToString(),
                    Text = e.FullName
                })
                .ToListAsync();

            ViewBag.Doctors = doctors;
            return View(new AppointmentRequestViewModel
            {
                PatientId = GetCurrentPatientId(),
                RequestedDateTime = DateTime.Now.AddDays(1).Date.AddHours(9) // default next day 9am
            });
        }

        // ── REQUEST APPOINTMENT (POST) ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestAppointment(AppointmentRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Doctors = await GetDoctorSelectList();
                return View(model);
            }

            // Verify the doctor is active
            var doctor = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeID == model.DoctorId &&
                                         e.Role == UserRole.Doctor &&
                                         e.IsActive == AccountStatus.Active);
            if (doctor == null)
            {
                ModelState.AddModelError("DoctorId", "Invalid doctor selected.");
                ViewBag.Doctors = await GetDoctorSelectList();
                return View(model);
            }

            var request = new AppointmentRequest
            {
                PatientId = GetCurrentPatientId(),
                DoctorId = model.DoctorId,
                RequestedDateTime = model.RequestedDateTime,
                Reason = model.Reason,
                Status = "Pending",
                RequestedAt = DateTime.Now
            };

            _context.AppointmentRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your appointment request has been submitted. The receptionist will confirm it soon.";
            return RedirectToAction("Dashboard");
        }



        // ── MY VISITS (LIST) ──────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MyVisits()
        {
            int patientId = GetCurrentPatientId();
            if (patientId == 0) return RedirectToAction("Login", "PatientAuth");

            var visits = await _context.Visits
                .Where(v => v.PatientId == patientId)
                .Include(v => v.Doctor)                // So we can show the doctor’s name
                .OrderByDescending(v => v.VisitDateTime)
                .AsNoTracking()
                .ToListAsync();

            return View(visits);
        }

        // ── VISIT SUMMARY (PRINT‑FRIENDLY) ────────────────────────
        [HttpGet]
        public async Task<IActionResult> VisitSummary(int? id)
        {
            if (id == null) return NotFound();

            int patientId = GetCurrentPatientId();
            var visit = await _context.Visits
                .Include(v => v.Doctor)
                .Include(v => v.Patient)
                .FirstOrDefaultAsync(v => v.VisitId == id);

            // Ensure the visit belongs to the logged‑in patient
            if (visit == null || visit.PatientId != patientId)
                return Forbid();

            return View(visit);
        }






        // ── HELPER: Get doctors dropdown ──────────────────────────
        private async Task<IEnumerable<SelectListItem>> GetDoctorSelectList()
        {
            return await _context.Employees
                .Where(e => e.Role == UserRole.Doctor && e.IsActive == AccountStatus.Active)
                .OrderBy(e => e.FullName)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeID.ToString(),
                    Text = e.FullName
                })
                .ToListAsync();
        }



        // ── HELPER ─────────────────────────────────────────────
        private int GetCurrentPatientId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out int id) ? id : 0;
        }
    }
}