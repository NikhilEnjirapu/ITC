using System.ComponentModel.DataAnnotations;

namespace ITC.TrainingPortal.Application.DTOs;

public sealed class TrainingVideoDto
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [StringLength(2000)] public string? Description { get; set; }
    [Required, StringLength(2048), Display(Name = "Video repository filename")] public string VideoUrl { get; set; } = string.Empty;
    [Range(1, int.MaxValue), Display(Name = "Sequence Order")] public int SequenceOrder { get; set; }
    [Range(1, 86400), Display(Name = "Duration (seconds)")] public int DurationSeconds { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Training Program")] public int TrainingProgramId { get; set; }
    public string? TrainingProgramTitle { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
