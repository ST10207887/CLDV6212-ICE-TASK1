namespace UniversityEnrolment.Models.ViewModels
{
    public class StudentCoursesViewModel
    {
        public Student Student { get; set; } = new();
        public List<Course> EnrolledCourses { get; set; } = new();
        public List<Course> AvailableCourses { get; set; } = new();
        public int PendingQueueMessages { get; set; }
    }

    public class CourseRosterViewModel
    {
        public Course Course { get; set; } = new();
        public List<Student> Students { get; set; } = new();
    }
}
