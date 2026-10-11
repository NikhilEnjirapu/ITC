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
        return new(new QuizService(quizzes,questions,results,programs,assignments,videos,progress),quizzes,results,programs,assignments,videos,progress);
    }

    private sealed record TestFixture(QuizService Service,FakeRepository<Quiz> Quizzes,FakeRepository<QuizResult> Results,
        FakeRepository<TrainingProgram> Programs,FakeRepository<TrainingAssignment> Assignments,
        FakeRepository<TrainingVideo> Videos,FakeRepository<VideoProgress> Progress);
}
