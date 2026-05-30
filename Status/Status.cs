namespace MedicalPractice.Status
{
    public enum AccountStatus
    {
        Inactive,
        Active
    }


    public enum UserRole
    {
        Admin,
        Doctor,
        Assistant,
        Receptionist

    }

    public enum AppointmentStatus
    {
        Booked,
        Arrived,
        InProgress,
        Completed,
        Cancelled,
        RescheduleRequested     // NEW
    }


    public enum NotificationCategory
    {
        General,       // default, fallback
        Appointment,
        Prescription,
        Billing,
        Patient,
        System,
        Emergency
    }


    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

}
