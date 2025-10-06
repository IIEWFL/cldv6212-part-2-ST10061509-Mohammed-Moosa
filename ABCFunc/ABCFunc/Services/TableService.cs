using ABCFunc.Models;
using Azure;
using Azure.Data.Tables;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABCFunc.Services
{
    // Code Attribution:
    // Azure Table Storage Client: Using the Azure.Data.Tables package for .NET — Microsoft Docs — https://learn.microsoft.com/en-us/azure/data-tables/client-libraries?tabs=dotnet%2Ccli
    public class TableService
    {
        private readonly TableServiceClient _tableServiceClient;

        // Constructor Injection: Receives the TableServiceClient instance from the Dependency Injection container
        public TableService(TableServiceClient tableServiceClient)
        {
            _tableServiceClient = tableServiceClient;
        }

        // ------------------------------------------------------------------
        // Customer Operations
        // ------------------------------------------------------------------

        // Retrieves all entities from the 'CustomerProfiles' table
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient("CustomerProfiles");
            await tableClient.CreateIfNotExistsAsync();

            var customers = new List<Customer>();
            // Code Attribution:
            // Querying Table Storage: QueryAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/data-tables/client-libraries?tabs=dotnet%2Ccli#query-entities
            await foreach (var customer in tableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }
            return customers;
        }

        // Retrieves a single customer entity by its PartitionKey and RowKey
        public async Task<Customer?> GetCustomerAsync(string partitionKey, string rowKey)
        {
            var tableClient = _tableServiceClient.GetTableClient("CustomerProfiles");
            try
            {
                // Code Attribution:
                // Getting a single entity: GetEntityAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/data-tables/client-libraries?tabs=dotnet%2Ccli#get-a-single-entity
                var response = await tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                return response.Value;
            }
            // Catches Azure SDK exception if the entity is not found (HTTP 404)
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        // Adds a new customer entity to the 'CustomerProfiles' table
        public async Task AddCustomerAsync(Customer customer)
        {
            var tableClient = _tableServiceClient.GetTableClient("CustomerProfiles");
            await tableClient.CreateIfNotExistsAsync();
            // Code Attribution:
            // Adding an Entity: AddEntityAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/data-tables/client-libraries?tabs=dotnet%2Ccli#add-an-entity
            await tableClient.AddEntityAsync(customer);
        }

        // Updates an existing customer entity in the 'CustomerProfiles' table
        public async Task UpdateCustomerAsync(Customer customer)
        {
            var tableClient = _tableServiceClient.GetTableClient("CustomerProfiles");
            // Code Attribution:
            // Updating an Entity: UpdateEntityAsync method with TableUpdateMode.Replace — Microsoft Docs — https://learn.microsoft.com/en-us/azure/data-tables/client-libraries?tabs=dotnet%2Ccli#update-an-entity
            await tableClient.UpdateEntityAsync(customer, customer.ETag, TableUpdateMode.Replace);
        }

        // Deletes a customer entity by its PartitionKey and RowKey
        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            var tableClient = _tableServiceClient.GetTableClient("CustomerProfiles");
            // Code Attribution:
            // Deleting an Entity: DeleteEntityAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/data-tables/client-libraries?tabs=dotnet%2Ccli#delete-an-entity
            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // ------------------------------------------------------------------
        // Product Operations
        // ------------------------------------------------------------------

        // Retrieves all entities from the 'Products' table
        public async Task<List<Product>> GetAllProductsAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient("Products");
            await tableClient.CreateIfNotExistsAsync();

            var products = new List<Product>();
            await foreach (var product in tableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }
            return products;
        }

        // Adds a new product entity to the 'Products' table
        public async Task AddProductAsync(Product product)
        {
            var tableClient = _tableServiceClient.GetTableClient("Products");
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.AddEntityAsync(product);
        }

        // ------------------------------------------------------------------
        // Order Operations
        // ------------------------------------------------------------------

        // Retrieves all entities from the 'Orders' table
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient("Orders");
            await tableClient.CreateIfNotExistsAsync();

            var orders = new List<Order>();
            await foreach (var order in tableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }
            return orders;
        }

        // Retrieves a single order entity by its PartitionKey and RowKey
        public async Task<Order?> GetOrderAsync(string partitionKey, string rowKey)
        {
            var tableClient = _tableServiceClient.GetTableClient("Orders");
            try
            {
                var response = await tableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        // Adds a new order entity to the 'Orders' table
        public async Task AddOrderAsync(Order order)
        {
            var tableClient = _tableServiceClient.GetTableClient("Orders");
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.AddEntityAsync(order);
        }

        // Updates an existing order entity in the 'Orders' table
        public async Task UpdateOrderAsync(Order order)
        {
            var tableClient = _tableServiceClient.GetTableClient("Orders");
            await tableClient.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);
        }

        // Searches orders by a specific CustomerName using an OData filter
        public async Task<List<Order>> SearchOrdersByCustomerAsync(string customerName)
        {
            var tableClient = _tableServiceClient.GetTableClient("Orders");
            await tableClient.CreateIfNotExistsAsync();

            var orders = new List<Order>();
            // Code Attribution:
            // Querying with OData Filter: Passing a filter string to QueryAsync — Microsoft Docs — https://learn.microsoft.com/en-us/azure/data-tables/client-libraries?tabs=dotnet%2Ccli#query-entities
            await foreach (var order in tableClient.QueryAsync<Order>(
                 // Builds the filter expression: CustomerName field equals the provided value
                 filter: $"CustomerName eq '{customerName}'"))
            {
                orders.Add(order);
            }
            return orders;
        }
    }
}