namespace ITC.TrainingPortal.Application.DTOs;

public sealed class EmployeeTrainingDto
{
    public int AssignmentId { get; init; }
    public int TrainingProgramId { get; init; }
    public string TrainingProgramTitle { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime AssignedAt { get; init; }
    public DateTime? DueDate { get; init; }
    public int VideoCount { get; init; }
    public int CompletedVideoCount { get; init; }
    public int ProgressPercentage => VideoCount == 0 ? 0 : (int)Math.Round(CompletedVideoCount * 100d / VideoCount);
    public string TrainingStatus => VideoCount > 0 && CompletedVideoCount == VideoCount
        ? "Complete"
        : CompletedVideoCount > 0 ? "In Progress" : "Assigned";
    public string ExamStatus { get; init; } = "No exam configured";
    public int? CurrentVideoId { get; init; }
    public string? CurrentVideoTitle { get; init; }
}
