namespace ITC.TrainingPortal.Application.DTOs;

public sealed class TrainingContentDto
{
    public int TrainingProgramId { get; init; }
    public string TrainingProgramTitle { get; init; } = string.Empty;
    public IReadOnlyList<TrainingContentVideoDto> Videos { get; init; } = [];
}

public sealed class TrainingContentVideoDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int SequenceOrder { get; init; }
    public int DurationSeconds { get; init; }
    public int CurrentPositionSeconds { get; init; }
    public bool IsCompleted { get; init; }
    public bool IsLocked { get; init; }
}

public sealed class VideoPlaybackDto
{
    public int VideoId { get; init; }
    public int TrainingProgramId { get; init; }
    public string TrainingProgramTitle { get; init; } = string.Empty;
    public string VideoTitle { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string VideoUrl { get; init; } = string.Empty;
    public int SequenceOrder { get; init; }
    public int VideoNumber { get; init; }
    public int VideoCount { get; init; }
    public int DurationSeconds { get; init; }
    public int CurrentPositionSeconds { get; init; }
    public bool IsCompleted { get; init; }
    public int? PreviousVideoId { get; init; }
    public int? NextVideoId { get; init; }
    public bool IsNextUnlocked { get; init; }
}

public sealed class VideoHeartbeatRequest
{
    public int VideoId { get; set; }
    public double PositionSeconds { get; set; }
    public string PlaybackState { get; set; } = string.Empty;
}

public sealed class VideoHeartbeatResult
{
    public int AcceptedPositionSeconds { get; init; }
    public bool IsCompleted { get; init; }
    public int ProgressPercentage { get; init; }
}
