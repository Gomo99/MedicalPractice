using MedicalPractice.Status;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalPractice.Models
{
    public class AppointmentRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient Patient { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public Employee Doctor { get; set; }

        [Required]
        public DateTime RequestedDateTime { get; set; }

        [Required]
        public string Reason { get; set; }

        /// <summary>Status: Pending, Approved, Rejected</summary>
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime RequestedAt { get; set; } = DateTime.Now;
    }
}