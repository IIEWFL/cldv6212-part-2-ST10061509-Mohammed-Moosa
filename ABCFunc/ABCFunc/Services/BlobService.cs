using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ABCFunc.Services
{
    // Code Attribution:
    // Azure Blob Storage Client: Using the Azure.Storage.Blobs package for .NET — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet
    public class BlobService
    {
        private readonly BlobServiceClient _blobServiceClient;

        // Constructor Injection: Receives the BlobServiceClient instance from the Dependency Injection container
        public BlobService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        // Retrieves the names of all blobs within a specified container
        public async Task<List<string>> GetAllBlobsAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Ensures the container exists, creating it if necessary, with public read access to blobs
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobs = new List<string>();
            // Code Attribution:
            // Listing Blobs: Iterating through blobs in a container — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet#list-the-blobs-in-a-container
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                blobs.Add(blobItem.Name);
            }
            return blobs;
        }

        // Uploads a stream of content to a specified blob in a container
        public async Task UploadBlobAsync(string containerName, string blobName, Stream content, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(blobName);

            // Configure headers, specifically the ContentType (MIME type)
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

            // Configure upload options, including transfer settings (e.g., block size)
            var blobUploadOptions = new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders,
                // Code Attribution:
                // Blob Upload Options: Specifying upload options like ContentType and transfer settings — Microsoft Docs — https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.models.blobuploadoptions
                TransferOptions = new Azure.Storage.StorageTransferOptions
                {
                    // Set initial transfer size for optimization (1 MB)
                    InitialTransferLength = 1024 * 1024
                }
            };

            // Code Attribution:
            // Uploading a Blob: UploadAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet#upload-blobs-to-a-container
            await blobClient.UploadAsync(content, blobUploadOptions);
        }

        // Downloads the content and metadata of a specified blob
        public async Task<BlobDownloadInfo> DownloadBlobAsync(string containerName, string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Code Attribution:
            // Downloading a Blob: DownloadAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet#download-blobs
            return await blobClient.DownloadAsync();
        }

        // Deletes a specified blob if it exists
        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Code Attribution:
            // Deleting a Blob: DeleteIfExistsAsync method — Microsoft Docs — https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet#delete-a-blob
            await blobClient.DeleteIfExistsAsync();
        }

        // Generates the public URI for a specified blob
        public string GetBlobUrl(string containerName, string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Code Attribution:
            // Getting Blob URI: Accessing the Uri property of BlobClient — Microsoft Docs — https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobclient.uri
            return blobClient.Uri.ToString();
        }
    }
}