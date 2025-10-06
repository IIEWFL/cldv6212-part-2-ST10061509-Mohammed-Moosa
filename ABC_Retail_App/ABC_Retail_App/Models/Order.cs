using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System;

namespace ABC_Retail_App.Models
{
    // Code Attribution:
    // Azure Table Entity Model: Implementing the ITableEntity interface — Microsoft Docs — https://learn.microsoft.com/en-us/azure/data-tables/table-storage-dotnet-how-to-use?tabs=visual-studio%2Cwebapp#define-an-entity-class
    public class Order : ITableEntity
    {
        // Azure Table Storage Key Properties
        
        // PartitionKey: Used for sharding data and determining transactional scope.
        public string PartitionKey { get; set; }
        
        // RowKey: Unique identifier for the entity within a partition. Used here as the Order ID.
        public string RowKey { get; set; }      // OrderId (unique identifier)

  
        // Business Properties
 
        // Code Attribution:
        // Data Annotations: [Required] attribute for MVC model validation — Microsoft Docs — https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
        [Required]
        public string CustomerName { get; set; }

        [Required]
        public string ProductName { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        // Order Status: Initial default value is "Pending"
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Cancelled

        // OrderDate: Default value set to the time the object is instantiated
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        // ProcessedDate: Nullable, to be set when the order status changes to Processing or Completed
        public DateTime? ProcessedDate { get; set; }


        // ETag: Used for optimistic concurrency control during updates/deletes. ETag.All (*) bypasses concurrency checks.
        public ETag ETag { get; set; } = ETag.All;
        
        // Timestamp: Automatically managed by Azure Table Storage, represents the last modified time.
        public DateTimeOffset? Timestamp { get; set; }
    }
}
