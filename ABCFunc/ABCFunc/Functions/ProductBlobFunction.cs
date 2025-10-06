using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Text;
using ABCFunc.Services;

namespace ABCFunc.Functions
{
    // Helper class to extract file data from the multipart request stream
    public class FileData
    {
        public Stream Stream { get; set; } = Stream.Null;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public long Length { get; set; }
    }

    // Code Attribution:
    // Azure Functions Dependency Injection: Using dependency injection in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#dependency-injection
    public class ProductBlobFunction
    {
        private readonly ILogger _logger;
        private readonly BlobService _blobService;
        private const string ContainerName = "product-images";

        // Constructor Injection: The host provides the required services (Logger and BlobService)
        public ProductBlobFunction(ILoggerFactory loggerFactory, BlobService blobService)
        {
            _logger = loggerFactory.CreateLogger<ProductBlobFunction>();
            _blobService = blobService; // Inject BlobService for interacting with Azure Blob Storage
        }

        [Function("UploadProductImage")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        // File Upload Processing: Reading multipart/form-data using MultipartReader — Microsoft Docs — https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-8.0
        public async Task<HttpResponseData> UploadImage(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("UploadProductImage function processing request.");

            try
            {
                // Helper method to parse the multipart/form-data request and extract the file stream
                var fileData = await ReadFileFromRequest(req);

                if (fileData.Length == 0)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("No file uploaded or file part not found.");
                    return badResponse;
                }

                // Use the injected BlobService to upload the file stream to Azure Blob Storage
                await _blobService.UploadBlobAsync(
                    ContainerName,
                    fileData.FileName,
                    fileData.Stream,
                    fileData.ContentType
                );

                // Get the public URL for the newly uploaded blob
                var url = _blobService.GetBlobUrl(ContainerName, fileData.FileName);

                // Return a successful response with the file details
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "Image uploaded successfully",
                    fileName = fileData.FileName,
                    url
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading image: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetProductImages")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> GetImages(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("GetProductImages function processing request.");

            try
            {
                // Use the injected BlobService to retrieve a list of all blobs in the container
                var blobs = await _blobService.GetAllBlobsAsync(ContainerName);

                // Return the list of blobs as a JSON response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(blobs);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting images: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("DeleteProductImage")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> DeleteImage(
            [HttpTrigger(AuthorizationLevel.Function, "delete")] HttpRequestData req)
        {
            _logger.LogInformation("DeleteProductImage function processing request.");

            try
            {
                // Parse the query string to get the 'fileName' parameter
                // Note: The use of System.Web.HttpUtility.ParseQueryString assumes a reference to System.Web is available.
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var fileName = query["fileName"];

                if (string.IsNullOrEmpty(fileName))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("FileName is required");
                    return badResponse;
                }

                // Use the injected BlobService to delete the specified blob
                await _blobService.DeleteBlobAsync(ContainerName, fileName);

                // Return a successful response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { message = "Image deleted successfully" });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting image: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        // Code Attribution:
        // File Upload Processing: Reading multipart/form-data using MultipartReader — Microsoft Docs — https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-8.0
        private async Task<FileData> ReadFileFromRequest(HttpRequestData req)
        {
            var fileData = new FileData();

            // Check for Content-Type header
            if (!req.Headers.TryGetValues("Content-Type", out IEnumerable<string>? contentTypes) || !contentTypes.Any())
            {
                return fileData; // No Content-Type header
            }

            var contentType = contentTypes.First();
            // Verify that the request is a multipart file upload
            if (!contentType.Contains("multipart/form-data"))
            {
                return fileData; // Not a file upload
            }

            // Extract the boundary from the Content-Type header to correctly parse the body
            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary).Value;
            var reader = new MultipartReader(boundary, req.Body);

            // Read the first section (the file part is typically the first section)
            var section = await reader.ReadNextSectionAsync();
            if (section == null)
            {
                return fileData; // No sections found
            }

            // Attempt to parse the Content-Disposition header to get the file name
            if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition) && contentDisposition.DispositionType.Equals("form-data") && !string.IsNullOrEmpty(contentDisposition.FileName.Value))
            {
                fileData.FileName = contentDisposition.FileName.Value;
                fileData.ContentType = section.ContentType;

                // Copy the section body stream into a MemoryStream to make it rewindable/reusable
                var ms = new MemoryStream();
                await section.Body.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin); // Reset stream position to the start

                fileData.Stream = ms;
                fileData.Length = ms.Length;
            }
            return fileData;
        }
    }
}