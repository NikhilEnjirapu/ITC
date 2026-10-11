using System.ComponentModel.DataAnnotations;

namespace ITC.TrainingPortal.Application.DTOs;

public sealed class QuizDto
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Range(0, 100)] public int PassingScorePercentage { get; set; } = 80;
    [Range(1, int.MaxValue)] public int TrainingProgramId { get; set; }
    public string? TrainingProgramTitle { get; set; }
    public string? ExamUnlockDescription { get; set; }
    public bool IsActive { get; set; } = true;
    public int QuestionCount { get; set; }
}

public sealed class QuizQuestionDto
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int QuizId { get; set; }
    [Required, StringLength(1000)] public string QuestionText { get; set; } = string.Empty;
    [Required, StringLength(500)] public string OptionA { get; set; } = string.Empty;
    [Required, StringLength(500)] public string OptionB { get; set; } = string.Empty;
    [Required, StringLength(500)] public string OptionC { get; set; } = string.Empty;
    [Required, StringLength(500)] public string OptionD { get; set; } = string.Empty;
    [Required, RegularExpression("^[ABCD]$")] public string CorrectOption { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int SequenceOrder { get; set; }
}

public sealed class ExamQuestionDto
{
    public int Id { get; set; }
    public int SequenceOrder { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
}

public class ExamAccessDto
{
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int TrainingProgramId { get; set; }
    public string TrainingProgramTitle { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; }
    public string UnlockMessage { get; set; } = "Complete all training videos to unlock this exam.";
    public bool IsActive { get; set; }
    public int QuestionCount { get; set; }
    public int? ActiveAttemptId { get; set; }
    public IReadOnlyList<QuizResultDto> Results { get; set; } = [];
}

public sealed class ExamAttemptDto : ExamAccessDto
{
    public int AttemptId { get; set; }
    public IReadOnlyList<ExamQuestionDto> Questions { get; set; } = [];
}

public sealed class ExamSubmissionDto
{
    public int AttemptId { get; set; }
    public IDictionary<int, string> Answers { get; set; } = new Dictionary<int, string>();
}

public sealed class QuizResultDto
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string TrainingProgramTitle { get; set; } = string.Empty;
    public int ScorePercentage { get; set; }
    public bool IsPassed { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime AttemptDate { get; set; }
    public DateTime? CompletedAt { get; set; }
}
