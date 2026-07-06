using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalPractice.Models
{
    public class Patient
    {
        [Key]
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

        // ────────────── NEW FIELDS ──────────────
        /// <summary>BCrypt hash of the password. Null if the patient was added by an assistant and hasn't set a password yet.</summary>
        public string? PasswordHash { get; set; }

        /// <summary>Account activation status.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>If true, the patient must change password on next login.</summary>
        public bool MustChangePassword { get; set; }

        // Password reset fields (same pattern as Employee)
        public string? ResetPin { get; set; }
        public DateTime? ResetPinExpiration { get; set; }

        // Lockout fields
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }

        // Two‑factor (optional, but may be added later)
        public bool IsTwoFactorEnabled { get; set; }
        public string? TwoFactorSecretKey { get; set; }
        public string? TwoFactorRecoveryCodes { get; set; }
    }
}