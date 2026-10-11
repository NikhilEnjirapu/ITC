namespace ITC.TrainingPortal.Domain.Entities;

public class QuizResult : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public int ScorePercentage { get; set; }
    public bool IsPassed { get; set; }
    public DateTime AttemptDate { get; set; } = DateTime.UtcNow;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? SubmittedAnswersJson { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
