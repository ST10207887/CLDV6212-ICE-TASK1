using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace UniversityEnrolment.Models
{
    /// <summary>
    /// A student stored in the "Students" Azure table.
    /// PartitionKey = "Student", RowKey = student number.
    /// </summary>
    public class Student : ITableEntity
    {
        public const string Partition = "Student";

        [Required, StringLength(12, MinimumLength = 3)]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Student number may only contain letters and numbers.")]
        [Display(Name = "Student Number")]
        public string StudentNumber { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>Comma-separated course codes the student is enrolled in.</summary>
        public string EnrolledCourses { get; set; } = string.Empty;

        // ITableEntity
        public string PartitionKey { get; set; } = Partition;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string FullName => $"{FirstName} {LastName}";
        public List<string> GetCourses() => CsvList.Split(EnrolledCourses);
        public void SetCourses(IEnumerable<string> codes) => EnrolledCourses = CsvList.Join(codes);
    }
}
