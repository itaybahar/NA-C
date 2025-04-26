// Add this to Blazor WebAssembly Project/Models/Equipment/EquipmentApiDto.cs
using System.Text.Json.Serialization;

namespace Blazor_WebAssembly.Models.Equipment
{
    public class EquipmentApiDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("serialNumber")]
        public string? SerialNumber { get; set; }

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("storageLocation")]
        public string StorageLocation { get; set; } = string.Empty;

        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }

        [JsonPropertyName("modelNumber")]
        public string? ModelNumber { get; set; }

        // No checkoutRecords property since it's not in the API response
    }
}
