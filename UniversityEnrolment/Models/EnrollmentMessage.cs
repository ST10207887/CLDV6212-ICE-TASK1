namespace UniversityEnrolment.Models
{
    public enum EnrollmentAction
    {
        Enroll,
        Drop
    }

    /// <summary>Message placed on the CourseEnrollmentQueue.</summary>
    public class EnrollmentMessage
    {
        public string StudentNumber { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public EnrollmentAction Action { get; set; } = EnrollmentAction.Enroll;
        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
