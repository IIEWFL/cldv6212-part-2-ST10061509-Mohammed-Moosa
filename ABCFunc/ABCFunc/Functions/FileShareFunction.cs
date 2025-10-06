using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCFunc.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace ABCFunc.Functions
{
    // Code Attribution:
    // Azure Functions Dependency Injection: Using dependency injection in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#dependency-injection
    public class FileShareFunction
    {
        private readonly ILogger _logger;
        private readonly FileService _fileService;
        private const string ShareName = "contracts";

        // Constructor Injection: The host provides the required services (Logger and FileService)
        public FileShareFunction(ILoggerFactory loggerFactory, FileService fileService)
        {
            _logger = loggerFactory.CreateLogger<FileShareFunction>();
            _fileService = fileService; // Inject FileService for interacting with Azure File Share
        }

        [Function("UploadContract")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> UploadFile(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("UploadContract function processing request");

            try
            {
                // Read the entire HTTP request body stream into a MemoryStream for multiple access
                using var memoryStream = new MemoryStream();
                await req.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Note: The use of System.Web.HttpUtility.ParseQueryString assumes a reference to System.Web is available.
                // Attempt to get filename from the query string
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var fileName = query["fileName"];

                if (string.IsNullOrEmpty(fileName))
                {
                    // If not in query string, attempt to extract filename from Content-Disposition header
                    if (req.Headers.TryGetValues("Content-Disposition", out var values))
                    {
                        var contentDisposition = values.FirstOrDefault();
                        if (!string.IsNullOrEmpty(contentDisposition))
                        {
                            // Use Regex to robustly parse the filename from the header
                            var fileNameMatch = System.Text.RegularExpressions.Regex.Match(
                                contentDisposition, @"filename=""?([^""]+)""?");
                            if (fileNameMatch.Success)
                            {
                                fileName = fileNameMatch.Groups[1].Value;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("fileName query parameter is required");
                    return badResponse;
                }

                if (memoryStream.Length == 0)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("No file content provided");
                    return badResponse;
                }

                // Upload the file using the injected FileService
                memoryStream.Position = 0; // Ensure stream is at the beginning before reading for upload
                await _fileService.UploadFileAsync(ShareName, fileName, memoryStream, memoryStream.Length);

                _logger.LogInformation($"File '{fileName}' uploaded successfully");

                // Return a successful response with file metadata
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "File uploaded successfully",
                    fileName,
                    size = memoryStream.Length
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading file: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetContracts")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> GetFiles(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("GetContracts function processing request");

            try
            {
                // Use the injected service to retrieve a list of all files in the share
                var files = await _fileService.GetAllFilesAsync(ShareName);

                // Return the list of files as a JSON response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(files);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting files: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("DownloadContract")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> DownloadFile(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("DownloadContract function processing request");

            try
            {
                // Note: The use of System.Web.HttpUtility.ParseQueryString assumes a reference to System.Web is available.
                // Parse the query string to extract the 'fileName' parameter
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var fileName = query["fileName"];

                if (string.IsNullOrEmpty(fileName))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("fileName query parameter is required");
                    return badResponse;
                }

                // Use the injected service to download the file content
                var fileDownload = await _fileService.DownloadFileAsync(ShareName, fileName);

                // Create the response with the downloaded content stream
                var response = req.CreateResponse(HttpStatusCode.OK);

                // Set appropriate headers for file download
                response.Headers.Add("Content-Type", fileDownload.ContentType ?? "application/octet-stream");
                response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");

                // Copy the downloaded content stream directly to the response body stream
                await fileDownload.Content.CopyToAsync(response.Body);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading file: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("DeleteContract")]
        // Code Attribution:
        // Azure Functions HTTP Trigger: HttpTrigger attribute in .NET isolated worker process — Microsoft Docs — https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#httptrigger
        public async Task<HttpResponseData> DeleteFile(
            [HttpTrigger(AuthorizationLevel.Function, "delete")] HttpRequestData req)
        {
            _logger.LogInformation("DeleteContract function processing request");

            try
            {
                // Note: The use of System.Web.HttpUtility.ParseQueryString assumes a reference to System.Web is available.
                // Parse the query string to extract the 'fileName' parameter
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var fileName = query["fileName"];

                if (string.IsNullOrEmpty(fileName))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("fileName query parameter is required");
                    return badResponse;
                }

                // Use the injected service to delete the specified file
                await _fileService.DeleteFileAsync(ShareName, fileName);

                // Return a successful response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { message = "File deleted successfully" });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting file: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}