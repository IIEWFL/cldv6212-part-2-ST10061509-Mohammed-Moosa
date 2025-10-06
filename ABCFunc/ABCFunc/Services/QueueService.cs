using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABCFunc.Services
{
    // Code Attribution:
    // Azure Queue Storage Client: Using the Azure.Storage.Queues package for .NET — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/queues/queue-storage-dotnet-app-how-to-use
    public class QueueService
    {
        private readonly QueueServiceClient _queueServiceClient;

        // Constructor Injection: Receives the QueueServiceClient instance from the Dependency Injection container
        public QueueService(QueueServiceClient queueServiceClient)
        {
            _queueServiceClient = queueServiceClient;
        }

        // Sends a new message to the specified queue
        public async Task SendMessageAsync(string queueName, string message)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);

            // Ensures the queue exists, creating it if necessary
            await queueClient.CreateIfNotExistsAsync();

            // Code Attribution:
            // Sending a Message: SendMessageAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/queues/queue-storage-dotnet-app-how-to-use#send-a-message
            await queueClient.SendMessageAsync(message);
        }

        // Retrieves one or more messages from the queue and makes them invisible (in-flight)
        public async Task<QueueMessage[]> ReceiveMessagesAsync(string queueName, int maxMessages = 1)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();

            // Code Attribution:
            // Receiving Messages: ReceiveMessagesAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/queues/queue-storage-dotnet-app-how-to-use#receive-messages
            Response<QueueMessage[]> response = await queueClient.ReceiveMessagesAsync(maxMessages);
            return response.Value;
        }

        // Deletes a specific message from the queue using its ID and pop receipt
        public async Task DeleteMessageAsync(string queueName, string messageId, string popReceipt)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);

            // Code Attribution:
            // Deleting a Message: DeleteMessageAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/queues/queue-storage-dotnet-app-how-to-use#delete-messages
            await queueClient.DeleteMessageAsync(messageId, popReceipt);
        }

        // Peeks (reads without removing) messages from the front of the queue
        public async Task<List<string>> PeekMessagesAsync(string queueName, int maxMessages = 10)
        {
            var messages = new List<string>();
            var queueClient = _queueServiceClient.GetQueueClient(queueName);

            if (await queueClient.ExistsAsync())
            {
                // Code Attribution:
                // Peeking Messages: PeekMessagesAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/queues/queue-storage-dotnet-app-how-to-use#peek-messages
                PeekedMessage[] peekedMessages = await queueClient.PeekMessagesAsync(maxMessages);
                foreach (var message in peekedMessages)
                {
                    messages.Add(message.MessageText);
                }
            }
            return messages;
        }

        // Gets the approximate number of messages in the queue
        public async Task<int> GetQueueLengthAsync(string queueName)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            if (await queueClient.ExistsAsync())
            {
                // Code Attribution:
                // Getting Queue Properties: GetPropertiesAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/queues/queue-storage-dotnet-app-how-to-use#get-queue-properties
                Response<QueueProperties> properties = await queueClient.GetPropertiesAsync();
                return properties.Value.ApproximateMessagesCount;
            }
            return 0;
        }
    }
}