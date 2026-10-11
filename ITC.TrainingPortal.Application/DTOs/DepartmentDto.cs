using System.ComponentModel.DataAnnotations;

namespace ITC.TrainingPortal.Application.DTOs;

public class DepartmentDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
