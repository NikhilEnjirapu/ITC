using System.ComponentModel.DataAnnotations;

namespace ITC.TrainingPortal.Application.DTOs;

public sealed class TrainingAssignmentDto : IValidatableObject
{
    public int Id { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Employee")] public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? DepartmentName { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Training Program")] public int TrainingProgramId { get; set; }
    public string? TrainingProgramTitle { get; set; }
    [Display(Name = "Assigned At")] public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    [Display(Name = "Due Date")] public DateTime? DueDate { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DueDate.HasValue && DueDate.Value < AssignedAt)
            yield return new ValidationResult("Due date cannot be earlier than the assignment date.", [nameof(DueDate)]);
    }
}
