using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace UniversityEnrolment.Models
{
    /// <summary>
    /// A course stored in the "Courses" Azure table.
    /// PartitionKey = "Course", RowKey = course code (e.g. CLDV6212).
    /// </summary>
    public class Course : ITableEntity
    {
        public const string Partition = "Course";

        [Required, StringLength(10, MinimumLength = 3)]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Course code may only contain letters and numbers.")]
        [Display(Name = "Course Code")]
        public string CourseCode { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Course Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required, StringLength(100)]
        public string Instructor { get; set; } = string.Empty;

        [Range(1, 60)]
        public int Credits { get; set; } = 15;

        [Range(1, 500)]
        [Display(Name = "Max Students")]
        public int Capacity { get; set; } = 30;

        /// <summary>Comma-separated student numbers on the course roster.</summary>
        public string EnrolledStudentIds { get; set; } = string.Empty;

        // ITableEntity
        public string PartitionKey { get; set; } = Partition;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public List<string> GetRoster() => CsvList.Split(EnrolledStudentIds);
        public void SetRoster(IEnumerable<string> ids) => EnrolledStudentIds = CsvList.Join(ids);
    }
}
