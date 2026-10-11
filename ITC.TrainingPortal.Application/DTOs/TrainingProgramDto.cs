using System.ComponentModel.DataAnnotations;

namespace ITC.TrainingPortal.Application.DTOs;

public sealed class TrainingProgramDto
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [StringLength(2000)] public string? Description { get; set; }
    public string? RelatedQuizTitle { get; set; }
    public byte[] RowVersion { get; set; } = [];
    [EnumDataType(typeof(ITC.TrainingPortal.Domain.Entities.ExamUnlockCondition))]
    public ITC.TrainingPortal.Domain.Entities.ExamUnlockCondition ExamUnlockCondition { get; set; }
    public int? ExamUnlockVideoId { get; set; }
}
