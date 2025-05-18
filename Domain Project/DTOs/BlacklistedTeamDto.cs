using System;
using System.Collections.Generic;

namespace Domain_Project.DTOs
{
    public class BlacklistedTeamDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string BlacklistReason { get; set; } = string.Empty;
        public DateTime BlacklistDate { get; set; }
        public List<OverdueItemDto> OverdueItems { get; set; } = new();
    }

    public class OverdueItemDto
    {
        public int EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public int CheckoutId { get; set; }
        public DateTime CheckoutDate { get; set; }
        public DateTime ExpectedReturnDate { get; set; }
        public double DaysOverdue { get; set; }
    }
}