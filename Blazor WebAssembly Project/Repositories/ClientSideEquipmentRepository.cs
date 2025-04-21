using Domain_Project.Interfaces;
using Domain_Project.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Repositories
{
    public class ClientSideEquipmentRepository : IEquipmentRepository
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "api/equipment";

        public ClientSideEquipmentRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Equipment> AddAsync(Equipment equipment)
        {
            var response = await _httpClient.PostAsJsonAsync(BaseUrl, equipment);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Equipment>();
        }

        public async Task<Equipment> AddEquipmentAsync(Equipment equipment)
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/add", equipment);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Equipment>();
        }

        public async Task DeleteEquipmentAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Equipment>> GetAllAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Equipment>>(BaseUrl);
        }

        public async Task<IEnumerable<Equipment>> GetAllEquipmentAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Equipment>>(BaseUrl);
        }

        public async Task<IEnumerable<Equipment>> GetAvailableEquipmentAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Equipment>>($"{BaseUrl}/available");
        }

        public async Task<Equipment> GetByIdAsync(string id)
        {
            return await _httpClient.GetFromJsonAsync<Equipment>($"{BaseUrl}/{id}");
        }

        public async Task<IEnumerable<Equipment>> GetEquipmentByCategoryAsync(int categoryId)
        {
            return await _httpClient.GetFromJsonAsync<List<Equipment>>($"{BaseUrl}/category/{categoryId}");
        }

        public async Task<Equipment?> GetEquipmentByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Equipment>($"{BaseUrl}/{id}");
            }
            catch
            {
                // Return null if equipment not found
                return null;
            }
        }

        public async Task UpdateAsync(Equipment equipment)
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{equipment.Id}", equipment);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateEquipmentAsync(Equipment existingEquipment)
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{existingEquipment.Id}", existingEquipment);
            response.EnsureSuccessStatusCode();
        }
    }
}
