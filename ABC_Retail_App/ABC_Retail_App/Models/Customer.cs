using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations; 

namespace ABC_Retail_App.Models 
{
    public class Customer : ITableEntity
    {
        [Required(ErrorMessage = "Partition Key is required.")]
        public string PartitionKey { get; set; }

        [Required(ErrorMessage = "Row Key is required.")]
        public string RowKey { get; set; }

        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid Phone Number.")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        public string Phone { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }
    }
}
