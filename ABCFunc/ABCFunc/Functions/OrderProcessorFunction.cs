using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ABCFunc.Models;
using System;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Tables;

namespace ABCFunc.Functions
{
    // Code Attribution:
    // Azure Functions Dependency Injection: Using dependency injection in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#dependency-injection
    public static class OrderProcessorFunction
    {
        [Function("ProcessOrderQueue")]
        // Code Attribution:
        // Azure Functions Table Output Binding: TableOutput binding in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-table-output?tabs=isolated-process%2Cextensionv5&pivots=programming-language-csharp
        [TableOutput("OrdersTable", Connection = "AzureWebJobsStorage")]
        // Code Attribution:
        // Azure Functions Queue Trigger: QueueTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#queuetrigger
        public static Order Run(
            [QueueTrigger("order-processing", Connection = "AzureWebJobsStorage")] string queueMessage,
            FunctionContext context)
        {
            var logger = context.GetLogger("OrderProcessorFunction");
            logger.LogInformation($"C# Queue trigger function received message");

            try
            {
                // Decode the message, assuming it was Base64 encoded before being placed in the queue
                // Code Attribution:
                // Azure Queue Storage: Base64 encoding for queue messages — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/queues/queue-storage-dotnet-app-how-to-use#encode-message-content
                byte[] data = Convert.FromBase64String(queueMessage);
                string decodedJson = Encoding.UTF8.GetString(data);

                logger.LogInformation($"Decoded message: {decodedJson}");

                // Deserialize the decoded JSON message into an Order object
                // Code Attribution:
                // JSON Deserialization: Reading JSON payload in .NET — Microsoft Docs — https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to?pivots=dotnet-7-0
                var order = JsonSerializer.Deserialize<Order>(decodedJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Allows deserialization to match PascalCase model properties
                });

                if (order == null)
                {
                    logger.LogError("Failed to deserialize order");
                    throw new InvalidOperationException("Order deserialization failed");
                }

                // Ensure PartitionKey is set before writing to Table Storage
                if (string.IsNullOrEmpty(order.PartitionKey))
                {
                    order.PartitionKey = "Orders";
                }

                // Ensure RowKey is set (though it should be set by the submit function)
                if (string.IsNullOrEmpty(order.RowKey))
                {
                    order.RowKey = Guid.NewGuid().ToString();
                }

                logger.LogInformation($"Order {order.RowKey} processed successfully for customer: {order.CustomerName}");

                // Return the Order object. The TableOutput binding will automatically save it to "OrdersTable".
                return order;
            }
            catch (FormatException ex)
            {
                logger.LogError($"Error decoding Base64 message: {ex.Message}");
                throw; // Triggers retry mechanism for queue message
            }
            catch (JsonException ex)
            {
                logger.LogError($"Error deserializing JSON: {ex.Message}");
                throw; // Triggers retry mechanism for queue message
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing queue message: {ex.Message}");
                throw; // Triggers retry mechanism for queue message
            }
        }
    }
}