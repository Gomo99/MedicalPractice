using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MedicalPractice.ViewModel
{
    public class AppointmentViewModel
    {
        public int? AppointmentId { get; set; }   // null when creating

        [Required]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [Required]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required]
        [Display(Name = "Date & Time")]
        public DateTime AppointmentDateTime { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Reason")]
        public string Reason { get; set; }

        // Dropdown lists populated by controller
        public List<SelectListItem>? Patients { get; set; }
        public List<SelectListItem>? Doctors { get; set; }
    }
}