using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Domain.Exceptions;
using ITC.TrainingPortal.Domain.Interfaces;

namespace ITC.TrainingPortal.Application.Services;

public sealed class TrainingVideoService : ITrainingVideoService
{
    private readonly IRepository<TrainingVideo> _videos; private readonly IRepository<TrainingProgram> _programs; private readonly IRepository<VideoProgress> _progress;
    public TrainingVideoService(IRepository<TrainingVideo> videos, IRepository<TrainingProgram> programs, IRepository<VideoProgress> progress)
    { _videos = videos; _programs = programs; _progress = progress; }
    public async Task<IEnumerable<TrainingVideoDto>> GetAllAsync()
    {
        var programs = (await _programs.GetAllAsync()).ToDictionary(x => x.Id, x => x.Title);
        return (await _videos.GetAllAsync()).OrderBy(x => x.TrainingProgramId).ThenBy(x => x.SequenceOrder).Select(x => Map(x, programs.GetValueOrDefault(x.TrainingProgramId)));
    }
    public async Task<TrainingVideoDto?> GetByIdAsync(int id)
    { var v = await _videos.GetByIdAsync(id); if (v == null) return null; return Map(v, (await _programs.GetByIdAsync(v.TrainingProgramId))?.Title); }
    public async Task CreateAsync(TrainingVideoDto dto)
    {
        Normalize(dto); await ValidateAsync(dto);
        await _videos.AddAsync(new TrainingVideo { Title = dto.Title, Description = dto.Description, VideoUrl = dto.VideoUrl, SequenceOrder = dto.SequenceOrder, DurationSeconds = dto.DurationSeconds, TrainingProgramId = dto.TrainingProgramId });
        await _videos.SaveChangesAsync();
    }
    public async Task UpdateAsync(TrainingVideoDto dto)
    {
        Normalize(dto); await ValidateAsync(dto);
        var entity = await _videos.GetByIdAsync(dto.Id) ?? throw new ConcurrencyConflictException("The training video was deleted by another user.");
        if (!entity.RowVersion.SequenceEqual(dto.RowVersion)) throw new ConcurrencyConflictException("The training video was changed by another user. Reload it and try again.");
        if (entity.TrainingProgramId != dto.TrainingProgramId && await _programs.AnyAsync(x => x.ExamUnlockVideoId == entity.Id))
            throw new InvalidOperationException("Change the program's exam unlock condition before moving its unlock video.");
        if ((entity.VideoUrl != dto.VideoUrl || entity.DurationSeconds != dto.DurationSeconds || entity.TrainingProgramId != dto.TrainingProgramId || entity.SequenceOrder != dto.SequenceOrder)
            && await _progress.AnyAsync(x => x.TrainingVideoId == entity.Id))
            throw new InvalidOperationException("Video content and ordering cannot change after viewing has started. Create a new training program for revised content.");
        entity.Title = dto.Title; entity.Description = dto.Description; entity.VideoUrl = dto.VideoUrl; entity.SequenceOrder = dto.SequenceOrder; entity.DurationSeconds = dto.DurationSeconds; entity.TrainingProgramId = dto.TrainingProgramId; entity.UpdatedAt = DateTime.UtcNow;
        _videos.Update(entity); await _videos.SaveChangesAsync();
    }
    public async Task DeleteAsync(int id)
    {
        var entity = await _videos.GetByIdAsync(id); if (entity == null) return;
        if (await _programs.AnyAsync(x => x.ExamUnlockVideoId == id))
            throw new InvalidOperationException("Change the program's exam unlock condition before deleting its unlock video.");
        if (await _progress.AnyAsync(x => x.TrainingVideoId == id)) throw new InvalidOperationException("Cannot delete a video that has employee progress records.");
        _videos.Remove(entity); await _videos.SaveChangesAsync();
    }
    private async Task ValidateAsync(TrainingVideoDto d)
    {
        if (Path.IsPathRooted(d.VideoUrl) || d.VideoUrl.Contains(':') || d.VideoUrl.Split('/', '\\').Any(x => x is ".." or ".") ||
            !new[] { ".mp4", ".webm" }.Contains(Path.GetExtension(d.VideoUrl).ToLowerInvariant()))
            throw new InvalidOperationException("Enter an MP4 or WebM filename relative to the private video repository, for example safety/intro.mp4.");
        if (!await _programs.AnyAsync(x => x.Id == d.TrainingProgramId)) throw new InvalidOperationException("Selected training program does not exist.");
        if (await _videos.AnyAsync(x => x.TrainingProgramId == d.TrainingProgramId && x.SequenceOrder == d.SequenceOrder && x.Id != d.Id)) throw new InvalidOperationException("Sequence order already exists in the selected program.");
    }
    private static TrainingVideoDto Map(TrainingVideo v, string? title) => new() { Id=v.Id, Title=v.Title, Description=v.Description, VideoUrl=v.VideoUrl, SequenceOrder=v.SequenceOrder, DurationSeconds=v.DurationSeconds, TrainingProgramId=v.TrainingProgramId, TrainingProgramTitle=title, RowVersion=v.RowVersion };
    private static void Normalize(TrainingVideoDto d) { d.Title=d.Title?.Trim()??""; d.Description=string.IsNullOrWhiteSpace(d.Description)?null:d.Description.Trim(); d.VideoUrl=d.VideoUrl?.Trim()??""; }
}
