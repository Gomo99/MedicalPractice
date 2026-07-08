using System.ComponentModel.DataAnnotations;

namespace MedicalPractice.ViewModel
{
    public class PatientViewModel
    {
        // Used for Create/Edit
        public int? PatientId { get; set; }   // null on create

        [Required]
        public string Name { get; set; }

        [Required]
        public string Surname { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public string HomeAddress { get; set; }

        [Required, EmailAddress]
        public string EmailAddress { get; set; }

        [Required, Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public bool MedicalAid { get; set; }

        public string? MedicalAidCompany { get; set; }
    }
}