using Blazor_WebAssembly.Models.Equipment;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Blazor_WebAssembly.Services.Extensions
{
    public static class EquipmentExtensions
    {
        // Use ConditionalWeakTable to store return quantities without modifying the original class
        private static readonly ConditionalWeakTable<EquipmentModel, ReturnQuantityHolder> _returnQuantities =
            new ConditionalWeakTable<EquipmentModel, ReturnQuantityHolder>();

        // Private class to hold the return quantity
        private class ReturnQuantityHolder
        {
            public int Quantity { get; set; }
        }

        /// <summary>
        /// Sets the return quantity for an equipment item
        /// </summary>
        public static void SetReturnQuantity(this EquipmentModel equipment, int quantity)
        {
            if (equipment == null)
                throw new ArgumentNullException(nameof(equipment));

            var holder = _returnQuantities.GetOrCreateValue(equipment);
            holder.Quantity = Math.Max(0, Math.Min(quantity, equipment.Quantity));
        }

        /// <summary>
        /// Gets the return quantity for an equipment item
        /// </summary>
        public static int GetReturnQuantity(this EquipmentModel equipment)
        {
            if (equipment == null)
                throw new ArgumentNullException(nameof(equipment));

            if (_returnQuantities.TryGetValue(equipment, out var holder))
            {
                return holder.Quantity;
            }

            // Default to the full quantity if not set
            return equipment.Quantity;
        }

        /// <summary>
        /// Initialize return quantities for a list of equipment
        /// </summary>
        public static void InitializeReturnQuantities(this List<EquipmentModel> equipment)
        {
            if (equipment == null)
                return;

            foreach (var item in equipment)
            {
                // Set return quantity to the full available quantity by default
                item.SetReturnQuantity(item.Quantity);
            }
        }
    }
}
