using System.ComponentModel.DataAnnotations;

namespace MedicalPractice.ViewModel
{
    public class VisitViewModel
    {
        public int VisitId { get; set; }          // 0 for new visits
        public int PatientId { get; set; }

        [Required]
        [Display(Name = "Date/Time")]
        public DateTime VisitDateTime { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Diagnosis")]
        public string Diagnosis { get; set; }

        [Required]
        [Display(Name = "Treatment")]
        public string Treatment { get; set; }
    }
}