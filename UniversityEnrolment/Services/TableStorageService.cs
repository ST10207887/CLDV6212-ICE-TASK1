using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using UniversityEnrolment.Models;

namespace UniversityEnrolment.Services
{
    /// <summary>
    /// Connects to the Azure Table storage account and performs CRUD
    /// operations on the Courses and Students tables.
    /// Based on the Azure.Data.Tables quick-start (Microsoft, 2024).
    /// </summary>
    public class TableStorageService
    {
        private readonly TableClient _courses;
        private readonly TableClient _students;

        public TableStorageService(IOptions<StorageOptions> options)
        {
            var o = options.Value;
            var serviceClient = new TableServiceClient(o.TableConnectionString);

            _courses = serviceClient.GetTableClient(o.CoursesTable);
            _students = serviceClient.GetTableClient(o.StudentsTable);

            _courses.CreateIfNotExists();
            _students.CreateIfNotExists();
        }

        // ---------------- Courses ----------------
        public async Task<List<Course>> GetCoursesAsync()
        {
            var list = new List<Course>();
            await foreach (var c in _courses.QueryAsync<Course>(c => c.PartitionKey == Course.Partition))
                list.Add(c);
            return list.OrderBy(c => c.CourseCode).ToList();
        }

        public async Task<Course?> GetCourseAsync(string code)
        {
            var result = await _courses.GetEntityIfExistsAsync<Course>(Course.Partition, code.ToUpperInvariant());
            return result.HasValue ? result.Value : null;
        }

        public async Task<bool> AddCourseAsync(Course course)
        {
            course.CourseCode = course.CourseCode.Trim().ToUpperInvariant();
            course.PartitionKey = Course.Partition;
            course.RowKey = course.CourseCode;
            try
            {
                await _courses.AddEntityAsync(course);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return false; // already exists
            }
        }

        public Task UpdateCourseAsync(Course course) =>
            _courses.UpdateEntityAsync(course, course.ETag == default ? ETag.All : course.ETag, TableUpdateMode.Replace);

        public async Task DeleteCourseAsync(string code)
        {
            var course = await GetCourseAsync(code);
            if (course == null) return;

            // remove the course from every enrolled student's list
            foreach (var studentId in course.GetRoster())
            {
                var student = await GetStudentAsync(studentId);
                if (student == null) continue;
                student.SetCourses(student.GetCourses().Where(c => !c.Equals(course.CourseCode, StringComparison.OrdinalIgnoreCase)));
                await UpdateStudentAsync(student);
            }

            await _courses.DeleteEntityAsync(Course.Partition, course.RowKey);
        }

        // ---------------- Students ----------------
        public async Task<List<Student>> GetStudentsAsync()
        {
            var list = new List<Student>();
            await foreach (var s in _students.QueryAsync<Student>(s => s.PartitionKey == Student.Partition))
                list.Add(s);
            return list.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();
        }

        public async Task<Student?> GetStudentAsync(string studentNumber)
        {
            var result = await _students.GetEntityIfExistsAsync<Student>(Student.Partition, studentNumber.ToUpperInvariant());
            return result.HasValue ? result.Value : null;
        }

        public async Task<bool> AddStudentAsync(Student student)
        {
            student.StudentNumber = student.StudentNumber.Trim().ToUpperInvariant();
            student.PartitionKey = Student.Partition;
            student.RowKey = student.StudentNumber;
            try
            {
                await _students.AddEntityAsync(student);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return false;
            }
        }

        public Task UpdateStudentAsync(Student student) =>
            _students.UpdateEntityAsync(student, student.ETag == default ? ETag.All : student.ETag, TableUpdateMode.Replace);

        public async Task DeleteStudentAsync(string studentNumber)
        {
            var student = await GetStudentAsync(studentNumber);
            if (student == null) return;

            // remove the student from every course roster
            foreach (var code in student.GetCourses())
            {
                var course = await GetCourseAsync(code);
                if (course == null) continue;
                course.SetRoster(course.GetRoster().Where(id => !id.Equals(student.StudentNumber, StringComparison.OrdinalIgnoreCase)));
                await UpdateCourseAsync(course);
            }

            await _students.DeleteEntityAsync(Student.Partition, student.RowKey);
        }
    }
}
