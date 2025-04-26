using Domain_Project.DTOs;

namespace API_Project.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseApiPath = "api/equipmentcheckout"; // Updated to match the controller name

        public string BaseApiPath { get; private set; }

        public CheckoutService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Initialize the BaseApiPath property
            BaseApiPath = _baseApiPath;
            // Don't set the BaseAddress here since it's already configured in Program.cs
        }

        public async Task<List<EquipmentCheckout>> GetAllCheckoutsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentCheckout>>(_baseApiPath) ?? new List<EquipmentCheckout>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error getting all checkouts: {ex.Message}");
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
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error getting checkout by ID {id}: {ex.Message}");
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
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating checkout: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ReturnEquipmentAsync(int checkoutId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseApiPath}/return/{checkoutId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error returning equipment (ID: {checkoutId}): {ex.Message}");
                return false;
            }
        }

        public async Task<List<EquipmentCheckout>> GetActiveCheckoutsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentCheckout>>($"{_baseApiPath}/active") ?? new List<EquipmentCheckout>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error getting active checkouts: {ex.Message}");
                return new List<EquipmentCheckout>();
            }
        }

        public async Task<List<EquipmentCheckout>> GetOverdueCheckoutsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentCheckout>>($"{_baseApiPath}/overdue") ?? new List<EquipmentCheckout>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error getting overdue checkouts: {ex.Message}");
                return new List<EquipmentCheckout>();
            }
        }

        public async Task<bool> CheckoutEquipmentAsync(int teamId, int equipmentId, int userId)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseApiPath}/checkout", new
                {
                    TeamID = teamId,
                    EquipmentID = equipmentId,
                    UserName = userId
                });
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error checking out equipment (Team: {teamId}, Equipment: {equipmentId}): {ex.Message}");
                return false;
            }
        }

        public async Task<List<CheckoutRecord>> GetUnreturnedByTeamAsync(string teamId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<CheckoutRecord>>($"{_baseApiPath}/unreturned/team/{teamId}") ?? new List<CheckoutRecord>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error getting unreturned equipment for team {teamId}: {ex.Message}");
                return new List<CheckoutRecord>();
            }
        }

        public async Task CheckoutItemAsync(string teamId, string equipmentId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseApiPath}/team/{teamId}/equipment/{equipmentId}", null);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error checking out item (Team: {teamId}, Equipment: {equipmentId}): {ex.Message}");
                throw;
            }
        }

        public async Task AutoBlacklistOverdueAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseApiPath}/auto-blacklist-overdue", null);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error auto-blacklisting overdue checkouts: {ex.Message}");
                throw;
            }
        }

        // Fix: Changed the return type to match the interface
        public async Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/checkout/history/detailed");

                if (response.IsSuccessStatusCode)
                {
                    // Options to handle potential issues with JSON serialization
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        IgnoreReadOnlyProperties = true
                    };

                    // Get and deserialize the response content
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Checkout history API response: {content}");

                    var history = await response.Content.ReadFromJsonAsync<List<CheckoutRecordDto>>(options);
                    return history ?? new List<CheckoutRecordDto>();
                }
                else
                {
                    Console.WriteLine($"Error fetching checkout history. Status code: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response: {errorContent}");
                    return new List<CheckoutRecordDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetCheckoutHistoryAsync: {ex.Message}");
                return new List<CheckoutRecordDto>();
            }
        }

        private Task HandleErrorResponse(HttpResponseMessage response, string operation)
        {
            // Log the error
            Console.Error.WriteLine($"API Error during {operation}: {response.StatusCode} - {response.ReasonPhrase}");
            return Task.CompletedTask;
        }

        private Task EnsureAuthorizationHeaderAsync()
        {
            // This would typically check for and refresh authentication tokens if needed
            // For now, we'll just return a completed task
            return Task.CompletedTask;
        }
    }
}
