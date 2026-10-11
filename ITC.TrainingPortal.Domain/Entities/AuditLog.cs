namespace ITC.TrainingPortal.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string Action { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? EmployeeId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}
