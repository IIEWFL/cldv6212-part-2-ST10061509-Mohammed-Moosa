// Code Attribution:
// 1. ASP.NET Core MVC: How to create CRUD operations in MVC — TutorialsTeacher — https://www.tutorialsteacher.com/mvc/mvc-crud-operations
// 2. ASP.NET Core MVC with Entity Framework Core: CRUD Operations — Microsoft Docs — https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/crud

using ABC_Retail_App.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABC_Retail_App.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly string _tableName = "CustomerProfiles";

        public CustomerController(TableServiceClient tableServiceClient)
        {
            _tableServiceClient = tableServiceClient;
            // Ensure the Azure Table exists (creates it if not found)
            _tableServiceClient.CreateTableIfNotExists(_tableName);
        }

        // Returns a client instance for interacting with the CustomerProfiles table
        private TableClient GetTableClient()
        {
            return _tableServiceClient.GetTableClient(_tableName);
        }

        // Displays a list of all customers from Azure Table Storage
        public async Task<IActionResult> Index()
        {
            var tableClient = GetTableClient();
            var customers = new List<Customer>();
            try
            {
                await foreach (var customer in tableClient.QueryAsync<Customer>())
                {
                    customers.Add(customer);
                }
            }
            catch (RequestFailedException ex)
            {
                ViewBag.ErrorMessage = $"Error retrieving customers: {ex.Message}";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
            }
            return View(customers);
        }

        // Renders the Create form for adding a new customer
        public IActionResult Create()
        {
            return View();
        }

        // Handles submission of the Create form and saves a new customer to Azure Table Storage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Phone")] Customer customer)
        {
            customer.PartitionKey = "USA";
            customer.RowKey = Guid.NewGuid().ToString();
            customer.CreatedDate = DateTime.UtcNow;

            ModelState.Remove("PartitionKey");
            ModelState.Remove("RowKey");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var tableClient = GetTableClient();
                    await tableClient.AddEntityAsync(customer);
                    return RedirectToAction(nameof(Index));
                }
                catch (RequestFailedException ex)
                {
                    ViewBag.ErrorMessage = $"Error creating customer: {ex.Message}";
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                }
            }

            return View(customer);
        }

        // Loads a customer's data for editing based on PartitionKey and RowKey
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            try
            {
                var tableClient = GetTableClient();
                var response = await tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                return View(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                return View();
            }
        }

        // Handles submission of the Edit form and updates an existing customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, [Bind("PartitionKey,RowKey,Name,Email,Phone,ETag")] Customer customer)
        {
            if (partitionKey != customer.PartitionKey || rowKey != customer.RowKey)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var tableClient = GetTableClient();
                    var existingCustomer = await tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                    var updatedCustomer = existingCustomer.Value;

                    updatedCustomer.Name = customer.Name;
                    updatedCustomer.Email = customer.Email;
                    updatedCustomer.Phone = customer.Phone;
                    updatedCustomer.ETag = customer.ETag;

                    await tableClient.UpdateEntityAsync(updatedCustomer, updatedCustomer.ETag, TableUpdateMode.Replace);
                    return RedirectToAction(nameof(Index));
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    return NotFound();
                }
                catch (RequestFailedException ex) when (ex.Status == 412)
                {
                    ModelState.AddModelError(string.Empty, "The customer was modified by another user. Please resolve conflicts and try again.");
                    var currentCustomer = await GetTableClient().GetEntityAsync<Customer>(partitionKey, rowKey);
                    return View(currentCustomer.Value);
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                }
            }
            return View(customer);
        }

        // Loads a customer's data for confirmation before deletion
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            try
            {
                var tableClient = GetTableClient();
                var response = await tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                return View(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                return View();
            }
        }

        // Confirms and executes deletion of a customer from Azure Table Storage
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            try
            {
                var tableClient = GetTableClient();
                await tableClient.DeleteEntityAsync(partitionKey, rowKey);
                return RedirectToAction(nameof(Index));
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                return View();
            }
        }
    }
}