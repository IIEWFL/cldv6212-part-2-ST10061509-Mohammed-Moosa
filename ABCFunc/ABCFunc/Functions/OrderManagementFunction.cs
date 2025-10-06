using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCFunc.Models;
using Azure;
using ABCFunc.Services;
using System;
using System.Threading.Tasks;

namespace ABCFunc.Functions
{
    // Code Attribution:
    // Azure Functions Dependency Injection: Using dependency injection in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#dependency-injection
    public class OrderManagementFunction
    {
        private readonly ILogger _logger;
        private readonly TableService _tableService;

        // Constructor Injection: The host provides the required services (Logger and TableService)
        public OrderManagementFunction(ILoggerFactory loggerFactory, TableService tableService)
        {
            _logger = loggerFactory.CreateLogger<OrderManagementFunction>();
            _tableService = tableService; // Inject TableService for interacting with Azure Table Storage
        }

        // Get all orders
        [Function("GetOrders")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> GetOrders(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("GetOrders function processing request");

            try
            {
                // Use the injected service to retrieve all orders from Azure Table Storage
                var orders = await _tableService.GetAllOrdersAsync();

                // Return the list of orders as a successful JSON response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(orders);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting orders: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        // Get single order by ID
        [Function("GetOrder")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> GetOrder(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("GetOrder function processing request");

            try
            {
                // Note: The use of System.Web.HttpUtility.ParseQueryString assumes a reference to System.Web is available.
                // Parse the query string to extract the 'orderId' parameter
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var orderId = query["orderId"];

                if (string.IsNullOrEmpty(orderId))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("OrderId is required");
                    return badResponse;
                }

                // Use the service to retrieve the specific order record (PartitionKey is hardcoded as "Orders")
                var order = await _tableService.GetOrderAsync("Orders", orderId);

                if (order == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync("Order not found");
                    return notFoundResponse;
                }

                // Return the single order as a successful JSON response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(order);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        // Update order status
        [Function("UpdateOrderStatus")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> UpdateOrderStatus(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("UpdateOrderStatus function processing request");

            UpdateStatusRequest? requestBody = null;

            try
            {
                // Read the JSON request body and deserialize it into the UpdateStatusRequest model
                requestBody = await req.ReadFromJsonAsync<UpdateStatusRequest>();

                if (requestBody == null || string.IsNullOrEmpty(requestBody.OrderId))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid request data: OrderId is missing.");
                    return badResponse;
                }

                // Get the existing order using the service (required for RowKey/PartitionKey for update)
                var order = await _tableService.GetOrderAsync("Orders", requestBody.OrderId);

                if (order == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"Order with ID {requestBody.OrderId} not found.");
                    return notFoundResponse;
                }

                // Update the status and set the processed date to now
                order.Status = requestBody.Status;
                order.ProcessedDate = DateTime.UtcNow;

                // Update the order using the injected TableService
                await _tableService.UpdateOrderAsync(order);

                _logger.LogInformation($"Order {order.RowKey} status updated to {order.Status}");

                // Return a successful response with the updated details
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "Order status updated successfully",
                    orderId = order.RowKey,
                    newStatus = order.Status
                });
                return response;
            }
            // Catch a specific exception for Azure Table Storage if the entity isn't found during an update operation
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Order with ID {requestBody?.OrderId} not found.");
                return notFoundResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order status: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }

    // Data contract for updating the status (used for deserializing the request body)
    public class UpdateStatusRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}