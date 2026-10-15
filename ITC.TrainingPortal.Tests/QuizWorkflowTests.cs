using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Domain.Entities;

namespace ITC.TrainingPortal.Tests;

public sealed class QuizWorkflowTests
{
    [Fact] public async Task Exam_Can_Be_Created_For_A_Program()
    { var f=Fixture(); await f.Service.CreateAsync(new(){Title="Exam B",TrainingProgramId=11}); Assert.Contains(await f.Quizzes.GetAllAsync(),x=>x.TrainingProgramId==11); }

    [Fact] public async Task Multiple_Programs_Have_Separate_Exams()
    { var f=Fixture(); await f.Service.CreateAsync(new(){Title="Exam B",TrainingProgramId=11}); Assert.Equal(2,(await f.Service.GetAllAsync()).Count); }

    [Fact] public async Task Question_Is_Stored_Against_Its_Exam()
    { var f=Fixture(); await f.Service.AddQuestionAsync(Question(10,2)); Assert.All(await f.Service.GetQuestionsAsync(10),x=>Assert.Equal(10,x.QuizId)); }

    [Fact] public async Task Assigned_Employee_Can_See_Exam()
    { var f=Fixture(); Assert.Single(await f.Service.GetEmployeeExamsAsync(1)); }

    [Fact] public async Task Unassigned_Employee_Cannot_Access_Exam()
    { var f=Fixture(); Assert.Null(await f.Service.GetAccessAsync(2,10)); }

    [Fact] public async Task Exam_Is_Locked_While_Required_Video_Is_Incomplete()
    { var f=Fixture(completed:false); Assert.False((await f.Service.GetAccessAsync(1,10))!.IsUnlocked); }

    [Fact] public async Task Exam_Unlocks_When_All_Videos_Are_Complete()
    { var f=Fixture(); Assert.True((await f.Service.GetAccessAsync(1,10))!.IsUnlocked); }

    [Fact] public async Task Locked_Exam_Cannot_Be_Started_By_Direct_Call()
    { var f=Fixture(completed:false); await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Service.StartOrResumeAsync(1,10)); }

    [Fact] public async Task Starting_Exam_Creates_Active_Attempt()
    { var f=Fixture(); await f.Service.StartOrResumeAsync(1,10); Assert.True(await f.Service.HasActiveAttemptAsync(1)); }

    [Fact] public async Task Active_Attempt_Blocks_Direct_Video_Access()
    { var f=Fixture(activeAttempt:true); var playback=new TrainingPlaybackService(f.Assignments,f.Programs,f.Videos,f.Progress,f.Results); await Assert.ThrowsAsync<InvalidOperationException>(()=>playback.OpenVideoAsync(1,100)); }

    [Fact] public async Task Active_Attempt_Blocks_Training_Content_Access()
    { var f=Fixture(activeAttempt:true); var playback=new TrainingPlaybackService(f.Assignments,f.Programs,f.Videos,f.Progress,f.Results); await Assert.ThrowsAsync<InvalidOperationException>(()=>playback.GetAssignedContentAsync(1,10)); }

    [Fact] public async Task Server_Calculates_Score_And_Pass_Fail_Then_Persists_Result()
    { var f=Fixture(activeAttempt:true); var result=await f.Service.SubmitAsync(1,new(){AttemptId=50,Answers=new Dictionary<int,string>{{20,"A"}}}); Assert.Equal(100,result.ScorePercentage); Assert.True(result.IsPassed); Assert.True((await f.Results.GetByIdAsync(50))!.IsCompleted); }

    [Fact]
    public async Task Employee_Can_Start_Three_Attempts_But_Not_A_Fourth()
    {
        var f = Fixture();
        for (var number = 1; number <= ExamAccessDto.MaximumAttempts; number++)
        {
            var attempt = await f.Service.StartOrResumeAsync(1, 10);
            var stored = (await f.Results.FindAsync(x => x.EmployeeId == 1 && x.QuizId == 10 && !x.IsCompleted)).Single();
            stored.Id = 50 + number;
            await f.Service.SubmitAsync(1, new() { AttemptId = stored.Id, Answers = new Dictionary<int, string> { [20] = "B" } });
        }

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.StartOrResumeAsync(1, 10));
        Assert.Contains("all 3 attempts", error.Message);
        Assert.Equal(ExamAccessDto.MaximumAttempts, (await f.Results.FindAsync(x => x.EmployeeId == 1 && x.QuizId == 10)).Count());
    }

    [Fact]
    public async Task Exam_Access_Uses_Highest_Graded_Result_As_Best_Score()
    {
        var f = Fixture();
        await f.Results.AddAsync(new() { Id=51, EmployeeId=1, QuizId=10, IsCompleted=true, ScorePercentage=40, AttemptDate=DateTime.UtcNow.AddMinutes(-2) });
        await f.Results.AddAsync(new() { Id=52, EmployeeId=1, QuizId=10, IsCompleted=true, ScorePercentage=100, IsPassed=true, AttemptDate=DateTime.UtcNow.AddMinutes(-3) });
        await f.Results.AddAsync(new() { Id=53, EmployeeId=1, QuizId=10, IsCompleted=true, ScorePercentage=60, AttemptDate=DateTime.UtcNow.AddMinutes(-1) });

        var access = await f.Service.GetAccessAsync(1, 10);

        Assert.Equal(100, access!.BestResult!.ScorePercentage);
        Assert.Equal(0, access.RemainingAttempts);
    }

    [Fact] public async Task Client_Cannot_Submit_Arbitrary_Score()
    { Assert.Null(typeof(ExamSubmissionDto).GetProperty("ScorePercentage")); await Task.CompletedTask; }

    [Fact] public async Task Missing_Answers_Are_Rejected()
    { var f=Fixture(activeAttempt:true); await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Service.SubmitAsync(1,new(){AttemptId=50})); }

    [Fact] public async Task Employee_Cannot_Submit_Another_Employees_Attempt()
    { var f=Fixture(activeAttempt:true); await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Service.SubmitAsync(2,new(){AttemptId=50,Answers=new Dictionary<int,string>{{20,"A"}}})); }

    [Fact] public async Task Employee_Cannot_Read_Another_Employees_Result()
    { var f=Fixture(completedResult:true); Assert.Null(await f.Service.GetResultAsync(2,50)); }

    [Fact] public void Employee_Question_Dto_Does_Not_Expose_Correct_Answer()
    { Assert.Null(typeof(ExamQuestionDto).GetProperty("CorrectOption")); }

    [Fact]
    public async Task Question_Order_Is_Shuffled_Per_Attempt_And_Remains_Stable_On_Resume()
    {
        var f = Fixture();
        await f.Questions.AddAsync(new() { Id=21, QuizId=10, SequenceOrder=2, QuestionText="Second", OptionA="A", OptionB="B", OptionC="C", OptionD="D", CorrectOption="A" });
        await f.Questions.AddAsync(new() { Id=22, QuizId=10, SequenceOrder=3, QuestionText="Third", OptionA="A", OptionB="B", OptionC="C", OptionD="D", CorrectOption="A" });
        await f.Assignments.AddAsync(new() { EmployeeId=2, TrainingProgramId=10 });
        await f.Progress.AddAsync(new() { EmployeeId=2, TrainingVideoId=100, IsCompleted=true });

        var first = await f.Service.StartOrResumeAsync(1,10);
        var resumed = await f.Service.StartOrResumeAsync(1,10);
        var secondEmployee = await f.Service.StartOrResumeAsync(2,10);

        Assert.Equal(first.Questions.Select(x => x.Id), resumed.Questions.Select(x => x.Id));
        Assert.NotEqual(first.Questions.Select(x => x.Id), secondEmployee.Questions.Select(x => x.Id));
        Assert.Equal([1,2,3], first.Questions.Select(x => x.SequenceOrder));
        Assert.Equal(new[] { 20,21,22 }.Order(), first.Questions.Select(x => x.Id).Order());
        Assert.False(string.IsNullOrWhiteSpace((await f.Results.FindAsync(x => x.EmployeeId == 1)).Single().QuestionOrderJson));
    }

    [Fact]
    public async Task Legacy_Active_Attempt_Gets_Stable_Shuffled_Order_On_First_Resume()
    {
        var f = Fixture(activeAttempt:true);
        await f.Questions.AddAsync(new() { Id=21, QuizId=10, SequenceOrder=2, QuestionText="Second", OptionA="A", OptionB="B", OptionC="C", OptionD="D", CorrectOption="A" });
        var first = await f.Service.StartOrResumeAsync(1,10);
        var resumed = await f.Service.StartOrResumeAsync(1,10);
        Assert.Equal(first.Questions.Select(x => x.Id), resumed.Questions.Select(x => x.Id));
        Assert.False(string.IsNullOrWhiteSpace((await f.Results.GetByIdAsync(50))!.QuestionOrderJson));
    }

    [Fact]
    public async Task Mixed_Exam_Submission_Awaits_Review_And_Releases_Playback_Lock()
    {
        var f = Fixture();
        await f.Service.AddQuestionAsync(new() { QuizId = 10, SequenceOrder = 2, QuestionText = "Explain the safe procedure", IsTypedAnswer = true });
        var attempt = await f.Service.StartOrResumeAsync(1, 10);
        var typed = Assert.Single(attempt.Questions.Where(x => x.IsTypedAnswer));
        Assert.Empty(typed.Options);
        var answers = attempt.Questions.ToDictionary(x => x.Id, x => x.IsTypedAnswer ? "My descriptive answer" : "A");
        var result = await f.Service.SubmitAsync(1, new() { AttemptId = attempt.AttemptId, Answers = answers });
        Assert.True(result.RequiresReview);
        Assert.False(result.IsPassed);
        Assert.Equal("Awaiting review", result.Outcome);
        Assert.False(await f.Service.HasActiveAttemptAsync(1));
        Assert.Contains("My descriptive answer", (await f.Results.GetByIdAsync(result.Id))!.SubmittedAnswersJson);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Blank_Typed_Answers_Are_Rejected(string? answer)
    {
        var f = Fixture();
        await f.Service.AddQuestionAsync(new() { QuizId = 10, SequenceOrder = 2, QuestionText = "Explain", IsTypedAnswer = true });
        var attempt = await f.Service.StartOrResumeAsync(1, 10);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.SubmitAsync(1, new() {
            AttemptId = attempt.AttemptId, Answers = attempt.Questions.ToDictionary(x => x.Id, x => x.IsTypedAnswer ? answer! : "A") }));
        Assert.True(await f.Service.HasActiveAttemptAsync(1));
    }

    [Fact]
    public async Task Oversized_Typed_Answer_Is_Rejected()
    {
        var f = Fixture();
        await f.Service.AddQuestionAsync(new() { QuizId = 10, SequenceOrder = 2, QuestionText = "Explain", IsTypedAnswer = true });
        var attempt = await f.Service.StartOrResumeAsync(1, 10);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.SubmitAsync(1, new() {
            AttemptId = attempt.AttemptId, Answers = attempt.Questions.ToDictionary(x => x.Id, x => x.IsTypedAnswer ? new string('x', 5001) : "A") }));
    }

    [Fact]
    public async Task Multiple_Choice_Still_Requires_Options()
    {
        var f = Fixture();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.AddQuestionAsync(new() { QuizId = 10, SequenceOrder = 2, QuestionText = "Q" }));
    }

    private static QuizQuestionDto Question(int quizId,int order)=>new(){QuizId=quizId,SequenceOrder=order,QuestionText="Q",OptionA="A",OptionB="B",OptionC="C",OptionD="D",CorrectOption="A"};

    private static TestFixture Fixture(bool completed=true,bool activeAttempt=false,bool completedResult=false)
    {
        var quizzes=new FakeRepository<Quiz>(new Quiz{Id=10,Title="Safety exam",TrainingProgramId=10,PassingScorePercentage=80,IsActive=true});
        var questions=new FakeRepository<QuizQuestion>(new QuizQuestion{Id=20,QuizId=10,SequenceOrder=1,QuestionText="Safe?",OptionA="Yes",OptionB="No",OptionC="Maybe",OptionD="Unknown",CorrectOption="A"});
        var results=new FakeRepository<QuizResult>();
        if(activeAttempt||completedResult) results.AddAsync(new(){Id=50,EmployeeId=1,QuizId=10,IsCompleted=completedResult,ScorePercentage=completedResult?100:0,IsPassed=completedResult}).GetAwaiter().GetResult();
        var programs=new FakeRepository<TrainingProgram>(new TrainingProgram{Id=10,Title="Safety"},new TrainingProgram{Id=11,Title="Quality"});
        var assignments=new FakeRepository<TrainingAssignment>(new TrainingAssignment{EmployeeId=1,TrainingProgramId=10});
        var videos=new FakeRepository<TrainingVideo>(new TrainingVideo{Id=100,TrainingProgramId=10,SequenceOrder=1,Title="Video",VideoUrl="https://example.com/v.mp4",DurationSeconds=100});
        var progress=new FakeRepository<VideoProgress>(new VideoProgress{EmployeeId=1,TrainingVideoId=100,IsCompleted=completed});
        return new(new QuizService(quizzes,questions,results,programs,assignments,videos,progress),quizzes,questions,results,programs,assignments,videos,progress);
    }

    private sealed record TestFixture(QuizService Service,FakeRepository<Quiz> Quizzes,FakeRepository<QuizQuestion> Questions,FakeRepository<QuizResult> Results,
        FakeRepository<TrainingProgram> Programs,FakeRepository<TrainingAssignment> Assignments,
        FakeRepository<TrainingVideo> Videos,FakeRepository<VideoProgress> Progress);
}
