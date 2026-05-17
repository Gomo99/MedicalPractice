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
using System.Threading.Tasks;

namespace MedicalPractice.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class ReceptionistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReceptionistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── DASHBOARD ────────────────────────────────────────────
        public IActionResult DashBoard()
        {
            return View();
        }

        // ── VIEW DAILY APPOINTMENTS ─────────────────────────────
        public async Task<IActionResult> DailySchedule(DateTime? date)
        {
            DateTime selectedDate = date ?? DateTime.Today;

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDateTime.Date == selectedDate.Date)
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();

            ViewBag.SelectedDate = selectedDate;
            return View(appointments);
        }

        // ── DOCTOR AVAILABILITY CALENDAR ────────────────────────
        // Shows appointments for a specific doctor on a selected date
        public async Task<IActionResult> DoctorSchedule(int? doctorId, DateTime? date)
        {
            DateTime selectedDate = date ?? DateTime.Today;

            // Populate doctor dropdown list
            var doctors = await _context.Employees
                .Where(e => e.Role == UserRole.Doctor && e.IsActive == AccountStatus.Active)
                .OrderBy(e => e.FullName)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeID.ToString(),
                    Text = e.FullName
                }).ToListAsync();
            ViewBag.Doctors = doctors;
            ViewBag.SelectedDate = selectedDate;

            if (doctorId.HasValue)
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctorId.Value
                           && a.AppointmentDateTime.Date == selectedDate.Date
                           && a.Status != AppointmentStatus.Cancelled)
                    .OrderBy(a => a.AppointmentDateTime)
                    .ToListAsync();

                ViewBag.SelectedDoctorId = doctorId.Value;
                return View(appointments);
            }

            // No doctor selected: show empty list
            return View(new List<Appointment>());
        }

        // ── BOOK APPOINTMENT (GET) ──────────────────────────────
        [HttpGet]
        public IActionResult BookAppointment()
        {
            var model = new AppointmentViewModel
            {
                AppointmentDateTime = DateTime.Now.AddHours(1)
            };
            PopulateDropdowns(model);
            return View(model);
        }

        // ── BOOK APPOINTMENT (POST) ─────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(AppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model);
                return View(model);
            }

            var doctor = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeID == model.DoctorId && e.Role == UserRole.Doctor);
            if (doctor == null)
            {
                ModelState.AddModelError("DoctorId", "Invalid doctor selected.");
                PopulateDropdowns(model);
                return View(model);
            }

            var appointment = new Appointment
            {
                PatientId = model.PatientId,
                DoctorId = model.DoctorId,
                AppointmentDateTime = model.AppointmentDateTime,
                Reason = model.Reason,
                Status = AppointmentStatus.Booked
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment booked successfully.";
            return RedirectToAction(nameof(DailySchedule));
        }

        // ── RESCHEDULE APPOINTMENT (GET) ────────────────────────
        [HttpGet]
        public async Task<IActionResult> Reschedule(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (appointment == null) return NotFound();

            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                TempData["Error"] = "Cannot reschedule a cancelled appointment.";
                return RedirectToAction(nameof(DailySchedule));
            }

            var model = new AppointmentViewModel
            {
                AppointmentId = appointment.AppointmentId,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                AppointmentDateTime = appointment.AppointmentDateTime,
                Reason = appointment.Reason
            };

            // Populate dropdowns but patient/doctor are read-only (display names only)
            ViewBag.PatientName = $"{appointment.Patient.Surname}, {appointment.Patient.Name}";
            ViewBag.DoctorName = appointment.Doctor.FullName;
            return View(model);
        }

        // ── RESCHEDULE APPOINTMENT (POST) ───────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, AppointmentViewModel model)
        {
            if (id != model.AppointmentId) return NotFound();

            if (!ModelState.IsValid)
            {
                // Repopulate display names in case of error
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == id);
                if (appointment != null)
                {
                    ViewBag.PatientName = $"{appointment.Patient.Surname}, {appointment.Patient.Name}";
                    ViewBag.DoctorName = appointment.Doctor.FullName;
                }
                return View(model);
            }

            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            if (appt.Status == AppointmentStatus.Cancelled)
            {
                TempData["Error"] = "Cannot reschedule a cancelled appointment.";
                return RedirectToAction(nameof(DailySchedule));
            }

            appt.AppointmentDateTime = model.AppointmentDateTime;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment rescheduled.";
            return RedirectToAction(nameof(DailySchedule));
        }

        // ── CANCEL APPOINTMENT (POST) ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                TempData["Error"] = "Appointment is already cancelled.";
                return RedirectToAction(nameof(DailySchedule));
            }

            appointment.Status = AppointmentStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment cancelled.";
            return RedirectToAction(nameof(DailySchedule));
        }

        // ── CHECK-IN (POST) ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            if (appointment.Status != AppointmentStatus.Booked)
            {
                TempData["Error"] = "Only booked appointments can be checked in.";
                return RedirectToAction(nameof(DailySchedule));
            }

            appointment.Status = AppointmentStatus.Arrived;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Patient checked in.";
            return RedirectToAction(nameof(DailySchedule));
        }

        // ── UPDATE STATUS (POST) ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, AppointmentStatus newStatus)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            if (appointment.Status == AppointmentStatus.Cancelled ||
                appointment.Status == AppointmentStatus.Completed)
            {
                TempData["Error"] = "Cannot change status of completed/cancelled appointments.";
                return RedirectToAction(nameof(DailySchedule));
            }

            appointment.Status = newStatus;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Appointment status changed to {newStatus}.";
            return RedirectToAction(nameof(DailySchedule));
        }

        // ── REGISTER PATIENT (GET) ──────────────────────────────
        [HttpGet]
        public IActionResult RegisterPatient()
        {
            return View(new PatientViewModel());
        }

        // ── REGISTER PATIENT (POST) ─────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPatient(PatientViewModel model)
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
                MedicalAidCompany = model.MedicalAid ? model.MedicalAidCompany : null
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Patient '{patient.Name} {patient.Surname}' registered.";
            return RedirectToAction(nameof(DailySchedule));
        }

        // ── PRIVATE HELPERS ─────────────────────────────────────
        private void PopulateDropdowns(AppointmentViewModel model)
        {
            var patients = _context.Patients
                .OrderBy(p => p.Surname)
                .Select(p => new SelectListItem
                {
                    Value = p.PatientId.ToString(),
                    Text = $"{p.Surname}, {p.Name} ({p.Username})"
                }).ToList();
            model.Patients = patients;

            var doctors = _context.Employees
                .Where(e => e.Role == UserRole.Doctor && e.IsActive == AccountStatus.Active)
                .OrderBy(e => e.FullName)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeID.ToString(),
                    Text = e.FullName
                }).ToList();
            model.Doctors = doctors;
        }
    }
}