using System.Net.Http.Headers;

namespace ABC_Retail_App.Services
{
    public class AzureFunctionsService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl; // Base URL for deployed Functions

        public AzureFunctionsService(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _http = httpFactory.CreateClient();
            _baseUrl = config.GetValue<string>("AzureFunctionsBaseUrl"); 
        }

        // TABLE STORAGE
        public async Task<HttpResponseMessage> AddToTableAsync(string tableName, object entity)
        {
            var url = $"{_baseUrl}/api/tables/{tableName}";
            var json = System.Text.Json.JsonSerializer.Serialize(entity);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return await _http.PostAsync(url, content);
        }

        // BLOB STORAGE
        public async Task<HttpResponseMessage> UploadBlobAsync(string containerName, Stream fileStream, string fileName)
        {
            var url = $"{_baseUrl}/api/blobs/{containerName}";
            var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // Tell the function the filename via header
            _http.DefaultRequestHeaders.Remove("x-filename");
            _http.DefaultRequestHeaders.Add("x-filename", fileName);

            return await _http.PostAsync(url, content);
        }

        // QUEUE STORAGE
        public async Task<HttpResponseMessage> EnqueueOrderAsync(object order)
        {
            var url = $"{_baseUrl}/api/orders/enqueue";
            var json = System.Text.Json.JsonSerializer.Serialize(order);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return await _http.PostAsync(url, content);
        }

        // FILE STORAGE
        public async Task<HttpResponseMessage> UploadFileShareAsync(string shareName, Stream fileStream, string fileName)
        {
            var url = $"{_baseUrl}/api/files/{shareName}";
            var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // Tell the function the filename via header
            _http.DefaultRequestHeaders.Remove("x-filename");
            _http.DefaultRequestHeaders.Add("x-filename", fileName);

            return await _http.PostAsync(url, content);
        }
    }
}
