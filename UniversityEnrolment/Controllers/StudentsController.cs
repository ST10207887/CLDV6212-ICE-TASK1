using Microsoft.AspNetCore.Mvc;
using UniversityEnrolment.Models;
using UniversityEnrolment.Models.ViewModels;
using UniversityEnrolment.Services;

namespace UniversityEnrolment.Controllers
{
    /// <summary>CRUD for the Students table plus enrolment (sent via the queue).</summary>
    public class StudentsController : Controller
    {
        private readonly TableStorageService _tables;
        private readonly EnrollmentQueueService _queue;

        public StudentsController(TableStorageService tables, EnrollmentQueueService queue)
        {
            _tables = tables;
            _queue = queue;
        }

        public async Task<IActionResult> Index() => View(await _tables.GetStudentsAsync());

        // GET: /Students/MyCourses/ST10207887  - track enrolled courses and enrol in new ones
        public async Task<IActionResult> MyCourses(string id)
        {
            var student = await _tables.GetStudentAsync(id);
            if (student == null) return NotFound();

            var allCourses = await _tables.GetCoursesAsync();
            var enrolled = student.GetCourses();

            var vm = new StudentCoursesViewModel
            {
                Student = student,
                EnrolledCourses = allCourses.Where(c => enrolled.Contains(c.CourseCode, StringComparer.OrdinalIgnoreCase)).ToList(),
                AvailableCourses = allCourses.Where(c => !enrolled.Contains(c.CourseCode, StringComparer.OrdinalIgnoreCase)).ToList(),
                PendingQueueMessages = await _queue.GetApproximateCountAsync()
            };
            return View(vm);
        }

        // POST: /Students/Enroll  - sends a message to the CourseEnrollmentQueue
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(string id, string courseCode)
        {
            var student = await _tables.GetStudentAsync(id);
            var course = await _tables.GetCourseAsync(courseCode);
            if (student == null || course == null) return NotFound();

            if (course.GetRoster().Count >= course.Capacity)
            {
                TempData["Error"] = $"{course.CourseCode} is full.";
                return RedirectToAction(nameof(MyCourses), new { id });
            }

            await _queue.SendAsync(new EnrollmentMessage
            {
                StudentNumber = student.StudentNumber,
                CourseCode = course.CourseCode,
                Action = EnrollmentAction.Enroll
            });

            TempData["Success"] = $"Enrolment request for {course.CourseCode} submitted. It will appear once the queue has been processed.";
            return RedirectToAction(nameof(MyCourses), new { id });
        }

        // POST: /Students/Drop
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Drop(string id, string courseCode)
        {
            await _queue.SendAsync(new EnrollmentMessage
            {
                StudentNumber = id,
                CourseCode = courseCode,
                Action = EnrollmentAction.Drop
            });

            TempData["Success"] = $"Request to drop {courseCode} submitted.";
            return RedirectToAction(nameof(MyCourses), new { id });
        }

        public IActionResult Create() => View(new Student());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            if (!ModelState.IsValid) return View(student);

            if (!await _tables.AddStudentAsync(student))
            {
                ModelState.AddModelError(nameof(Student.StudentNumber), "A student with this number already exists.");
                return View(student);
            }

            TempData["Success"] = $"Student {student.StudentNumber} created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var student = await _tables.GetStudentAsync(id);
            return student == null ? NotFound() : View(student);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Student input)
        {
            var student = await _tables.GetStudentAsync(id);
            if (student == null) return NotFound();

            input.StudentNumber = student.StudentNumber;
            ModelState.Remove(nameof(Student.StudentNumber));
            if (!ModelState.IsValid) return View(input);

            student.FirstName = input.FirstName;
            student.LastName = input.LastName;
            student.Email = input.Email;
            await _tables.UpdateStudentAsync(student);

            TempData["Success"] = $"Student {student.StudentNumber} updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            var student = await _tables.GetStudentAsync(id);
            return student == null ? NotFound() : View(student);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _tables.DeleteStudentAsync(id);
            TempData["Success"] = $"Student {id} deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
