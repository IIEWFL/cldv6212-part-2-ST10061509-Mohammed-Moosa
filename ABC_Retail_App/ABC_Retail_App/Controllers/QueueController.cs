// Code Attribution:
// 1. ASP.NET Core MVC: Passing Data from Controller to View — Ardalis — https://ardalis.com/passing-data-from-controllers-to-views-in-aspnet-core/
// 2. ASP.NET Core MVC with EF Core: Using Include() to load related data — Microsoft Docs — https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager

using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABC_Retail_App.Controllers
{
    public class QueueController : Controller
    {
        private readonly QueueServiceClient _queueServiceClient;
        private readonly string _orderQueueName = "order-processing";
        private readonly string _inventoryQueueName = "inventory-management";

        public QueueController(QueueServiceClient queueServiceClient)
        {
            _queueServiceClient = queueServiceClient;

            // Ensure both queues exist (create if they don’t)
            _queueServiceClient.GetQueueClient(_orderQueueName).CreateIfNotExistsAsync().Wait();
            _queueServiceClient.GetQueueClient(_inventoryQueueName).CreateIfNotExistsAsync().Wait();
        }

        // Displays the current messages in both queues (peek only, no dequeue)
        public async Task<IActionResult> Index()
        {
            ViewBag.OrderQueueMessages = await PeekMessages(_orderQueueName);
            ViewBag.InventoryQueueMessages = await PeekMessages(_inventoryQueueName);
            return View();
        }

        // Sends a new message to the specified queue
        [HttpPost]
        public async Task<IActionResult> SendMessage(string queueName, string messageContent)
        {
            if (string.IsNullOrWhiteSpace(messageContent))
            {
                TempData["ErrorMessage"] = "Message content cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            if (queueName != _orderQueueName && queueName != _inventoryQueueName)
            {
                TempData["ErrorMessage"] = "Invalid queue name.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var queueClient = _queueServiceClient.GetQueueClient(queueName);
                await queueClient.SendMessageAsync(messageContent);
                TempData["SuccessMessage"] = $"Message sent to '{queueName}' queue.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error sending message: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // Dequeues (retrieves and removes) one message from the specified queue
        [HttpPost]
        public async Task<IActionResult> DequeueMessage(string queueName)
        {
            if (queueName != _orderQueueName && queueName != _inventoryQueueName)
            {
                TempData["ErrorMessage"] = "Invalid queue name.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var queueClient = _queueServiceClient.GetQueueClient(queueName);
                QueueMessage[] retrievedMessages = await queueClient.ReceiveMessagesAsync(maxMessages: 1);

                if (retrievedMessages != null && retrievedMessages.Length > 0)
                {
                    QueueMessage message = retrievedMessages[0];
                    TempData["SuccessMessage"] = $"Dequeued message from '{queueName}': {message.MessageText}";
                    await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                }
                else
                {
                    TempData["InfoMessage"] = $"No messages in the '{queueName}' queue.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error dequeuing message: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // Peeks at up to 10 messages from a queue (non-destructive read)
        private async Task<List<string>> PeekMessages(string queueName)
        {
            var messages = new List<string>();
            try
            {
                var queueClient = _queueServiceClient.GetQueueClient(queueName);
                if (await queueClient.ExistsAsync())
                {
                    PeekedMessage[] peekedMessages = await queueClient.PeekMessagesAsync(maxMessages: 10);
                    foreach (var message in peekedMessages)
                    {
                        messages.Add(message.MessageText);
                    }
                }
            }
            catch (Exception ex)
            {
                messages.Add($"Error peeking messages: {ex.Message}");
            }
            return messages;
        }
    }
}
