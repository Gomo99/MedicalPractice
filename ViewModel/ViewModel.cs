using MedicalPractice.Status;
using Microsoft.AspNetCore.Mvc.Rendering;
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




    public class ApproveRequestViewModel
    {
        public int RequestId { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public DateTime RequestedDateTime { get; set; }
        public string Reason { get; set; }

        // The final appointment date/time – pre-filled with requested, editable
        public DateTime AppointmentDateTime { get; set; }
    }



    public class ChangePasswordViewModel
    {

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "CurrentPassword")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }


    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class PatientProfileViewModel
    {
        public int PatientId { get; set; }

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


    public class PatientRegisterViewModel
    {
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

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }


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


    public class ProfileViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class TwoFactorChallengeViewModel
    {
        [Display(Name = "Authentication code")]
        public string? Code { get; set; }

        [Display(Name = "Recovery code")]
        public string? RecoveryCode { get; set; }

        public bool UseRecoveryCode { get; set; } = false;

        // Passed through the challenge — needed to complete sign-in
        public string ReturnUrl { get; set; } = string.Empty;
    }


    public class TwoFactorRecoveryCodesViewModel
    {
        public List<string> PlainCodes { get; set; } = new();
    }


    public class TwoFactorSetupViewModel
    {
        public string SecretKey { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;  // PNG as base64

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits.")]
        [Display(Name = "Verification code")]
        public string VerificationCode { get; set; } = string.Empty;
    }



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

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Reset PIN")]
        public string Pin { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }





    public class PatientLoginViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class EditEmployeeViewModel
    {
        public int EmployeeID { get; set; }

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


    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;
    }



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
