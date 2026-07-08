using MedicalPractice.Status;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalPractice.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient Patient { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public Employee Doctor { get; set; }

        [Required]
        public DateTime AppointmentDateTime { get; set; }

        [Required]
        public string Reason { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;

        public int? CreatedByReceptionistId { get; set; }

        [ForeignKey("CreatedByReceptionistId")]
        public Employee CreatedByReceptionist { get; set; }

        // ── NEW: Reschedule request details ──
        public DateTime? RescheduleRequestedAt { get; set; }
        public string? RescheduleRequestReason { get; set; }
    }
}