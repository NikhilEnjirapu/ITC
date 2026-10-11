namespace ITC.TrainingPortal.Domain.Entities;

public class QuizQuestion : BaseEntity
{
    public string QuestionText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectOption { get; set; } = string.Empty; 
    public int SequenceOrder { get; set; }

    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
}
