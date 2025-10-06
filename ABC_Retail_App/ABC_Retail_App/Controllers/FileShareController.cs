// Code Attribution:
// 1. Upload Files in ASP.NET Core MVC — Microsoft Docs — https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
// 2. Uploading Files to Azure Blob Storage in ASP.NET Core MVC — Damien Bowden — https://damienbod.com/2020/07/08/upload-download-files-to-azure-blob-storage-with-asp-net-core/
// 3. How to create a file sharing app in ASP.NET Core MVC — C# Corner — https://www.c-sharpcorner.com/article/file-upload-and-download-in-asp-net-core-mvc/

using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ABC_Retail_App.Controllers
{
    public class FileShareController : Controller
    {
        private readonly ShareServiceClient _shareServiceClient;
        private readonly string _shareName = "contracts";

        public FileShareController(ShareServiceClient shareServiceClient)
        {
            _shareServiceClient = shareServiceClient;
            // Ensure the Azure File Share exists (create if missing)
            _shareServiceClient.GetShareClient(_shareName).CreateIfNotExistsAsync().Wait();
        }

        // Returns a client for accessing the "contracts" file share
        private ShareClient GetShareClient()
        {
            return _shareServiceClient.GetShareClient(_shareName);
        }

        // Displays a list of all files in the root directory of the file share
        public async Task<IActionResult> Index()
        {
            var shareClient = GetShareClient();
            var files = new List<string>();

            if (await shareClient.ExistsAsync())
            {
                var rootDirectory = shareClient.GetRootDirectoryClient();
                await foreach (ShareFileItem fileItem in rootDirectory.GetFilesAndDirectoriesAsync())
                {
                    if (!fileItem.IsDirectory)
                    {
                        files.Add(fileItem.Name);
                    }
                }
            }
            return View(files);
        }

        // Uploads a file from the client machine to the Azure File Share
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Index));
            }

            var shareClient = GetShareClient();
            var rootDirectory = shareClient.GetRootDirectoryClient();
            var shareFileClient = rootDirectory.GetFileClient(file.FileName);

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    await shareFileClient.CreateAsync(file.Length);
                    await shareFileClient.UploadRangeAsync(new HttpRange(0, file.Length), stream);
                }
                TempData["SuccessMessage"] = $"File '{file.FileName}' uploaded successfully to File Share.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Downloads a file from the Azure File Share
        public async Task<IActionResult> Download(string fileName)
        {
            var shareClient = GetShareClient();
            var rootDirectory = shareClient.GetRootDirectoryClient();
            var shareFileClient = rootDirectory.GetFileClient(fileName);

            try
            {
                if (await shareFileClient.ExistsAsync())
                {
                    ShareFileDownloadInfo download = await shareFileClient.DownloadAsync();
                    return File(download.Content, download.ContentType ?? "application/octet-stream", fileName);
                }
                else
                {
                    TempData["ErrorMessage"] = $"File '{fileName}' not found in File Share.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading file: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // Deletes a file from the Azure File Share
        public async Task<IActionResult> Delete(string fileName)
        {
            var shareClient = GetShareClient();
            var rootDirectory = shareClient.GetRootDirectoryClient();
            var shareFileClient = rootDirectory.GetFileClient(fileName);

            try
            {
                await shareFileClient.DeleteIfExistsAsync();
                TempData["SuccessMessage"] = $"File '{fileName}' deleted from File Share.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

