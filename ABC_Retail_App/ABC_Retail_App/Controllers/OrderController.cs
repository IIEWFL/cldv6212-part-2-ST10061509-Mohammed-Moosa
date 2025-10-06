using ABC_Retail_App.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ABC_Retail_App.Controllers
{
    // Code Attribution:
    // ASP.NET Core MVC Controller: Structure and functionality — Microsoft Docs — https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions
    public class OrderController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _functionBaseUrl;
        private readonly string _functionKey;

        // Constructor Injection: Retrieves HttpClientFactory and Configuration from the DI container
        public OrderController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            
            // Code Attribution:
            // Configuration Access: Reading settings from IConfiguration — Microsoft Docs — https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration
            _functionBaseUrl = _configuration["AzureFunctions:BaseUrl"];
            _functionKey = _configuration["AzureFunctions:FunctionKey"];
        }

        // Display all orders, with optional search functionality
        public async Task<IActionResult> Index(string searchTerm)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                // Construct URL for the Azure Function that retrieves ALL orders (GetOrders)
                var functionUrl = $"{_functionBaseUrl}/api/GetOrders?code={_functionKey}";

                // Send GET request to the Azure Function
                var response = await httpClient.GetAsync(functionUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    // Deserialize the JSON response into a list of Order objects
                    var orders = JsonSerializer.Deserialize<List<Order>>(content) ?? new List<Order>();

                    // Search functionality (filtering client-side after retrieving all orders)
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        orders = orders.Where(o =>
                            // 1. Search Customer Name
                            (o.CustomerName != null && o.CustomerName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                            // 2. Search Order ID / RowKey
                            (o.RowKey != null && o.RowKey.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                            // 3. Search Status
                            (o.Status != null && o.Status.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        ).ToList();

                        ViewBag.SearchTerm = searchTerm;
                    }

                    return View(orders);
                }
                else
                {
                    ViewBag.ErrorMessage = "Failed to retrieve orders from Azure Functions";
                    return View(new List<Order>());
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error: {ex.Message}";
                return View(new List<Order>());
            }
        }

        // Display create order form
        public IActionResult Create()
        {
            return View();
        }

        // Submit new order (sends to Azure Function → Queue)
        // Code Attribution:
        // ASP.NET Core Form Submission: [HttpPost] and [ValidateAntiForgeryToken] — Microsoft Docs — https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerName,ProductName,Quantity,TotalPrice")] Order order)
        {
            // Remove Table Storage fields from validation since they are set here or on the Function side
            ModelState.Remove("PartitionKey");
            ModelState.Remove("RowKey");
            ModelState.Remove("Status");

            if (ModelState.IsValid)
            {
                try
                {
                    var httpClient = _httpClientFactory.CreateClient();
                    // Construct URL for the Azure Function that submits an order (SubmitOrder)
                    var functionUrl = $"{_functionBaseUrl}/api/SubmitOrder?code={_functionKey}";

                    // Prepare the order data, including initial keys/status for the function's queue processing
                    var orderData = new
                    {
                        customerName = order.CustomerName,
                        productName = order.ProductName,
                        quantity = order.Quantity,
                        totalPrice = order.TotalPrice,
                        // Include Table Storage fields for complete object structure on the function side
                        partitionKey = "Orders",
                        rowKey = Guid.NewGuid().ToString(), // Assign new ID here before sending to queue
                        status = "Pending"
                    };

                    // Serialize the data and create HTTP content
                    var json = JsonSerializer.Serialize(orderData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Send POST request to the Azure Function
                    var response = await httpClient.PostAsync(functionUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        // Deserialize the response to extract the Order ID and initial status
                        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        TempData["SuccessMessage"] = $"Order submitted successfully! Order ID: {result.GetProperty("orderId").GetString()}. Status: {result.GetProperty("status").GetString()}";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        ViewBag.ErrorMessage = $"Failed to submit order. Status: {response.StatusCode}. Details: {errorContent}";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = $"Error submitting order: {ex.Message}";
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Please fill in all required fields correctly.";
            }

            return View(order);
        }

        // View order details
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                // Construct URL for the Azure Function that retrieves a single order (GetOrder)
                var functionUrl = $"{_functionBaseUrl}/api/GetOrder?orderId={id}&code={_functionKey}";

                var response = await httpClient.GetAsync(functionUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    // Deserialize the response. Use PropertyNameCaseInsensitive=true for flexible deserialization
                    var order = JsonSerializer.Deserialize<Order>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (order == null)
                    {
                        return NotFound();
                    }

                    return View(order);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error: {ex.Message}";
                return View();
            }
        }

        // Update order status via POST request to an Azure Function
        [HttpPost]
        // Code Attribution:
        // ASP.NET Core Redirection: RedirectToAction method — Microsoft Docs — https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.redirecttoaction
        public async Task<IActionResult> UpdateStatus(string orderId, string newStatus)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                // Construct URL for the Azure Function that updates order status (UpdateOrderStatus)
                var functionUrl = $"{_functionBaseUrl}/api/UpdateOrderStatus?code={_functionKey}";

                // Prepare payload with the ID and new Status
                var statusData = new
                {
                    orderId = orderId,
                    status = newStatus
                };

                var json = JsonSerializer.Serialize(statusData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send POST request to the Azure Function
                var response = await httpClient.PostAsync(functionUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = $"Order status updated to: {newStatus}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update order status";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
