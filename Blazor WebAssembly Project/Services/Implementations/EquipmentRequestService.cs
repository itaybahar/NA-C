using Blazor_WebAssembly.Models.Equipment;
using Blazor_WebAssembly.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class EquipmentRequestService : IEquipmentRequestService
    {
        private readonly HttpClient _httpClient;
        private readonly ICheckoutService _checkoutService;
        private readonly IEquipmentService _equipmentService;

        public EquipmentRequestService(HttpClient httpClient, ICheckoutService checkoutService, IEquipmentService equipmentService)
        {
            _httpClient = httpClient;
            _checkoutService = checkoutService;
            _equipmentService = equipmentService;
        }

        public async Task<List<EquipmentRequestModel>> GetPendingRequestsAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<EquipmentRequestModel>>("equipment-requests/pending");
                return result ?? new List<EquipmentRequestModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPendingRequestsAsync: {ex.Message}");
                return new List<EquipmentRequestModel>();
            }
        }

        public async Task<bool> CreateEquipmentRequestAsync(EquipmentRequestModel request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("equipment-requests", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateEquipmentRequestAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ApproveRequestAsync(int requestId)
        {
            try
            {
                var response = await _httpClient.PatchAsync($"equipment-requests/{requestId}/approve", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ApproveRequestAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RejectRequestAsync(int requestId, string reason)
        {
            try
            {
                var content = new StringContent(reason, Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"equipment-requests/{requestId}/reject", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RejectRequestAsync: {ex.Message}");
                return false;
            }
        }

        public async Task SendEquipmentRequestAsync(string message)
        {
            try
            {
                var content = new StringContent(message, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync("equipment-requests/send", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendEquipmentRequestAsync: {ex.Message}");
            }
        }

        public async Task<List<EquipmentModel>> GetCheckedOutEquipmentByTeamAsync(int teamId)
        {
            try
            {
                // First try API route if available
                try
                {
                    var result = await _httpClient.GetFromJsonAsync<List<EquipmentModel>>($"api/teams/{teamId}/checked-out-equipment");
                    if (result != null && result.Count > 0)
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"API method failed, falling back to direct method: {ex.Message}");
                }

                // Fallback to direct method
                return await GetTeamCheckedOutEquipmentDirectly(teamId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCheckedOutEquipmentByTeamAsync: {ex.Message}");
                return new List<EquipmentModel>();
            }
        }

        private async Task<List<EquipmentModel>> GetTeamCheckedOutEquipmentDirectly(int teamId)
        {
            try
            {
                Console.WriteLine($"Directly fetching checked-out equipment for team {teamId}");
                var activeCheckouts = await _checkoutService.GetActiveCheckoutsAsync();

                if (activeCheckouts == null || !activeCheckouts.Any())
                {
                    Console.WriteLine("No active checkouts found in the system");
                    return new List<EquipmentModel>();
                }

                var teamCheckouts = activeCheckouts
                    .Where(c => c.TeamID == teamId)
                    .ToList();

                Console.WriteLine($"Found {teamCheckouts.Count} checkouts for team {teamId}");

                if (!teamCheckouts.Any())
                    return new List<EquipmentModel>();

                var result = new List<EquipmentModel>();

                foreach (var checkout in teamCheckouts)
                {
                    var equipment = await _equipmentService.GetEquipmentByIdAsync(checkout.EquipmentId);
                    if (equipment != null)
                    {
                        // Adjust the quantity to match what was checked out
                        equipment.Quantity = checkout.Quantity;
                        result.Add(equipment);
                        Console.WriteLine($"Added equipment {equipment.Name} with quantity {equipment.Quantity}");
                    }
                    else
                    {
                        Console.WriteLine($"Could not find equipment with ID {checkout.EquipmentId}");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTeamCheckedOutEquipmentDirectly: {ex.Message}");
                return new List<EquipmentModel>();
            }
        }
    }
}
