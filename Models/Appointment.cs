using MedicalPractice.Status;
using System;
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
        public int DoctorId { get; set; }          // EmployeeID of a Doctor

        [ForeignKey("DoctorId")]
        public Employee Doctor { get; set; }

        [Required]
        public DateTime AppointmentDateTime { get; set; }

        [Required]
        public string Reason { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;

        // Optional: track which receptionist created the appointment
        public int? CreatedByReceptionistId { get; set; }

        [ForeignKey("CreatedByReceptionistId")]
        public Employee CreatedByReceptionist { get; set; }
    }
}