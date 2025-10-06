using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ABCFunc.Services
{
    // Code Attribution:
    // Azure File Share Client: Using the Azure.Storage.Files.Shares package for .NET — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/files/storage-files-dotnet-how-to-use-files
    public class FileService
    {
        private readonly ShareServiceClient _shareServiceClient;

        // Constructor Injection: Receives the ShareServiceClient instance from the Dependency Injection container
        public FileService(ShareServiceClient shareServiceClient)
        {
            _shareServiceClient = shareServiceClient;
        }

        // Retrieves the names of all files (excluding directories) within the root of a share
        public async Task<List<string>> GetAllFilesAsync(string shareName)
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            // Ensures the share exists, creating it if necessary
            await shareClient.CreateIfNotExistsAsync();

            var files = new List<string>();
            var rootDirectory = shareClient.GetRootDirectoryClient();

            // Code Attribution:
            // Listing Files: Iterating through files and directories in a share — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/files/storage-files-dotnet-how-to-use-files#list-directories-and-files-in-a-share
            await foreach (ShareFileItem fileItem in rootDirectory.GetFilesAndDirectoriesAsync())
            {
                if (!fileItem.IsDirectory)
                {
                    files.Add(fileItem.Name);
                }
            }
            return files;
        }

        // Uploads a stream of content to a specified file in a share
        public async Task UploadFileAsync(string shareName, string fileName, Stream content, long fileSize)
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            await shareClient.CreateIfNotExistsAsync();

            var rootDirectory = shareClient.GetRootDirectoryClient();
            var shareFileClient = rootDirectory.GetFileClient(fileName);

            // Code Attribution:
            // Uploading a File: Must first create the file with its size, then upload the range — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/files/storage-files-dotnet-how-to-use-files#upload-a-file
            // 1. Create the file on the share with the desired file size
            await shareFileClient.CreateAsync(fileSize);

            // 2. Upload the content range to the newly created file
            await shareFileClient.UploadRangeAsync(new HttpRange(0, fileSize), content);
        }

        // Downloads the content and metadata of a specified file
        public async Task<ShareFileDownloadInfo> DownloadFileAsync(string shareName, string fileName)
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            var rootDirectory = shareClient.GetRootDirectoryClient();
            var shareFileClient = rootDirectory.GetFileClient(fileName);

            // Code Attribution:
            // Downloading a File: DownloadAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/files/storage-files-dotnet-how-to-use-files#download-a-file
            return await shareFileClient.DownloadAsync();
        }

        // Deletes a specified file if it exists
        public async Task DeleteFileAsync(string shareName, string fileName)
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            var rootDirectory = shareClient.GetRootDirectoryClient();
            var shareFileClient = rootDirectory.GetFileClient(fileName);

            // Code Attribution:
            // Deleting a File: DeleteIfExistsAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/files/storage-files-dotnet-how-to-use-files#delete-a-file
            await shareFileClient.DeleteIfExistsAsync();
        }

        // Checks if a specified file exists in the share
        public async Task<bool> FileExistsAsync(string shareName, string fileName)
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            var rootDirectory = shareClient.GetRootDirectoryClient();
            var shareFileClient = rootDirectory.GetFileClient(fileName);

            // Code Attribution:
            // Checking Existence: ExistsAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/dotnet/api/azure.storage.files.shares.sharefileclient.existsasync
            return await shareFileClient.ExistsAsync();
        }
    }
}