using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ABCFunc.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } // e.g., "Orders"
        public string RowKey { get; set; }       // OrderId (unique identifier)

        [Required]
        public string CustomerName { get; set; }

        [Required]
        public string ProductName { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Cancelled

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedDate { get; set; }

        // ITableEntity required properties
        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }
    }
}