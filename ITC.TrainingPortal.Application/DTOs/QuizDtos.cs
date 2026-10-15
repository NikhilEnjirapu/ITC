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

public sealed class QuizQuestionDto : IValidatableObject
{
    public bool IsTypedAnswer { get; set; }
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int QuizId { get; set; }
    [Required, StringLength(1000)] public string QuestionText { get; set; } = string.Empty;
    [StringLength(500)] public string? OptionA { get; set; } = string.Empty;
    [StringLength(500)] public string? OptionB { get; set; } = string.Empty;
    [StringLength(500)] public string? OptionC { get; set; } = string.Empty;
    [StringLength(500)] public string? OptionD { get; set; } = string.Empty;
    public string? CorrectOption { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int SequenceOrder { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!IsTypedAnswer)
        {
            if (new[] { OptionA, OptionB, OptionC, OptionD }.Any(string.IsNullOrWhiteSpace))
                yield return new ValidationResult("Enter all four options for a multiple-choice question.");
            if (CorrectOption is not ("A" or "B" or "C" or "D"))
                yield return new ValidationResult("Select the correct option.", new[] { nameof(CorrectOption) });
        }
    }

}

public sealed class ExamQuestionDto
{
    public bool IsTypedAnswer { get; set; }
    public int Id { get; set; }
    public int SequenceOrder { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
}

public class ExamAccessDto
{
    public const int MaximumAttempts = 3;
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
    public int AttemptCount => Results.Count;
    public int RemainingAttempts => Math.Max(0, MaximumAttempts - AttemptCount);
    public bool HasAttemptsRemaining => RemainingAttempts > 0;
    public QuizResultDto? BestResult => Results.Where(x => !x.RequiresReview)
        .OrderByDescending(x => x.ScorePercentage).ThenByDescending(x => x.CompletedAt ?? x.AttemptDate).FirstOrDefault();
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
    public bool RequiresReview { get; set; }
    public string Outcome => RequiresReview ? "Awaiting review" : $"{ScorePercentage}% - {(IsPassed ? "Passed" : "Failed")}";
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
