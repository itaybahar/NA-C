using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Domain_Project.Models;
using API_Project.Services;

namespace API_Project.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseApiPath = "api/checkout";

        public CheckoutService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Don't set the BaseAddress here since it's already configured in Program.cs
        }

        // Implementation for the internal domain model
        public async Task<List<EquipmentCheckout>> GetAllCheckoutsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentCheckout>>(_baseApiPath) ?? new List<EquipmentCheckout>();
            }
            catch (Exception)
            {
                // Log the exception if needed
                return new List<EquipmentCheckout>();
            }
        }

        public async Task<EquipmentCheckout> GetCheckoutByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<EquipmentCheckout>($"{_baseApiPath}/{id}") ??
                    new EquipmentCheckout { Status = "Unknown" };
            }
            catch (Exception)
            {
                // Log the exception if needed
                return new EquipmentCheckout { Status = "Unknown" };
            }
        }

        public async Task<bool> CreateCheckoutAsync(EquipmentCheckout checkout)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_baseApiPath, checkout);
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
                var response = await _httpClient.PostAsync($"{_baseApiPath}/{checkoutId}/return", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Log the exception if needed
                return false;
            }
        }

        public async Task<List<EquipmentCheckout>> GetActiveCheckoutsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentCheckout>>($"{_baseApiPath}/active") ?? new List<EquipmentCheckout>();
            }
            catch (Exception)
            {
                // Log the exception if needed
                return new List<EquipmentCheckout>();
            }
        }

        public async Task<List<EquipmentCheckout>> GetOverdueCheckoutsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentCheckout>>($"{_baseApiPath}/overdue") ?? new List<EquipmentCheckout>();
            }
            catch (Exception)
            {
                // Log the exception if needed
                return new List<EquipmentCheckout>();
            }
        }

        public async Task<bool> CheckoutEquipmentAsync(int teamId, int equipmentId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseApiPath}/team/{teamId}/equipment/{equipmentId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Log the exception if needed
                return false;
            }
        }

        public async Task<List<CheckoutRecord>> GetUnreturnedByTeamAsync(string teamId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<CheckoutRecord>>($"{_baseApiPath}/team/{teamId}/unreturned") ?? new List<CheckoutRecord>();
            }
            catch (Exception)
            {
                // Log the exception if needed
                return new List<CheckoutRecord>();
            }
        }

        // Explicit interface implementations for the first ICheckoutService interface
        async Task ICheckoutService.CheckoutItemAsync(string teamId, string equipmentId)
        {
            try
            {
                await _httpClient.PostAsync($"{_baseApiPath}/team/{teamId}/equipment/{equipmentId}", null);
            }
            catch (Exception)
            {
                // Log the exception if needed
                throw;
            }
        }

        async Task ICheckoutService.AutoBlacklistOverdueAsync()
        {
            try
            {
                await _httpClient.PostAsync($"{_baseApiPath}/auto-blacklist-overdue", null);
            }
            catch (Exception)
            {
                // Log the exception if needed
                throw;
            }
        }

        // Helper methods for interface compatibility

        // Explicit interface implementations for the second ICheckoutService interface
        Task<List<EquipmentCheckout>> ICheckoutService.GetAllCheckoutsAsync()
        {
            return ConvertToModelList(GetAllCheckoutsAsync());
        }

        async Task<EquipmentCheckout> ICheckoutService.GetCheckoutByIdAsync(int id)
        {
            var result = await GetCheckoutByIdAsync(id);
            return ConvertToModel(result);
        }

        async Task<bool> ICheckoutService.CreateCheckoutAsync(EquipmentCheckout checkout)
        {
            return await CreateCheckoutAsync(ConvertFromModel(checkout));
        }

        Task<List<EquipmentCheckout>> ICheckoutService.GetActiveCheckoutsAsync()
        {
            return ConvertToModelList(GetActiveCheckoutsAsync());
        }

        Task<List<EquipmentCheckout>> ICheckoutService.GetOverdueCheckoutsAsync()
        {
            return ConvertToModelList(GetOverdueCheckoutsAsync());
        }

        // Utility methods to convert between types
        private async Task<List<EquipmentCheckout>> ConvertToModelList(Task<List<EquipmentCheckout>> checkoutsTask)
        {
            var checkouts = await checkoutsTask;
            return checkouts.Select(c => ConvertToModel(c)).ToList();
        }

        private EquipmentCheckout ConvertToModel(EquipmentCheckout checkout)
        {
            return new EquipmentCheckout
            {
                CheckoutID = checkout.CheckoutID,
                EquipmentID = checkout.EquipmentID,
                TeamID = checkout.TeamID,
                CheckedOutBy = checkout.CheckedOutBy,
                IssuedBy = checkout.IssuedBy,
                CheckoutDate = checkout.CheckoutDate,
                ExpectedReturnDate = checkout.ExpectedReturnDate,
                ActualReturnDate = checkout.ActualReturnDate,
                Status = checkout.Status
            };
        }              

        private EquipmentCheckout ConvertFromModel(EquipmentCheckout model)
        {
            return new EquipmentCheckout
            {
                CheckoutID = model.CheckoutID,
                EquipmentID = model.EquipmentID,
                TeamID = model.TeamID,
                CheckedOutBy = model.CheckedOutBy,
                IssuedBy = model.IssuedBy,
                CheckoutDate = model.CheckoutDate,
                ExpectedReturnDate = model.ExpectedReturnDate,
                ActualReturnDate = model.ActualReturnDate,
                Status = model.Status ?? "Unknown"
            };
        }
    }
}
