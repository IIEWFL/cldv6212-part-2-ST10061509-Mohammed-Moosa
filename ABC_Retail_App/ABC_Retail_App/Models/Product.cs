using Azure;
using Azure.Data.Tables;

namespace ABC_Retail_App.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } // e.g., "Products" or ProductCategoryID
        public string RowKey { get; set; }     // e.g., ProductId (unique identifier)
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }

        // ITableEntity required properties
        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }
    }
}