using Azure;
using Azure.Data.Tables;

namespace ABCFunc.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } 
        public string RowKey { get; set; }     
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }

        // ITableEntity required properties
        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }
    }
}