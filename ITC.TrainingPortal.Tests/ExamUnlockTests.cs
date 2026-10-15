using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Domain.Entities;

namespace ITC.TrainingPortal.Tests;

public sealed class ExamUnlockTests
{
    [Theory]
    [InlineData(ExamUnlockCondition.AllVideos, 0, false)]
    [InlineData(ExamUnlockCondition.AllVideos, 1, false)]
    [InlineData(ExamUnlockCondition.AllVideos, 2, false)]
    [InlineData(ExamUnlockCondition.AllVideos, 3, true)]
    [InlineData(ExamUnlockCondition.SpecificVideo, 0, false)]
    [InlineData(ExamUnlockCondition.SpecificVideo, 1, false)]
    [InlineData(ExamUnlockCondition.SpecificVideo, 2, true)]
    [InlineData(ExamUnlockCondition.SpecificVideo, 3, true)]
    public async Task Unlock_And_Direct_Start_Use_Configured_Condition(ExamUnlockCondition condition, int count, bool expected)
    {
        var f = new Fixture(condition, count);
        var access = (await f.QuizService().GetAccessAsync(1, 10))!;
        Assert.Equal(expected, access.IsUnlocked);
        Assert.Contains(condition == ExamUnlockCondition.SpecificVideo ? "Emergency Procedures" : "all training videos", access.UnlockMessage);
        if(expected) Assert.NotNull(await f.QuizService().StartOrResumeAsync(1, 10));
        else await Assert.ThrowsAsync<InvalidOperationException>(() => f.QuizService().StartOrResumeAsync(1, 10));
        Assert.Null(await f.QuizService().GetAccessAsync(2, 10));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.QuizService().StartOrResumeAsync(2, 10));
    }

    [Fact]
    public async Task Switching_To_All_Clears_Video_And_Requires_Remaining_Videos()
    {
        var f = new Fixture(ExamUnlockCondition.SpecificVideo, 2);
        await f.ProgramService().UpdateAsync(new() { Id = 1, Title = "Safety", ExamUnlockVideoId = 102 });
        Assert.Null(f.Program.ExamUnlockVideoId);
        Assert.Equal(ExamUnlockCondition.AllVideos, f.Program.ExamUnlockCondition);
        Assert.False((await f.QuizService().GetAccessAsync(1, 10))!.IsUnlocked);
    }

    [Fact]
    public async Task Switching_To_Specific_Uses_Persisted_Completion_In_New_Service_Instance()
    {
        var f = new Fixture(ExamUnlockCondition.AllVideos, 2);
        Assert.False((await f.QuizService().GetAccessAsync(1, 10))!.IsUnlocked);
        await f.ProgramService().UpdateAsync(new() { Id = 1, Title = "Safety", ExamUnlockCondition = ExamUnlockCondition.SpecificVideo, ExamUnlockVideoId = 102 });
        Assert.True((await f.QuizService().GetAccessAsync(1, 10))!.IsUnlocked);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(201)]
    [InlineData(999)]
    public async Task Missing_Foreign_Or_Unknown_Video_Is_Rejected(int? video)
    {
        var f = new Fixture();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.ProgramService().UpdateAsync(new() {
            Id = 1, Title = "Safety", ExamUnlockCondition = ExamUnlockCondition.SpecificVideo, ExamUnlockVideoId = video }));
        Assert.Equal(ExamUnlockCondition.AllVideos, f.Program.ExamUnlockCondition);
    }

    [Fact]
    public async Task Invalid_Condition_Is_Rejected()
    {
        var f = new Fixture();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.ProgramService().UpdateAsync(new() {
            Id = 1, Title = "Safety", ExamUnlockCondition = (ExamUnlockCondition)99 }));
    }

    [Fact]
    public async Task New_Programs_Default_To_All_Videos_And_Cannot_Borrow_An_Existing_Video()
    {
        var f = new Fixture();
        await f.ProgramService().CreateAsync(new() { Title = "New" });
        Assert.All(await f.Programs.GetAllAsync(), p => { Assert.Equal(ExamUnlockCondition.AllVideos, p.ExamUnlockCondition); Assert.Null(p.ExamUnlockVideoId); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.ProgramService().CreateAsync(new() {
            Id = 1, Title = "Forged", ExamUnlockCondition = ExamUnlockCondition.SpecificVideo, ExamUnlockVideoId = 102 }));
    }

    [Fact]
    public async Task Other_Employees_Completion_And_Playback_Position_Do_Not_Unlock()
    {
        var f = new Fixture(ExamUnlockCondition.SpecificVideo);
        await f.Progress.AddAsync(new() { EmployeeId = 2, TrainingVideoId = 102, IsCompleted = true });
        await f.Progress.AddAsync(new() { EmployeeId = 1, TrainingVideoId = 102, CurrentPositionSeconds = 100, IsCompleted = false });
        Assert.False((await f.QuizService().GetAccessAsync(1, 10))!.IsUnlocked);
    }

    [Fact]
    public async Task Misconfigured_Reference_Fails_Closed()
    {
        var f = new Fixture(ExamUnlockCondition.SpecificVideo, 3);
        f.Program.ExamUnlockVideoId = 201;
        await f.Progress.AddAsync(new() { EmployeeId = 1, TrainingVideoId = 201, IsCompleted = true });
        Assert.False((await f.QuizService().GetAccessAsync(1, 10))!.IsUnlocked);
    }

    [Fact]
    public async Task Unlock_Video_Cannot_Be_Deleted_Or_Moved_Until_Configuration_Changes()
    {
        var f = new Fixture(ExamUnlockCondition.SpecificVideo);
        var service = new TrainingVideoService(f.Videos, f.Programs, f.Progress);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(102));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(new() {
            Id = 102, Title = "Emergency Procedures", VideoUrl = "v.mp4", DurationSeconds = 100, SequenceOrder = 2, TrainingProgramId = 2 }));
    }

    [Fact]
    public async Task Specific_Unlock_Preserves_Sequential_Viewing_And_Active_Attempt_Block()
    {
        var f = new Fixture(ExamUnlockCondition.SpecificVideo, 1);
        var playback = new TrainingPlaybackService(f.Assignments, f.Programs, f.Videos, f.Progress, f.Results);
        await Assert.ThrowsAsync<InvalidOperationException>(() => playback.OpenVideoAsync(1, 103));
        await f.Progress.AddAsync(new() { EmployeeId = 1, TrainingVideoId = 102, IsCompleted = true });
        await f.QuizService().StartOrResumeAsync(1, 10);
        await Assert.ThrowsAsync<InvalidOperationException>(() => playback.OpenVideoAsync(1, 103));
    }

    private sealed class Fixture
    {
        public TrainingProgram Program { get; }
        public FakeRepository<TrainingProgram> Programs { get; }
        public FakeRepository<TrainingVideo> Videos { get; }
        public FakeRepository<VideoProgress> Progress { get; } = new();
        public FakeRepository<TrainingAssignment> Assignments { get; } = new(new TrainingAssignment { EmployeeId = 1, TrainingProgramId = 1 });
        public FakeRepository<QuizResult> Results { get; } = new();
        private readonly FakeRepository<Quiz> _quizzes = new(new Quiz { Id = 10, TrainingProgramId = 1, Title = "Safety exam", IsActive = true });
        private readonly FakeRepository<QuizQuestion> _questions = new(new QuizQuestion { Id = 20, QuizId = 10, QuestionText = "Safe?", CorrectOption = "A" });
        public Fixture(ExamUnlockCondition condition = ExamUnlockCondition.AllVideos, int completed = 0)
        {
            Program = new() { Id = 1, Title = "Safety", ExamUnlockCondition = condition, ExamUnlockVideoId = condition == ExamUnlockCondition.SpecificVideo ? 102 : null };
            Programs = new(Program, new TrainingProgram { Id = 2, Title = "Other" });
            Videos = new(Enumerable.Range(1, 3).Select(n => new TrainingVideo { Id = 100 + n, TrainingProgramId = 1,
                Title = n == 2 ? "Emergency Procedures" : $"Video {n}", SequenceOrder = n, DurationSeconds = 100, VideoUrl = "v.mp4" })
                .Append(new TrainingVideo { Id = 201, TrainingProgramId = 2, SequenceOrder = 1 }).ToArray());
            for(var n = 1; n <= completed; n++) Progress.AddAsync(new() { EmployeeId = 1, TrainingVideoId = 100 + n, IsCompleted = true }).GetAwaiter().GetResult();
        }
        public QuizService QuizService() => new(_quizzes, _questions, Results, Programs, Assignments, Videos, Progress, new TestAssessmentTransaction());
        public TrainingProgramService ProgramService() => new(Programs, Videos, Assignments, _quizzes, null!);
    }
}
