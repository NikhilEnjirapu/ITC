using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Domain.Interfaces;

namespace ITC.TrainingPortal.Application.Services;

public sealed class TrainingPlaybackService : ITrainingPlaybackService
{
    private const double MaximumHeartbeatGapSeconds = 20;
    private const double TimingToleranceSeconds = 1;
    // Browser media duration and administrator-entered whole seconds can differ at the final fractional second.
    private const double EndedCompletionToleranceSeconds = 1;
    private readonly IRepository<TrainingAssignment> _assignments;
    private readonly IRepository<TrainingProgram> _programs;
    private readonly IRepository<TrainingVideo> _videos;
    private readonly IRepository<VideoProgress> _progress;
    private readonly IRepository<QuizResult> _quizResults;

    public TrainingPlaybackService(IRepository<TrainingAssignment> assignments, IRepository<TrainingProgram> programs,
        IRepository<TrainingVideo> videos, IRepository<VideoProgress> progress, IRepository<QuizResult> quizResults)
    {
        _assignments = assignments;
        _programs = programs;
        _videos = videos;
        _progress = progress;
        _quizResults = quizResults;
    }

    public async Task<TrainingContentDto?> GetAssignedContentAsync(int employeeId, int trainingProgramId)
    {
        await EnsureNoActiveAttemptAsync(employeeId);
        if (!await IsAssignedAsync(employeeId, trainingProgramId)) return null;
        var program = await _programs.GetByIdAsync(trainingProgramId);
        if (program == null) return null;
        var videos = (await _videos.FindAsync(x => x.TrainingProgramId == trainingProgramId)).OrderBy(x => x.SequenceOrder).ToList();
        var progress = (await _progress.FindAsync(x => x.EmployeeId == employeeId)).ToDictionary(x => x.TrainingVideoId);
        var priorComplete = true;
        var items = new List<TrainingContentVideoDto>();
        foreach (var video in videos)
        {
            progress.TryGetValue(video.Id, out var saved);
            items.Add(new TrainingContentVideoDto { Id = video.Id, Title = video.Title, SequenceOrder = video.SequenceOrder,
                DurationSeconds = video.DurationSeconds, CurrentPositionSeconds = saved?.CurrentPositionSeconds ?? 0,
                IsCompleted = saved?.IsCompleted ?? false, IsLocked = !priorComplete });
            priorComplete &= saved?.IsCompleted == true;
        }
        return new TrainingContentDto { TrainingProgramId = program.Id, TrainingProgramTitle = program.Title, Videos = items };
    }

    public async Task<VideoPlaybackDto?> OpenVideoAsync(int employeeId, int videoId)
    {
        await EnsureNoActiveAttemptAsync(employeeId);
        var video = await _videos.GetByIdAsync(videoId);
        if (video == null || !await IsAssignedAsync(employeeId, video.TrainingProgramId)) return null;
        var ordered = (await _videos.FindAsync(x => x.TrainingProgramId == video.TrainingProgramId)).OrderBy(x => x.SequenceOrder).ToList();
        var index = ordered.FindIndex(x => x.Id == videoId);
        if (index < 0) return null;
        var completed = (await _progress.FindAsync(x => x.EmployeeId == employeeId && x.IsCompleted)).Select(x => x.TrainingVideoId).ToHashSet();
        if (ordered.Take(index).Any(x => !completed.Contains(x.Id))) throw new InvalidOperationException("Complete the previous video before continuing.");
        var saved = (await _progress.FindAsync(x => x.EmployeeId == employeeId && x.TrainingVideoId == videoId)).SingleOrDefault();
        if (saved == null)
        {
            saved = new VideoProgress { EmployeeId = employeeId, TrainingVideoId = videoId };
            await _progress.AddAsync(saved);
        }
        saved.UpdatedAt = DateTime.UtcNow; // starts a server-timed playback session
        saved.PlaybackState = "paused";
        saved.PlaybackCreditSeconds = Math.Min(0, saved.PlaybackCreditSeconds);
        await _progress.SaveChangesAsync();
        var program = await _programs.GetByIdAsync(video.TrainingProgramId);
        return new VideoPlaybackDto { VideoId = video.Id, TrainingProgramId = video.TrainingProgramId,
            TrainingProgramTitle = program?.Title ?? string.Empty, VideoTitle = video.Title, Description = video.Description,
            VideoUrl = video.VideoUrl, SequenceOrder = video.SequenceOrder, VideoNumber = index + 1, VideoCount = ordered.Count,
            DurationSeconds = video.DurationSeconds, CurrentPositionSeconds = saved.CurrentPositionSeconds, IsCompleted = saved.IsCompleted,
            PreviousVideoId = index > 0 ? ordered[index - 1].Id : null, NextVideoId = index + 1 < ordered.Count ? ordered[index + 1].Id : null,
            IsNextUnlocked = saved.IsCompleted };
    }

    public async Task<VideoHeartbeatResult> RecordHeartbeatAsync(int employeeId, VideoHeartbeatRequest request)
    {
        await EnsureNoActiveAttemptAsync(employeeId);
        if (request.PlaybackState is not ("playing" or "paused" or "ended")) throw new ArgumentException("Invalid playback state.");
        var video = await _videos.GetByIdAsync(request.VideoId);
        if (video == null || !await IsAssignedAsync(employeeId, video.TrainingProgramId)) throw new UnauthorizedAccessException();
        if (video.DurationSeconds <= 0 || double.IsNaN(request.PositionSeconds) || double.IsInfinity(request.PositionSeconds) || request.PositionSeconds < 0)
            throw new ArgumentException("Invalid playback position.");
        var saved = (await _progress.FindAsync(x => x.EmployeeId == employeeId && x.TrainingVideoId == video.Id)).SingleOrDefault()
            ?? throw new InvalidOperationException("Open the video before reporting playback progress.");
        var prior = await _videos.FindAsync(x => x.TrainingProgramId == video.TrainingProgramId && x.SequenceOrder < video.SequenceOrder);
        var completedIds = (await _progress.FindAsync(x => x.EmployeeId == employeeId && x.IsCompleted)).Select(x => x.TrainingVideoId).ToHashSet();
        if (prior.Any(x => !completedIds.Contains(x.Id))) throw new InvalidOperationException("Complete the previous videos first.");
        if (saved.IsCompleted) return Result(saved, video.DurationSeconds);

        var now = DateTime.UtcNow;
        var elapsed = Math.Max(0, (now - (saved.UpdatedAt ?? saved.CreatedAt)).TotalSeconds);
        if (elapsed > MaximumHeartbeatGapSeconds)
            throw new InvalidOperationException("Playback session expired. Reload the player to resume.");
        var credit = Math.Min(MaximumHeartbeatGapSeconds, saved.PlaybackCreditSeconds + (saved.PlaybackState == "playing" ? elapsed : 0));
        var advance = Math.Max(0, Math.Floor(request.PositionSeconds) - saved.CurrentPositionSeconds);
        if (advance > 0 && advance > credit + TimingToleranceSeconds)
            throw new ITC.TrainingPortal.Domain.Exceptions.PlaybackTimingException(saved.CurrentPositionSeconds, request.PositionSeconds, elapsed, credit);

        var endedAtDurationBoundary = request.PlaybackState == "ended" &&
            request.PositionSeconds >= video.DurationSeconds - EndedCompletionToleranceSeconds;
        var accepted = endedAtDurationBoundary
            ? video.DurationSeconds
            : Math.Min(video.DurationSeconds, (int)Math.Floor(request.PositionSeconds));
        if (accepted > saved.CurrentPositionSeconds) saved.CurrentPositionSeconds = accepted;
        // Keep any timing debt: the tolerance is not extra credit on every request.
        saved.PlaybackCreditSeconds = credit - advance;
        saved.PlaybackState = request.PlaybackState;
        saved.IsCompleted = saved.CurrentPositionSeconds >= video.DurationSeconds;
        saved.UpdatedAt = now;
        _progress.Update(saved);
        await _progress.SaveChangesAsync();
        return Result(saved, video.DurationSeconds);
    }

    private Task<bool> IsAssignedAsync(int employeeId, int programId) =>
        _assignments.AnyAsync(x => x.EmployeeId == employeeId && x.TrainingProgramId == programId);

    private async Task EnsureNoActiveAttemptAsync(int employeeId)
    {
        if (await _quizResults.AnyAsync(x => x.EmployeeId == employeeId && !x.IsCompleted))
            throw new InvalidOperationException("Training content cannot be viewed while an exam attempt is in progress.");
    }

    private static VideoHeartbeatResult Result(VideoProgress progress, int duration) => new()
    {
        AcceptedPositionSeconds = progress.CurrentPositionSeconds,
        IsCompleted = progress.IsCompleted,
        ProgressPercentage = duration <= 0 ? 0 : Math.Min(100, (int)Math.Round(progress.CurrentPositionSeconds * 100d / duration))
    };
}
