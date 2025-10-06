using System.Net;
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
    public class CustomerTableFunction
    {
        private readonly ILogger _logger;
        private readonly TableService _tableService;

        // Constructor Injection: The host provides the required services (Logger and TableService)
        public CustomerTableFunction(ILoggerFactory loggerFactory, TableService tableService)
        {
            _logger = loggerFactory.CreateLogger<CustomerTableFunction>();
            _tableService = tableService; // Inject TableService for interacting with Azure Table Storage
        }

        [Function("AddCustomer")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> AddCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("AddCustomer function processing request");

            try
            {
                // Read the JSON payload from the HTTP request body and deserialize it into the Customer model
                var customer = await req.ReadFromJsonAsync<Customer>();
                if (customer == null)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid customer data");
                    return badResponse;
                }

                // Set required Azure Table Storage keys and default properties
                customer.PartitionKey = "USA"; // Example static PartitionKey
                customer.RowKey = Guid.NewGuid().ToString(); // Generate a unique RowKey (Customer ID)
                customer.CreatedDate = DateTime.UtcNow;

                // Use the injected service to insert the new customer entity
                await _tableService.AddCustomerAsync(customer);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "Customer added successfully",
                    customerId = customer.RowKey
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding customer: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetCustomers")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> GetCustomers(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("GetCustomers function processing request");

            try
            {
                // Use the injected service to retrieve all customer entities
                var customers = await _tableService.GetAllCustomersAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(customers);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting customers: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("UpdateCustomer")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> UpdateCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "put")] HttpRequestData req)
        {
            _logger.LogInformation("UpdateCustomer function processing request");

            try
            {
                // Read the JSON payload, which must contain PartitionKey and RowKey for update
                var customer = await req.ReadFromJsonAsync<Customer>();
                if (customer == null)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid customer data");
                    return badResponse;
                }

                // Use the injected service to update the existing customer entity
                await _tableService.UpdateCustomerAsync(customer);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { message = "Customer updated successfully" });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating customer: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("DeleteCustomer")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> DeleteCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "delete")] HttpRequestData req)
        {
            _logger.LogInformation("DeleteCustomer function processing request");

            try
            {
                // Note: The use of System.Web.HttpUtility.ParseQueryString assumes a reference to System.Web is available.
                // Parse the query string to extract the required composite keys
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var partitionKey = query["partitionKey"];
                var rowKey = query["rowKey"];

                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("PartitionKey and RowKey are required");
                    return badResponse;
                }

                // Use the injected service to delete the specified customer entity
                await _tableService.DeleteCustomerAsync(partitionKey, rowKey);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { message = "Customer deleted successfully" });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting customer: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}