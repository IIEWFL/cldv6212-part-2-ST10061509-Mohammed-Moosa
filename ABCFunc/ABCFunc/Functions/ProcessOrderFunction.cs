using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ABCFunc.Models;
using ABCFunc.Services;
using System;
using System.Threading.Tasks;

namespace ABCFunc.Functions
{
    // Code Attribution:
    // Azure Functions Dependency Injection: Using dependency injection in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#dependency-injection
    public class ProcessOrderFunction
    {
        private readonly ILogger _logger;
        private readonly TableService _tableService;

        // Constructor Injection: The host provides the required services (Logger and TableService)
        public ProcessOrderFunction(ILoggerFactory loggerFactory, TableService tableService)
        {
            _logger = loggerFactory.CreateLogger<ProcessOrderFunction>();
            _tableService = tableService; // Inject TableService for interacting with Azure Table Storage
        }

        [Function("ProcessOrder")]
        // Code Attribution:
        // Azure Functions Queue Trigger: QueueTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#queuetrigger
        public async Task Run(
            [QueueTrigger("order-processing", Connection = "AzureWebJobsStorage")] string queueMessage)
        {
            _logger.LogInformation($" ProcessOrder triggered with message: {queueMessage}");

            try
            {
                // Code Attribution:
                // JSON Deserialization: Reading JSON payload in .NET — Microsoft Docs — https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to?pivots=dotnet-7-0
                // Deserialize the Order object from the queue message JSON string
                var order = JsonSerializer.Deserialize<Order>(queueMessage);
                if (order == null)
                {
                    _logger.LogError("Failed to deserialize order message");
                    return;
                }

                _logger.LogInformation($"Processing order: {order.RowKey}");

                // Update status to 'Processing' upon starting work
                order.Status = "Processing";
                order.ProcessedDate = DateTime.UtcNow;

                // Use the injected service to perform the initial save/insertion to Azure Table Storage
                await _tableService.AddOrderAsync(order);

                _logger.LogInformation($"Order {order.RowKey} saved to table storage");

                // Simulate processing time
                await Task.Delay(2000);

                // Update status to 'Completed' after simulating work is done
                order.Status = "Completed";
                // Use the injected service to update the existing record in Azure Table Storage
                await _tableService.UpdateOrderAsync(order);

                _logger.LogInformation($" Order {order.RowKey} processing completed");
            }
            catch (Exception ex)
            {
                _logger.LogError($" Error processing order: {ex.Message}");
                throw; // Throwing the exception ensures the message returns to the queue for a retry attempt
            }
        }
    }
}