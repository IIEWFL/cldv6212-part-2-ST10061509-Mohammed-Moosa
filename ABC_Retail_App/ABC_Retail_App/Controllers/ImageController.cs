// Code Attribution:
// 1. How to Upload and Display Image in ASP.NET Core MVC — Yassine Elouafi — https://stackoverflow.com/questions/63408310/how-to-upload-and-display-image-in-asp-net-core-mvc
// 2. Uploading Files to Azure Blob Storage using ASP.NET Core — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/blobs/storage-upload-process-images
// -----------------------------------------------------------------------
// Image Controller
// Manages uploading, downloading, and deleting images using Azure Blob Storage.
// 3. Microsoft Learn - Upload a blob with .NET - https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-upload?tabs=dotnet
//    Core logic for BlobServiceClient, BlobContainerClient, BlobClient, and UploadAsync.
// 4. Microsoft Learn - Download a blob with .NET - https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-download?tabs=dotnet
//    Core logic for DownloadAsync.
// 5. Microsoft Learn - Delete a blob with .NET - https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-delete?tabs=dotnet
//    Core logic for DeleteIfExistsAsync.
// -----------------------------------------------------------------------

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using Azure; // Required for Azure.RequestFailedException

namespace ABC_Retail_App.Controllers
{
    public class ImageController : Controller
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "product-images"; // Your Azure Blob Container name

        public ImageController(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            // Ensure the container exists.
            _blobServiceClient.GetBlobContainerClient(_containerName).CreateIfNotExistsAsync(PublicAccessType.Blob).Wait();
        }

        // GET: Image
        public async Task<IActionResult> Index()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobs = new List<string>();
            try
            {
                await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                {
                    blobs.Add(blobItem.Name);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error listing images: {ex.Message}";
            }
            return View(blobs);
        }

        // POST: Image/Upload
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Index));
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(file.FileName);

            // Added BlobHttpHeaders for content type and BlobUploadOptions for timeout
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType };
            var blobUploadOptions = new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders,
                TransferOptions = new Azure.Storage.StorageTransferOptions
                {
                    InitialTransferLength = 1024 * 1024
                }
            };

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, blobUploadOptions); // New call with options
                }
                TempData["SuccessMessage"] = $"File '{file.FileName}' uploaded successfully.";
            }
            catch (RequestFailedException ex) // Catch Azure-specific errors
            {
                TempData["ErrorMessage"] = $"Azure Storage error uploading file: {ex.Message}";
                // Log the exception details for server-side diagnosis
                Console.WriteLine($"Azure Storage Error uploading {file.FileName}: {ex}");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred while uploading file: {ex.Message}";
                // Log the exception details for server-side diagnosis
                Console.WriteLine($"Unexpected Error uploading {file.FileName}: {ex}");
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Image/Download/fileName
        public async Task<IActionResult> Download(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            try
            {
                BlobDownloadInfo download = await blobClient.DownloadAsync();
                return File(download.Content, download.ContentType, fileName);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                TempData["ErrorMessage"] = $"File '{fileName}' not found.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading file: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Image/Delete/fileName
        public async Task<IActionResult> Delete(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            try
            {
                await blobClient.DeleteIfExistsAsync();
                TempData["SuccessMessage"] = $"File '{fileName}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
