using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCFunc.Models;
using ABCFunc.Services;
using System;
using System.Threading.Tasks;

namespace ABCFunc.Functions
{
    // Code Attribution:
    // Azure Functions Dependency Injection: Using dependency injection in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#dependency-injection
    public class OrderQueueFunction
    {
        private readonly ILogger _logger;
        private readonly QueueService _queueService;
        private const string QueueName = "order-processing";

        // Constructor Injection: The host provides the required services (Logger and QueueService)
        public OrderQueueFunction(ILoggerFactory loggerFactory, QueueService queueService)
        {
            _logger = loggerFactory.CreateLogger<OrderQueueFunction>();
            _queueService = queueService; // Inject QueueService for interacting with Azure Queue Storage
        }

        [Function("SubmitOrder")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> SubmitOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("SubmitOrder function processing request");

            try
            {
                // Read the JSON payload from the HTTP request body and deserialize it into the Order model
                var order = await req.ReadFromJsonAsync<Order>();
                if (order == null)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid order data");
                    return badResponse;
                }

                // Set required Azure Table Storage keys and default properties
                order.PartitionKey = "Orders";
                order.RowKey = Guid.NewGuid().ToString();
                order.OrderDate = DateTime.UtcNow;
                order.Status = "Pending";

                // Code Attribution:
                // JSON Serialization: Writing JSON payload in .NET — Microsoft Docs — https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to?pivots=dotnet-7-0
                // Serialize the Order object into a JSON string for the queue message
                var messageJson = JsonSerializer.Serialize(order);

                // Use the injected service to send the message to the Azure Queue
                await _queueService.SendMessageAsync(QueueName, messageJson);

                _logger.LogInformation($"Order {order.RowKey} sent to queue successfully");

                // Return a successful HTTP response to the client
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "Order submitted successfully",
                    orderId = order.RowKey,
                    status = "Pending - Order is being processed"
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error submitting order: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}