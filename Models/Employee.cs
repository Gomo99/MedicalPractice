using MedicalPractice.Status;
using System;
using System.ComponentModel.DataAnnotations;

namespace MedicalPractice.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeID { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        public string FullName { get; set; }

        public string? FirstName { get; set; }   // used in forgot password email

        [Required]
        public string PasswordHash { get; set; }

        public UserRole Role { get; set; }

        public AccountStatus IsActive { get; set; } = AccountStatus.Active;

        public bool IsLockedOut { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int FailedLoginAttempts { get; set; }
        public bool MustChangePassword { get; set; }

        // Two‑factor authentication fields
        public bool IsTwoFactorEnabled { get; set; }
        public string? TwoFactorSecretKey { get; set; }
        public string? TwoFactorRecoveryCodes { get; set; } // JSON array of hashed codes

        // Password reset fields
        public string? ResetPin { get; set; }
        public DateTime? ResetPinExpiration { get; set; }
    }
}