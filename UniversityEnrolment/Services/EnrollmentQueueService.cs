using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Options;
using UniversityEnrolment.Models;

namespace UniversityEnrolment.Services
{
    /// <summary>
    /// Sends enrolment requests to the CourseEnrollmentQueue.
    /// Based on the Azure.Storage.Queues quick-start (Microsoft, 2024).
    /// </summary>
    public class EnrollmentQueueService
    {
        private readonly QueueClient _queue;

        public EnrollmentQueueService(IOptions<StorageOptions> options)
        {
            var o = options.Value;
            // Base64 encoding keeps messages compatible with Azure Functions queue triggers
            _queue = new QueueClient(o.QueueConnectionString, o.EnrollmentQueue,
                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            _queue.CreateIfNotExists();
        }

        public QueueClient Client => _queue;

        public async Task SendAsync(EnrollmentMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            await _queue.SendMessageAsync(json);
        }

        public async Task<int> GetApproximateCountAsync()
        {
            QueueProperties props = await _queue.GetPropertiesAsync();
            return props.ApproximateMessagesCount;
        }
    }
}
