using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ABCFunc.Models;

namespace ABCFunc.Functions
{
    // Code Attribution:
    // Azure Functions Dependency Injection: Using dependency injection in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#dependency-injection
    public class GetOrdersFunction
    {
        private readonly ILogger<GetOrdersFunction> _logger;

        // Constructor Injection: The host provides the required Logger service
        public GetOrdersFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetOrdersFunction>();
        }

        [Function("GetOrders")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Retrieving all orders from Table Storage");

            try
            {
                // Retrieve the connection string from application settings
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                // Code Attribution:
                // Azure Table Storage Client: Creating a TableClient instance — Microsoft Docs — https://learn.microsoft.com/en-us/azure/data-tables/client-libraries?tabs=dotnet%2Ccli#create-a-table-client
                // Create a TableClient instance for the "OrdersTable"
                var tableClient = new TableClient(connectionString, "OrdersTable");

                var orders = new List<Order>();

                // Code Attribution:
                // Azure Table Storage Query: Querying entities using the TableClient — Microsoft Docs — https://learn.microsoft.com/en-us/azure/data-tables/client-libraries?tabs=dotnet%2Ccli#query-entities
                // Query Table Storage for all entities where PartitionKey equals 'Orders'
                await foreach (var order in tableClient.QueryAsync<Order>(filter: "PartitionKey eq 'Orders'"))
                {
                    orders.Add(order);
                }

                _logger.LogInformation($"Retrieved {orders.Count} orders");

                // Return the retrieved list of orders as a successful JSON response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(orders);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}