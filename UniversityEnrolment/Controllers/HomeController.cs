using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UniversityEnrolment.Models;
using UniversityEnrolment.Services;

namespace UniversityEnrolment.Controllers
{
    public class HomeController : Controller
    {
        private readonly TableStorageService _tables;
        private readonly EnrollmentQueueService _queue;
        private readonly EnrollmentProcessor _processor;

        public HomeController(TableStorageService tables, EnrollmentQueueService queue, EnrollmentProcessor processor)
        {
            _tables = tables;
            _queue = queue;
            _processor = processor;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.CourseCount = (await _tables.GetCoursesAsync()).Count;
            ViewBag.StudentCount = (await _tables.GetStudentsAsync()).Count;
            ViewBag.QueueCount = await _queue.GetApproximateCountAsync();
            return View();
        }

        // POST: process the queue immediately (the background worker also does this every few seconds)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessQueue(string? returnUrl)
        {
            var count = await _processor.ProcessPendingAsync();
            TempData["Success"] = $"{count} queue message(s) processed.";
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
