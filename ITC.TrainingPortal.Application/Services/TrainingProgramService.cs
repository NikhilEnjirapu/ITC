using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Domain.Exceptions;
using ITC.TrainingPortal.Domain.Interfaces;

namespace ITC.TrainingPortal.Application.Services;

public sealed class TrainingProgramService : ITrainingProgramService
{
    private readonly IRepository<TrainingProgram> _programs;
    private readonly IRepository<TrainingVideo> _videos;
    private readonly IRepository<TrainingAssignment> _assignments;
    private readonly IRepository<Quiz> _quizzes;
    private readonly ITrainingProgramDeletion _deletion;

    public TrainingProgramService(IRepository<TrainingProgram> programs, IRepository<TrainingVideo> videos,
        IRepository<TrainingAssignment> assignments, IRepository<Quiz> quizzes, ITrainingProgramDeletion deletion)
    { _programs = programs; _videos = videos; _assignments = assignments; _quizzes = quizzes; _deletion = deletion; }

    public async Task<IEnumerable<TrainingProgramDto>> GetAllAsync()
    {
        var quizzes = (await _quizzes.GetAllAsync()).ToDictionary(x => x.TrainingProgramId, x => x.Title);
        return (await _programs.GetAllAsync()).Select(p => Map(p, quizzes.GetValueOrDefault(p.Id)));
    }
    public async Task<TrainingProgramDto?> GetByIdAsync(int id)
    {
        var program = await _programs.GetByIdAsync(id);
        if (program == null) return null;
        var quiz = (await _quizzes.FindAsync(x => x.TrainingProgramId == id)).SingleOrDefault();
        return Map(program, quiz?.Title);
    }

    public async Task<int> CreateAsync(TrainingProgramDto dto)
    {
        Normalize(dto);
        await ValidateUnlockAsync(dto, isNew: true);
        var program = new TrainingProgram { Title = dto.Title, Description = dto.Description,
            ExamUnlockCondition = dto.ExamUnlockCondition, ExamUnlockVideoId = dto.ExamUnlockVideoId };
        await _programs.AddAsync(program);
        await _programs.SaveChangesAsync();
        return program.Id;
    }

    public async Task UpdateAsync(TrainingProgramDto dto)
    {
        Normalize(dto);
        await ValidateUnlockAsync(dto);
        var entity = await _programs.GetByIdAsync(dto.Id) ?? throw new ConcurrencyConflictException("The training program was deleted by another user.");
        if (!entity.RowVersion.SequenceEqual(dto.RowVersion)) throw new ConcurrencyConflictException("The training program was changed by another user. Reload it and try again.");
        entity.Title = dto.Title; entity.Description = dto.Description; entity.UpdatedAt = DateTime.UtcNow;
        entity.ExamUnlockCondition = dto.ExamUnlockCondition; entity.ExamUnlockVideoId = dto.ExamUnlockVideoId;
        _programs.Update(entity); await _programs.SaveChangesAsync();
    }

    public Task DeleteAsync(int id) => _deletion.DeleteAsync(id);

    private async Task ValidateUnlockAsync(TrainingProgramDto dto, bool isNew = false)
    {
        if (!Enum.IsDefined(dto.ExamUnlockCondition)) throw new InvalidOperationException("Select a valid exam unlock condition.");
        if (dto.ExamUnlockCondition == ExamUnlockCondition.AllVideos) { dto.ExamUnlockVideoId = null; return; }
        if (isNew || !dto.ExamUnlockVideoId.HasValue || !await _videos.AnyAsync(x => x.Id == dto.ExamUnlockVideoId && x.TrainingProgramId == dto.Id))
            throw new InvalidOperationException("Select an unlock video belonging to this training program. For a new program, save it and add its videos first.");
    }

    private static TrainingProgramDto Map(TrainingProgram p, string? quizTitle) => new() { Id = p.Id, Title = p.Title, Description = p.Description, RelatedQuizTitle = quizTitle, RowVersion = p.RowVersion,
        ExamUnlockCondition = p.ExamUnlockCondition, ExamUnlockVideoId = p.ExamUnlockVideoId };
    private static void Normalize(TrainingProgramDto d) { d.Title = d.Title?.Trim() ?? ""; d.Description = string.IsNullOrWhiteSpace(d.Description) ? null : d.Description.Trim(); }
}
