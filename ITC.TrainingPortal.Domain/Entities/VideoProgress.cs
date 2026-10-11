namespace ITC.TrainingPortal.Domain.Entities;

public class VideoProgress : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int TrainingVideoId { get; set; }
    public TrainingVideo TrainingVideo { get; set; } = null!;

    public int CurrentPositionSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public double PlaybackCreditSeconds { get; set; }
    public string PlaybackState { get; set; } = "playing";
    public byte[] RowVersion { get; set; } = [];
}
