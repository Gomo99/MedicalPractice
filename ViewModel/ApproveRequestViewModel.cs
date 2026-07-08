namespace MedicalPractice.ViewModel
{
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
}