using MedicalPractice.Status;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalPractice.Models
{
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