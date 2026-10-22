namespace StayBnb.Models
{
    public enum ApplicationStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public enum BookingStatus
    {
        Pending,
        Approved,
        Rejected,
        CheckedIn,
        Cancelled,
        Completed
    }

    public enum PaymentStatus
    {
        Pending,
        Paid,
        Failed,
        Refunded
    }

    public enum CheckInStatus
    {
        Pending,
        Submitted,
        Verified,
        Rejected
    }

    public enum DocumentStatus
    {
        Pending,
        Verified,
        Rejected
    }

    public enum NotificationType
    {
        System,
        HostApplicationUpdate,
        BookingUpdate,
        NewMessage,
        CheckInUpdate
    }

    public enum ActivityType
    {
        Login,
        Logout,
        RoleChanged,
        HostApplicationSubmitted,
        HostApplicationReviewed,
        PropertyUpdated,
        BookingCreated,
        BookingStatusChanged,
        CheckInUpdated,
        DocumentReviewed,
        MessageSent
    }
}