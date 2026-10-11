namespace ITC.TrainingPortal.Domain.Entities;

public class TrainingProgram : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ExamUnlockCondition ExamUnlockCondition { get; set; }
    public int? ExamUnlockVideoId { get; set; }
    public TrainingVideo? ExamUnlockVideo { get; set; }
    
    public ICollection<TrainingVideo> Videos { get; set; } = new List<TrainingVideo>();
    public Quiz? Quiz { get; set; }
    public ICollection<TrainingAssignment> Assignments { get; set; } = new List<TrainingAssignment>();
}
