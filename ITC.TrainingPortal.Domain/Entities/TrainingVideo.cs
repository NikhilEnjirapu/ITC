namespace ITC.TrainingPortal.Domain.Entities;

public class TrainingVideo : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public int DurationSeconds { get; set; }
    
    public int TrainingProgramId { get; set; }
    public TrainingProgram TrainingProgram { get; set; } = null!;
    public byte[] RowVersion { get; set; } = [];

    public ICollection<VideoProgress> Progresses { get; set; } = new List<VideoProgress>();
}
