using System;
using System.ComponentModel.DataAnnotations;

namespace MedicalPractice.ViewModel
{
    public class AppointmentRequestViewModel
    {
        public int? RequestId { get; set; }   // null when creating, set when editing

        [Required]
        public int PatientId { get; set; }    // hidden / auto-assigned

        [Required(ErrorMessage = "Please select a doctor.")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Preferred date/time is required.")]
        public DateTime RequestedDateTime { get; set; } = DateTime.Now.AddDays(1);

        [Required(ErrorMessage = "Please provide a reason for the visit.")]
        public string Reason { get; set; }
    }
}