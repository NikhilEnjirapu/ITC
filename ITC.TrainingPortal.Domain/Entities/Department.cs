namespace ITC.TrainingPortal.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
