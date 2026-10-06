namespace UniversityEnrolment.Models
{
    /// <summary>Bound from the "AzureStorage" section of appsettings.json.</summary>
    public class StorageOptions
    {
        // Connection string of the "universitycourses" storage account (tables)
        public string TableConnectionString { get; set; } = "UseDevelopmentStorage=true";

        // Connection string of the "enrollmentqueue" storage account (queues)
        public string QueueConnectionString { get; set; } = "UseDevelopmentStorage=true";

        public string CoursesTable { get; set; } = "Courses";
        public string StudentsTable { get; set; } = "Students";

        // Azure queue names must be lower case, so "CourseEnrollmentQueue" becomes "courseenrollmentqueue"
        public string EnrollmentQueue { get; set; } = "courseenrollmentqueue";

        public int QueuePollSeconds { get; set; } = 5;
    }
}
