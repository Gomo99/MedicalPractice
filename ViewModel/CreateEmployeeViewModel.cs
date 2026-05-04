using MedicalPractice.Status;
using System.ComponentModel.DataAnnotations;

namespace MedicalPractice.ViewModel
{
    public class CreateEmployeeViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        

        [Required]
        [Display(Name = "Role")]
        public UserRole Role { get; set; }
    }
}