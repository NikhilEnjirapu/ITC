using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Domain.Interfaces;

namespace ITC.TrainingPortal.Application.Services;

public sealed class QuizService : IQuizService
{
    private readonly IRepository<Quiz> _quizzes;
    private readonly IRepository<QuizQuestion> _questions;
    private readonly IRepository<QuizResult> _results;
    private readonly IRepository<TrainingProgram> _programs;
    private readonly IRepository<TrainingAssignment> _assignments;
    private readonly IRepository<TrainingVideo> _videos;
    private readonly IRepository<VideoProgress> _progress;

    public QuizService(IRepository<Quiz> quizzes, IRepository<QuizQuestion> questions, IRepository<QuizResult> results,
        IRepository<TrainingProgram> programs, IRepository<TrainingAssignment> assignments,
        IRepository<TrainingVideo> videos, IRepository<VideoProgress> progress)
    {
        _quizzes = quizzes; _questions = questions; _results = results; _programs = programs;
        _assignments = assignments; _videos = videos; _progress = progress;
    }

    public async Task<IReadOnlyList<QuizDto>> GetAllAsync()
    {
        var programs = (await _programs.GetAllAsync()).ToDictionary(x => x.Id, x => x.Title);
        var counts = (await _questions.GetAllAsync()).GroupBy(x => x.QuizId).ToDictionary(x => x.Key, x => x.Count());
        return (await _quizzes.GetAllAsync()).OrderBy(x => x.Title)
            .Select(x => Map(x, programs.GetValueOrDefault(x.TrainingProgramId), counts.GetValueOrDefault(x.Id))).ToList();
    }

    public async Task<QuizDto?> GetAsync(int id)
    {
        var quiz = await _quizzes.GetByIdAsync(id);
        if (quiz == null) return null;
        var program = await _programs.GetByIdAsync(quiz.TrainingProgramId);
        var dto = Map(quiz, program?.Title, (await _questions.FindAsync(x => x.QuizId == id)).Count());
        dto.ExamUnlockDescription = program?.ExamUnlockCondition == ExamUnlockCondition.SpecificVideo
            ? $"After \"{(await _videos.GetByIdAsync(program.ExamUnlockVideoId ?? 0))?.Title ?? "Unavailable video"}\""
            : "After all videos";
        return dto;
    }

    public async Task<int> CreateAsync(QuizDto dto)
    {
        await ValidateQuizAsync(dto);
        var entity = new Quiz { Title = dto.Title.Trim(), PassingScorePercentage = dto.PassingScorePercentage,
            TrainingProgramId = dto.TrainingProgramId, IsActive = false };
        await _quizzes.AddAsync(entity); await _quizzes.SaveChangesAsync(); return entity.Id;
    }

    public async Task UpdateAsync(QuizDto dto)
    {
        await ValidateQuizAsync(dto);
        var entity = await _quizzes.GetByIdAsync(dto.Id) ?? throw new InvalidOperationException("Exam does not exist.");
        if (dto.IsActive && (!await _questions.AnyAsync(x => x.QuizId == dto.Id) || !await _videos.AnyAsync(x => x.TrainingProgramId == dto.TrainingProgramId)))
            throw new InvalidOperationException("Add videos and at least one exam question before activating the exam.");
        if ((entity.TrainingProgramId != dto.TrainingProgramId || entity.PassingScorePercentage != dto.PassingScorePercentage)
            && await _results.AnyAsync(x => x.QuizId == dto.Id))
            throw new InvalidOperationException("The program and passing score cannot change after an exam has attempts.");
        entity.Title = dto.Title.Trim(); entity.PassingScorePercentage = dto.PassingScorePercentage;
        entity.TrainingProgramId = dto.TrainingProgramId; entity.IsActive = dto.IsActive; entity.UpdatedAt = DateTime.UtcNow;
        _quizzes.Update(entity); await _quizzes.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _quizzes.GetByIdAsync(id); if (entity == null) return;
        if (await _results.AnyAsync(x => x.QuizId == id)) throw new InvalidOperationException("An exam with attempts cannot be deleted. Deactivate it instead.");
        _quizzes.Remove(entity); await _quizzes.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<QuizQuestionDto>> GetQuestionsAsync(int quizId) =>
        (await _questions.FindAsync(x => x.QuizId == quizId)).OrderBy(x => x.SequenceOrder).Select(Map).ToList();

    public async Task<QuizQuestionDto?> GetQuestionAsync(int id) => (await _questions.GetByIdAsync(id)) is { } q ? Map(q) : null;

    public async Task AddQuestionAsync(QuizQuestionDto dto)
    {
        await ValidateQuestionAsync(dto);
        await _questions.AddAsync(ToEntity(dto)); await _questions.SaveChangesAsync();
    }

    public async Task UpdateQuestionAsync(QuizQuestionDto dto)
    {
        await ValidateQuestionAsync(dto);
        var q = await _questions.GetByIdAsync(dto.Id) ?? throw new InvalidOperationException("Question does not exist.");
        if (q.QuizId != dto.QuizId) throw new InvalidOperationException("A question cannot be moved to another exam.");
        await EnsureQuestionsEditableAsync(q.QuizId);
        q.QuestionText = dto.QuestionText.Trim(); q.OptionA = dto.OptionA.Trim(); q.OptionB = dto.OptionB.Trim();
        q.OptionC = dto.OptionC.Trim(); q.OptionD = dto.OptionD.Trim(); q.CorrectOption = dto.CorrectOption.ToUpperInvariant();
        q.SequenceOrder = dto.SequenceOrder; q.UpdatedAt = DateTime.UtcNow;
        _questions.Update(q); await _questions.SaveChangesAsync();
    }

    public async Task DeleteQuestionAsync(int id)
    {
        var q = await _questions.GetByIdAsync(id); if (q == null) return;
        await EnsureQuestionsEditableAsync(q.QuizId);
        _questions.Remove(q); await _questions.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ExamAccessDto>> GetEmployeeExamsAsync(int employeeId)
    {
        var programIds = (await _assignments.FindAsync(x => x.EmployeeId == employeeId)).Select(x => x.TrainingProgramId).ToHashSet();
        var quizzes = (await _quizzes.GetAllAsync()).Where(x => programIds.Contains(x.TrainingProgramId)).ToList();
        var list = new List<ExamAccessDto>();
        foreach (var quiz in quizzes) list.Add((await GetAccessAsync(employeeId, quiz.Id))!);
        return list;
    }

    public async Task<ExamAccessDto?> GetAccessAsync(int employeeId, int quizId)
    {
        var quiz = await _quizzes.GetByIdAsync(quizId);
        if (quiz == null || !await _assignments.AnyAsync(x => x.EmployeeId == employeeId && x.TrainingProgramId == quiz.TrainingProgramId)) return null;
        var program = await _programs.GetByIdAsync(quiz.TrainingProgramId);
        var questions = (await _questions.FindAsync(x => x.QuizId == quizId)).ToList();
        var results = (await _results.FindAsync(x => x.EmployeeId == employeeId && x.QuizId == quizId)).OrderByDescending(x => x.AttemptDate).ToList();
        var videos = (await _videos.FindAsync(x => x.TrainingProgramId == quiz.TrainingProgramId)).ToList();
        var completed = (await _progress.FindAsync(x => x.EmployeeId == employeeId && x.IsCompleted)).Select(x => x.TrainingVideoId).ToHashSet();
        var unlockVideo = videos.SingleOrDefault(x => x.Id == program?.ExamUnlockVideoId);
        var specific = program?.ExamUnlockCondition == ExamUnlockCondition.SpecificVideo;
        var unlocked = program?.ExamUnlockCondition switch
        {
            ExamUnlockCondition.AllVideos => videos.Count > 0 && videos.All(x => completed.Contains(x.Id)),
            ExamUnlockCondition.SpecificVideo => unlockVideo != null && completed.Contains(unlockVideo.Id),
            _ => false
        };
        return new ExamAccessDto { QuizId = quiz.Id, QuizTitle = quiz.Title, TrainingProgramId = quiz.TrainingProgramId,
            TrainingProgramTitle = program?.Title ?? string.Empty, IsActive = quiz.IsActive,
            IsUnlocked = unlocked, QuestionCount = questions.Count,
            UnlockMessage = specific ? unlockVideo == null ? "The exam unlock video is unavailable. Contact your administrator."
                : $"Complete '{unlockVideo.Title}' to unlock this exam." : "Complete all training videos to unlock this exam.",
            ActiveAttemptId = results.FirstOrDefault(x => !x.IsCompleted)?.Id,
            Results = results.Where(x => x.IsCompleted).Select(x => MapResult(x, quiz, program)).ToList() };
    }

    public async Task<ExamAttemptDto> StartOrResumeAsync(int employeeId, int quizId)
    {
        var access = await GetAccessAsync(employeeId, quizId) ?? throw new UnauthorizedAccessException();
        if (access.Results.Count > 0 && !access.ActiveAttemptId.HasValue)
            throw new InvalidOperationException("This exam has already been completed. View your saved result.");
        if (!access.IsActive) throw new InvalidOperationException("This exam is not active.");
        if (!access.IsUnlocked) throw new InvalidOperationException(access.UnlockMessage);
        var questions = (await _questions.FindAsync(x => x.QuizId == quizId)).OrderBy(x => x.SequenceOrder).ToList();
        if (questions.Count == 0) throw new InvalidOperationException("This exam has no questions and cannot be started.");
        var attempt = (await _results.FindAsync(x => x.EmployeeId == employeeId && x.QuizId == quizId && !x.IsCompleted)).SingleOrDefault();
        if (attempt == null)
        {
            if (await HasActiveAttemptAsync(employeeId)) throw new InvalidOperationException("Finish your active exam before starting another exam.");
            attempt = new QuizResult { EmployeeId = employeeId, QuizId = quizId, AttemptDate = DateTime.UtcNow };
            await _results.AddAsync(attempt); await _results.SaveChangesAsync();
        }
        return new ExamAttemptDto { QuizId = access.QuizId, QuizTitle = access.QuizTitle,
            TrainingProgramId = access.TrainingProgramId, TrainingProgramTitle = access.TrainingProgramTitle,
            IsUnlocked = true, IsActive = true, QuestionCount = questions.Count, AttemptId = attempt.Id,
            ActiveAttemptId = attempt.Id, Results = access.Results,
            Questions = questions.Select(x => new ExamQuestionDto { Id = x.Id, SequenceOrder = x.SequenceOrder,
                QuestionText = x.QuestionText, Options = new Dictionary<string, string> { ["A"] = x.OptionA, ["B"] = x.OptionB, ["C"] = x.OptionC, ["D"] = x.OptionD } }).ToList() };
    }

    public async Task<QuizResultDto> SubmitAsync(int employeeId, ExamSubmissionDto submission)
    {
        var attempt = await _results.GetByIdAsync(submission.AttemptId);
        if (attempt == null || attempt.EmployeeId != employeeId) throw new UnauthorizedAccessException();
        if (attempt.IsCompleted) throw new InvalidOperationException("This exam attempt has already been submitted.");
        var questions = (await _questions.FindAsync(x => x.QuizId == attempt.QuizId)).ToList();
        if (questions.Count == 0 || questions.Any(x => !submission.Answers.TryGetValue(x.Id, out var answer) || string.IsNullOrEmpty(answer) || answer.Length != 1 || !"ABCD".Contains(answer.ToUpperInvariant())))
            throw new InvalidOperationException("Answer every question before submitting the exam.");
        var correct = questions.Count(x => string.Equals(x.CorrectOption, submission.Answers[x.Id], StringComparison.OrdinalIgnoreCase));
        var quiz = await _quizzes.GetByIdAsync(attempt.QuizId) ?? throw new InvalidOperationException("Exam does not exist.");
        attempt.ScorePercentage = (int)Math.Round(correct * 100d / questions.Count, MidpointRounding.AwayFromZero);
        attempt.IsPassed = attempt.ScorePercentage >= quiz.PassingScorePercentage; attempt.IsCompleted = true;
        attempt.CompletedAt = DateTime.UtcNow; attempt.UpdatedAt = DateTime.UtcNow;
        attempt.SubmittedAnswersJson = System.Text.Json.JsonSerializer.Serialize(submission.Answers);
        _results.Update(attempt); await _results.SaveChangesAsync();
        return MapResult(attempt, quiz, await _programs.GetByIdAsync(quiz.TrainingProgramId));
    }

    public async Task<QuizResultDto?> GetResultAsync(int employeeId, int resultId)
    {
        var result = await _results.GetByIdAsync(resultId);
        if (result == null || result.EmployeeId != employeeId || !result.IsCompleted) return null;
        var quiz = await _quizzes.GetByIdAsync(result.QuizId); if (quiz == null) return null;
        return MapResult(result, quiz, await _programs.GetByIdAsync(quiz.TrainingProgramId));
    }

    public Task<bool> HasActiveAttemptAsync(int employeeId) => _results.AnyAsync(x => x.EmployeeId == employeeId && !x.IsCompleted);

    private async Task ValidateQuizAsync(QuizDto dto)
    {
        if (!await _programs.AnyAsync(x => x.Id == dto.TrainingProgramId)) throw new InvalidOperationException("Selected training program does not exist.");
        if (await _quizzes.AnyAsync(x => x.TrainingProgramId == dto.TrainingProgramId && x.Id != dto.Id)) throw new InvalidOperationException("This training program already has an exam.");
    }

    private async Task ValidateQuestionAsync(QuizQuestionDto dto)
    {
        await EnsureQuestionsEditableAsync(dto.QuizId);
        if (!await _quizzes.AnyAsync(x => x.Id == dto.QuizId)) throw new InvalidOperationException("Exam does not exist.");
        if (await _questions.AnyAsync(x => x.QuizId == dto.QuizId && x.SequenceOrder == dto.SequenceOrder && x.Id != dto.Id))
            throw new InvalidOperationException("Question order must be unique within the exam.");
    }

    private async Task EnsureQuestionsEditableAsync(int quizId)
    {
        if (await _results.AnyAsync(x => x.QuizId == quizId))
            throw new InvalidOperationException("Questions cannot change after an exam has attempts. Create a new training program for a revised assessment.");
    }

    private static QuizDto Map(Quiz x, string? program, int count) => new() { Id = x.Id, Title = x.Title,
        PassingScorePercentage = x.PassingScorePercentage, TrainingProgramId = x.TrainingProgramId,
        TrainingProgramTitle = program, IsActive = x.IsActive, QuestionCount = count };
    private static QuizQuestionDto Map(QuizQuestion x) => new() { Id = x.Id, QuizId = x.QuizId, QuestionText = x.QuestionText,
        OptionA = x.OptionA, OptionB = x.OptionB, OptionC = x.OptionC, OptionD = x.OptionD,
        CorrectOption = x.CorrectOption, SequenceOrder = x.SequenceOrder };
    private static QuizQuestion ToEntity(QuizQuestionDto x) => new() { QuizId = x.QuizId, QuestionText = x.QuestionText.Trim(),
        OptionA = x.OptionA.Trim(), OptionB = x.OptionB.Trim(), OptionC = x.OptionC.Trim(), OptionD = x.OptionD.Trim(),
        CorrectOption = x.CorrectOption.ToUpperInvariant(), SequenceOrder = x.SequenceOrder };
    private static QuizResultDto MapResult(QuizResult x, Quiz quiz, TrainingProgram? program) => new() { Id = x.Id,
        QuizId = x.QuizId, QuizTitle = quiz.Title, TrainingProgramTitle = program?.Title ?? string.Empty,
        ScorePercentage = x.ScorePercentage, IsPassed = x.IsPassed, IsCompleted = x.IsCompleted,
        AttemptDate = x.AttemptDate, CompletedAt = x.CompletedAt };
}
