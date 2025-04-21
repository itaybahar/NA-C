using System.Net.Http.Json;
using Blazor_WebAssembly.Models.Checkout;
using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs.Domain_Project.DTOs.Domain_Project.Models;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class CheckoutService : ICheckoutService
    {
        private readonly HttpClient _httpClient;

        public CheckoutService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<List<EquipmentCheckoutModel>> GetAllCheckoutsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentCheckoutModel>>("checkout")
                       ?? new List<EquipmentCheckoutModel>();
            }
            catch (Exception ex)
            {
                // Log the exception (replace with actual logging)
                Console.Error.WriteLine($"Error fetching all checkouts: {ex.Message}");
                return new List<EquipmentCheckoutModel>();
            }
        }

        public async Task<EquipmentCheckoutModel> GetCheckoutByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<EquipmentCheckoutModel>($"checkout/{id}")
                       ?? new EquipmentCheckoutModel();
            }
            catch (Exception ex)
            {
                // Log the exception (replace with actual logging)
                Console.Error.WriteLine($"Error fetching checkout by ID {id}: {ex.Message}");
                return new EquipmentCheckoutModel();
            }
        }

        public async Task<bool> CreateCheckoutAsync(EquipmentCheckoutModel checkout)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("checkout", checkout);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log the exception (replace with actual logging)
                Console.Error.WriteLine($"Error creating checkout: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ReturnEquipmentAsync(int checkoutId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"checkout/{checkoutId}/return", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log the exception (replace with actual logging)
                Console.Error.WriteLine($"Error returning equipment for checkout ID {checkoutId}: {ex.Message}");
                return false;
            }
        }

        public async Task<List<EquipmentCheckoutModel>> GetActiveCheckoutsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentCheckoutModel>>("checkout/active")
                       ?? new List<EquipmentCheckoutModel>();
            }
            catch (Exception ex)
            {
                // Log the exception (replace with actual logging)
                Console.Error.WriteLine($"Error fetching active checkouts: {ex.Message}");
                return new List<EquipmentCheckoutModel>();
            }
        }

        public async Task<List<EquipmentCheckoutModel>> GetOverdueCheckoutsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentCheckoutModel>>("checkout/overdue")
                       ?? new List<EquipmentCheckoutModel>();
            }
            catch (Exception ex)
            {
                // Log the exception (replace with actual logging)
                Console.Error.WriteLine($"Error fetching overdue checkouts: {ex.Message}");
                return new List<EquipmentCheckoutModel>();
            }
        }

        public async Task CheckoutEquipmentAsync(int teamId, int equipmentId)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("checkout/equipment", new
                {
                    TeamID = teamId,
                    EquipmentID = equipmentId
                });

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to checkout equipment. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Log the exception (replace with actual logging)
                Console.Error.WriteLine($"Error checking out equipment for TeamID {teamId} and EquipmentID {equipmentId}: {ex.Message}");
                throw;
            }
        }

        public Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckoutEquipmentAsync(EquipmentCheckoutModel checkoutModel)
        {
            throw new NotImplementedException();
        }
    }
}
