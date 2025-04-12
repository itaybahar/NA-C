using System.Net.Http.Json;
using Blazor_WebAssembly.Models.Checkout;
using Blazor_WebAssembly.Services.Interfaces;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class CheckoutService : ICheckoutService
    {
        private readonly HttpClient _httpClient;

        public CheckoutService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://localhost:7235/api/");
        }

        public async Task<List<EquipmentCheckoutModel>> GetAllCheckoutsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentCheckoutModel>>("checkout") ?? new List<EquipmentCheckoutModel>();
            }
            catch (Exception)
            {
                // Log the exception if needed
                return new List<EquipmentCheckoutModel>();
            }
        }

        public async Task<EquipmentCheckoutModel> GetCheckoutByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<EquipmentCheckoutModel>($"checkout/{id}") ?? new EquipmentCheckoutModel();
            }
            catch (Exception)
            {
                // Log the exception if needed
                return new EquipmentCheckoutModel(); // Return a default object instead of null
            }
        }

        public async Task<bool> CreateCheckoutAsync(EquipmentCheckoutModel checkout)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("checkout", checkout);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Log the exception if needed
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
            catch (Exception)
            {
                // Log the exception if needed
                return false;
            }
        }

        public async Task<List<EquipmentCheckoutModel>> GetActiveCheckoutsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentCheckoutModel>>("checkout/active") ?? new List<EquipmentCheckoutModel>();
            }
            catch (Exception)
            {
                // Log the exception if needed
                return new List<EquipmentCheckoutModel>();
            }
        }

        public async Task<List<EquipmentCheckoutModel>> GetOverdueCheckoutsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentCheckoutModel>>("checkout/overdue") ?? new List<EquipmentCheckoutModel>();
            }
            catch (Exception)
            {
                // Log the exception if needed
                return new List<EquipmentCheckoutModel>();
            }
        }

        public Task CheckoutEquipmentAsync(int teamId, int equipmentId)
        {
            throw new NotImplementedException();
        }
    }
}
