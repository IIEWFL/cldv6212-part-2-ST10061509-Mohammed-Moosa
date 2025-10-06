using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using ABCFunc.Services;
using ABCFunc.Models;

namespace ABCFunc.Functions
{
    // Code Attribution:
    // ASP.NET Core MVC with EF Core: Using dependency injection in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#dependency-injection
    public class SubmitOrderFunction
    {
        private readonly ILogger<SubmitOrderFunction> _logger;
        private readonly QueueService _queueService;

        // Constructor Injection: The host provides the required services (Logger and QueueService)
        public SubmitOrderFunction(ILoggerFactory loggerFactory, QueueService queueService)
        {
            _logger = loggerFactory.CreateLogger<SubmitOrderFunction>();
            _queueService = queueService;
        }

        [Function("SubmitNewOrder")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            const string queueName = "order-processing";

            _logger.LogInformation("Processing new order submission request.");

            try
            {
                // Read the JSON payload from the HTTP request body and deserialize it into the Order model
                var order = await req.ReadFromJsonAsync<Order>();

                // Check for null or required fields (ProductName is used as an example)
                if (order == null || string.IsNullOrWhiteSpace(order.ProductName))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid order data. ProductName is required.");
                    return badResponse;
                }

                // Set the necessary Table Storage keys and default fields for the new order
                order.PartitionKey = "Orders"; // Sets the partition key for Table Storage
                order.RowKey = Guid.NewGuid().ToString(); // Generates a unique Order ID
                order.OrderDate = DateTime.UtcNow;
                order.Status = "Pending";

                // Serialize the Order object into a JSON string for the queue message
                string queueMessageJson = JsonSerializer.Serialize(order);

                // Code Attribution:
                // Azure Queue Storage: Base64 encoding for queue messages — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/queues/queue-storage-dotnet-app-how-to-use#encode-message-content
                // Base64 ENCODE the JSON string (a best practice for Azure Queue messages)
                string base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(queueMessageJson));

                // Use the injected QueueService to send the Base64 message to the specified queue
                await _queueService.SendMessageAsync(queueName, base64Message);

                _logger.LogInformation($"Order {order.RowKey} queued successfully for {order.CustomerName}");

                // Return the successful HTTP response back to the MVC application
                var response = req.CreateResponse(HttpStatusCode.OK);
                // Return Order ID and Status, which the MVC Controller expects to display in TempData
                await response.WriteAsJsonAsync(new
                {
                    orderId = order.RowKey,
                    status = order.Status,
                    message = $"Order submitted successfully for {order.CustomerName}"
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order submission.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: An unexpected server error occurred: {ex.Message}");
                return errorResponse;
            }
        }
    }
}