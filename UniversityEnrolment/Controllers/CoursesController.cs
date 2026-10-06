using Microsoft.AspNetCore.Mvc;
using UniversityEnrolment.Models;
using UniversityEnrolment.Models.ViewModels;
using UniversityEnrolment.Services;

namespace UniversityEnrolment.Controllers
{
    /// <summary>CRUD for the Courses table plus the instructor roster view.</summary>
    public class CoursesController : Controller
    {
        private readonly TableStorageService _tables;
        private readonly EnrollmentQueueService _queue;

        public CoursesController(TableStorageService tables, EnrollmentQueueService queue)
        {
            _tables = tables;
            _queue = queue;
        }

        // GET: /Courses  (students view available courses here)
        public async Task<IActionResult> Index() => View(await _tables.GetCoursesAsync());

        // GET: /Courses/Details/CLDV6212
        public async Task<IActionResult> Details(string id)
        {
            var course = await _tables.GetCourseAsync(id);
            return course == null ? NotFound() : View(course);
        }

        // GET: /Courses/Create
        public IActionResult Create() => View(new Course());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course)
        {
            if (!ModelState.IsValid) return View(course);

            if (!await _tables.AddCourseAsync(course))
            {
                ModelState.AddModelError(nameof(Course.CourseCode), "A course with this code already exists.");
                return View(course);
            }

            TempData["Success"] = $"Course {course.CourseCode} created.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Courses/Edit/CLDV6212
        public async Task<IActionResult> Edit(string id)
        {
            var course = await _tables.GetCourseAsync(id);
            return course == null ? NotFound() : View(course);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Course input)
        {
            var course = await _tables.GetCourseAsync(id);
            if (course == null) return NotFound();

            input.CourseCode = course.CourseCode; // the key cannot change
            ModelState.Remove(nameof(Course.CourseCode));
            if (!ModelState.IsValid) return View(input);

            if (input.Capacity < course.GetRoster().Count)
            {
                ModelState.AddModelError(nameof(Course.Capacity), $"{course.GetRoster().Count} students are already enrolled.");
                return View(input);
            }

            course.Name = input.Name;
            course.Description = input.Description;
            course.Instructor = input.Instructor;
            course.Credits = input.Credits;
            course.Capacity = input.Capacity;
            await _tables.UpdateCourseAsync(course);

            TempData["Success"] = $"Course {course.CourseCode} updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Courses/Delete/CLDV6212
        public async Task<IActionResult> Delete(string id)
        {
            var course = await _tables.GetCourseAsync(id);
            return course == null ? NotFound() : View(course);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _tables.DeleteCourseAsync(id);
            TempData["Success"] = $"Course {id} deleted.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Courses/Roster/CLDV6212  (instructor view of enrolled students)
        public async Task<IActionResult> Roster(string id)
        {
            var course = await _tables.GetCourseAsync(id);
            if (course == null) return NotFound();

            var students = new List<Student>();
            foreach (var studentId in course.GetRoster())
            {
                var s = await _tables.GetStudentAsync(studentId);
                if (s != null) students.Add(s);
            }

            ViewBag.AllStudents = (await _tables.GetStudentsAsync())
                .Where(s => !course.GetRoster().Contains(s.StudentNumber, StringComparer.OrdinalIgnoreCase))
                .ToList();

            return View(new CourseRosterViewModel { Course = course, Students = students.OrderBy(s => s.LastName).ToList() });
        }

        // POST: instructor adds a student to the roster (goes through the queue)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToRoster(string id, string studentNumber)
        {
            if (!string.IsNullOrWhiteSpace(studentNumber))
            {
                await _queue.SendAsync(new EnrollmentMessage { CourseCode = id, StudentNumber = studentNumber, Action = EnrollmentAction.Enroll });
                TempData["Success"] = $"Enrolment request for {studentNumber} sent to the queue.";
            }
            return RedirectToAction(nameof(Roster), new { id });
        }

        // POST: instructor removes a student from the roster (goes through the queue)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromRoster(string id, string studentNumber)
        {
            await _queue.SendAsync(new EnrollmentMessage { CourseCode = id, StudentNumber = studentNumber, Action = EnrollmentAction.Drop });
            TempData["Success"] = $"Removal request for {studentNumber} sent to the queue.";
            return RedirectToAction(nameof(Roster), new { id });
        }
    }
}
