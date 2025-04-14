using Blazor_WebAssembly.Models.Checkout;
using Domain_Project.DTOs;
using Domain_Project.DTOs.Domain_Project.DTOs.Domain_Project.Models;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface ICheckoutService
    {
        /// <summary>
        /// Checks out equipment for a specific team.
        /// </summary>
        /// <param name="teamId">The ID of the team.</param>
        /// <param name="equipmentId">The ID of the equipment.</param>
        Task CheckoutEquipmentAsync(int teamId, int equipmentId);

        /// <summary>
        /// Retrieves all equipment checkouts.
        /// </summary>
        /// <returns>A list of all equipment checkouts.</returns>
        Task<List<EquipmentCheckoutModel>> GetAllCheckoutsAsync();

        /// <summary>
        /// Retrieves a specific checkout by its ID.
        /// </summary>
        /// <param name="id">The ID of the checkout.</param>
        /// <returns>The checkout details.</returns>
        Task<EquipmentCheckoutModel> GetCheckoutByIdAsync(int id);

        /// <summary>
        /// Creates a new equipment checkout.
        /// </summary>
        /// <param name="checkout">The checkout details.</param>
        /// <returns>True if the checkout was created successfully, otherwise false.</returns>
        Task<bool> CreateCheckoutAsync(EquipmentCheckoutModel checkout);

        /// <summary>
        /// Marks equipment as returned for a specific checkout.
        /// </summary>
        /// <param name="checkoutId">The ID of the checkout.</param>
        /// <returns>True if the equipment was returned successfully, otherwise false.</returns>
        Task<bool> ReturnEquipmentAsync(int checkoutId);

        /// <summary>
        /// Retrieves all active equipment checkouts.
        /// </summary>
        /// <returns>A list of active equipment checkouts.</returns>
        Task<List<EquipmentCheckoutModel>> GetActiveCheckoutsAsync();

        /// <summary>
        /// Retrieves all overdue equipment checkouts.
        /// </summary>
        /// <returns>A list of overdue equipment checkouts.</returns>
        Task<List<EquipmentCheckoutModel>> GetOverdueCheckoutsAsync();

        /// <summary>
        /// Retrieves the history of equipment checkouts and returns.
        /// </summary>
        /// <returns>A list of checkout history records.</returns>
        Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync();
    }
}
