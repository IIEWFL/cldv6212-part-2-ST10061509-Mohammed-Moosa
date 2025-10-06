using ABC_Retail_App.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace ABC_Retail_App.Controllers
{
    public class OrderController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _functionBaseUrl;
        private readonly string _functionKey;

        public OrderController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _functionBaseUrl = _configuration["AzureFunctions:BaseUrl"];
            _functionKey = _configuration["AzureFunctions:FunctionKey"];
        }

        // Display all orders
        public async Task<IActionResult> Index(string searchTerm)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var functionUrl = $"{_functionBaseUrl}/api/GetOrders?code={_functionKey}";

                var response = await httpClient.GetAsync(functionUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var orders = JsonSerializer.Deserialize<List<Order>>(content) ?? new List<Order>();

                    // Search functionality
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerName,ProductName,Quantity,TotalPrice")] Order order)
        {
            ModelState.Remove("PartitionKey");
            ModelState.Remove("RowKey");
            ModelState.Remove("Status");

            if (ModelState.IsValid)
            {
                try
                {
                    var httpClient = _httpClientFactory.CreateClient();
                    var functionUrl = $"{_functionBaseUrl}/api/SubmitOrder?code={_functionKey}";

                    // Ensures all necessary fields (including keys/status) are sent to the Azure Function.
                    var orderData = new
                    {
                        customerName = order.CustomerName,
                        productName = order.ProductName,
                        quantity = order.Quantity,
                        totalPrice = order.TotalPrice,
                        // Include Table Storage fields for complete object structure on the function side
                        partitionKey = "Orders",
                        rowKey = Guid.NewGuid().ToString(),
                        status = "Pending"
                    };

                    var json = JsonSerializer.Serialize(orderData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(functionUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
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
                var functionUrl = $"{_functionBaseUrl}/api/GetOrder?orderId={id}&code={_functionKey}";

                var response = await httpClient.GetAsync(functionUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
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

        // Update order status
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string orderId, string newStatus)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var functionUrl = $"{_functionBaseUrl}/api/UpdateOrderStatus?code={_functionKey}";

                var statusData = new
                {
                    orderId = orderId,
                    status = newStatus
                };

                var json = JsonSerializer.Serialize(statusData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

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