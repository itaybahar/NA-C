using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Blazor_WebAssembly.Models.Checkout;

namespace Blazor_WebAssembly.Models.Equipment
{
    public class EquipmentModel
    {
        [JsonPropertyName("id")]
        public int EquipmentID { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? SerialNumber { get; set; }
        public decimal Value { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("storageLocation")]
        public string StorageLocation { get; set; } = string.Empty;

        [JsonPropertyName("checkoutRecords")]
        public List<EquipmentCheckoutModel> CheckoutRecords { get; set; } = new();

        [JsonIgnore]
        public int AvailableQuantity => Quantity - (CheckoutRecords?.Count(r => r.ActualReturnDate == null) ?? 0);
    }
}