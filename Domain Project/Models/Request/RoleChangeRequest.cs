using Domain_Project.Models;

public class RoleChangeRequest
{
    public int RequestID { get; set; }
    public int UserID { get; set; }
    public string CurrentRole { get; set; } = string.Empty;
    public string RequestedRole { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? AdminNotes { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public int? ProcessedByUserID { get; set; }

    // Navigation property
    public virtual User User { get; set; } = null!;
}

public enum RequestStatus
{
    Pending,
    Approved,
    Rejected
}

