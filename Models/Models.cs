using MedicalPractice.Status;
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
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public Employee Doctor { get; set; }

        [Required]
        public DateTime AppointmentDateTime { get; set; }

        [Required]
        public string Reason { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;

        public int? CreatedByReceptionistId { get; set; }

        [ForeignKey("CreatedByReceptionistId")]
        public Employee CreatedByReceptionist { get; set; }

        // ── NEW: Reschedule request details ──
        public DateTime? RescheduleRequestedAt { get; set; }
        public string? RescheduleRequestReason { get; set; }
    }


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




    public class Notification
    {
        public int NotificationId { get; set; }
        public int EmployeeId { get; set; }
        public string Message { get; set; }
        public string? Link { get; set; }
        public string Icon { get; set; } = "bi-bell";
        public string Type { get; set; } = "info";
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public NotificationCategory Category { get; set; } = NotificationCategory.General;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        // ─── NEW ───────────────────────────────────────
        public DateTime? ExpiresAt { get; set; }

        public Employee Employee { get; set; }
    }



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




    public class Visit
    {
        [Key]
        public int VisitId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient Patient { get; set; }

        [Required]
        public int DoctorId { get; set; }   // Employee.EmployeeID

        [ForeignKey("DoctorId")]
        public Employee Doctor { get; set; }

        [Required]
        public DateTime VisitDateTime { get; set; }

        [Required]
        public string Diagnosis { get; set; }

        [Required]
        public string Treatment { get; set; }
    }



}
