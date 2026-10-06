using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Options;
using UniversityEnrolment.Models;

namespace UniversityEnrolment.Services
{
    /// <summary>
    /// Reads enrolment requests from the CourseEnrollmentQueue and updates the
    /// student's enrolled-courses list and the course roster in Table storage.
    /// Runs in the background every few seconds and can also be triggered from the UI.
    /// </summary>
    public class EnrollmentProcessor : BackgroundService
    {
        private readonly EnrollmentQueueService _queue;
        private readonly TableStorageService _tables;
        private readonly ILogger<EnrollmentProcessor> _logger;
        private readonly TimeSpan _interval;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public EnrollmentProcessor(EnrollmentQueueService queue, TableStorageService tables,
            IOptions<StorageOptions> options, ILogger<EnrollmentProcessor> logger)
        {
            _queue = queue;
            _tables = tables;
            _logger = logger;
            _interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.QueuePollSeconds));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error while processing the enrolment queue");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        /// <summary>Processes every message currently on the queue. Returns the number handled.</summary>
        public async Task<int> ProcessPendingAsync(CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct);
            try
            {
                var handled = 0;
                while (true)
                {
                    QueueMessage[] messages = await _queue.Client.ReceiveMessagesAsync(maxMessages: 16, cancellationToken: ct);
                    if (messages.Length == 0) break;

                    foreach (var msg in messages)
                    {
                        try
                        {
                            var request = JsonSerializer.Deserialize<EnrollmentMessage>(msg.MessageText);
                            if (request != null)
                                await HandleAsync(request);
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Discarding malformed queue message {Id}", msg.MessageId);
                        }

                        // remove the message once it has been handled
                        await _queue.Client.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, ct);
                        handled++;
                    }
                }
                return handled;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task HandleAsync(EnrollmentMessage request)
        {
            var student = await _tables.GetStudentAsync(request.StudentNumber);
            var course = await _tables.GetCourseAsync(request.CourseCode);

            if (student == null || course == null)
            {
                _logger.LogWarning("Enrolment request ignored - student {S} or course {C} not found",
                    request.StudentNumber, request.CourseCode);
                return;
            }

            var studentCourses = student.GetCourses();
            var roster = course.GetRoster();

            if (request.Action == EnrollmentAction.Enroll)
            {
                if (!roster.Contains(student.StudentNumber, StringComparer.OrdinalIgnoreCase) && roster.Count >= course.Capacity)
                {
                    _logger.LogWarning("Course {C} is full - {S} not enrolled", course.CourseCode, student.StudentNumber);
                    return;
                }

                studentCourses.Add(course.CourseCode);
                roster.Add(student.StudentNumber);
                _logger.LogInformation("Enrolled {S} in {C}", student.StudentNumber, course.CourseCode);
            }
            else
            {
                studentCourses.RemoveAll(c => c.Equals(course.CourseCode, StringComparison.OrdinalIgnoreCase));
                roster.RemoveAll(s => s.Equals(student.StudentNumber, StringComparison.OrdinalIgnoreCase));
                _logger.LogInformation("Removed {S} from {C}", student.StudentNumber, course.CourseCode);
            }

            student.SetCourses(studentCourses);
            course.SetRoster(roster);

            await _tables.UpdateStudentAsync(student);
            await _tables.UpdateCourseAsync(course);
        }
    }
}
