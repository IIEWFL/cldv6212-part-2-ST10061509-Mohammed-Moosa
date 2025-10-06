using System.Net.Http.Headers;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace ABC_Retail_App.Services
{
    // Code Attribution:
    // HttpClient Factory: Recommended approach for making HTTP requests in .NET Core — Microsoft Docs — https://learn.microsoft.com/en-us/dotnet/fundamentals/http/httpclient-factory
    public class AzureFunctionsService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl; // Base URL for deployed Functions

        // Constructor Injection: Retrieves HttpClientFactory and Configuration from the DI container
        public AzureFunctionsService(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _http = httpFactory.CreateClient();
            // Code Attribution:
            // Configuration Access: Using GetValue<T> to retrieve strongly-typed settings — Microsoft Docs — https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.configurationextensions.getvalue
            _baseUrl = config.GetValue<string>("AzureFunctionsBaseUrl");
        }


        // TABLE STORAGE Operations
        // Sends a POST request to an Azure Function to add an entity to a specified Azure Table.
        public async Task<HttpResponseMessage> AddToTableAsync(string tableName, object entity)
        {
            // Assumes the function endpoint is like: POST /api/tables/{tableName}
            var url = $"{_baseUrl}/api/tables/{tableName}";
            
            // Serialize the entity object to JSON
            var json = System.Text.Json.JsonSerializer.Serialize(entity);
            
            // Code Attribution:
            // HttpClient POST Request: Sending JSON content — Microsoft Docs — https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.postasync
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return await _http.PostAsync(url, content);
        }


        // BLOB STORAGE Operations
        // Sends a file stream as the request body to an Azure Function for Blob upload.
        public async Task<HttpResponseMessage> UploadBlobAsync(string containerName, Stream fileStream, string fileName)
        {
            // Assumes the function endpoint is like: POST /api/blobs/{containerName}
            var url = $"{_baseUrl}/api/blobs/{containerName}";
            
            // Code Attribution:
            // StreamContent: Creating HTTP content from a stream — Microsoft Docs — https://learn.microsoft.com/en-us/dotnet/api/system.net.http.streamcontent
            var content = new StreamContent(fileStream);
            
            // Set content type for binary data transfer
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // Tell the function the filename via custom header, required for functions to know the destination blob name
            _http.DefaultRequestHeaders.Remove("x-filename");
            _http.DefaultRequestHeaders.Add("x-filename", fileName);

            return await _http.PostAsync(url, content);
        }


        // QUEUE STORAGE Operations
        // Sends an order object to an Azure Function to be enqueued in Azure Queue Storage.

        public async Task<HttpResponseMessage> EnqueueOrderAsync(object order)
        {
            // Assumes the function endpoint is like: POST /api/orders/enqueue
            var url = $"{_baseUrl}/api/orders/enqueue";
            
            // Serialize the order object to JSON (the queue message payload)
            var json = System.Text.Json.JsonSerializer.Serialize(order);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            return await _http.PostAsync(url, content);
        }


        // FILE STORAGE Operations
        // Sends a file stream as the request body to an Azure Function for Azure File Share upload.

            public async Task<HttpResponseMessage> UploadFileShareAsync(string shareName, Stream fileStream, string fileName)
        {
            // Assumes the function endpoint is like: POST /api/files/{shareName}
            var url = $"{_baseUrl}/api/files/{shareName}";
            var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // Tell the function the filename via custom header, required for functions to know the destination file name
            _http.DefaultRequestHeaders.Remove("x-filename");
            _http.DefaultRequestHeaders.Add("x-filename", fileName);

            return await _http.PostAsync(url, content);
        }
    }
}
