using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Domain.Entities;

namespace ITC.TrainingPortal.Tests;

public sealed class TrainingPlaybackTests
{
    [Fact]
    public async Task Employee_Can_See_Only_Assigned_Program()
    {
        var service = Service(assignments: [new() { EmployeeId = 1, TrainingProgramId = 10 }]);
        Assert.NotNull(await service.GetAssignedContentAsync(1, 10));
        Assert.Null(await service.GetAssignedContentAsync(2, 10));
    }

    [Fact]
    public async Task Employee_Cannot_Open_Unassigned_Video()
    {
        var service = Service(assignments: [new() { EmployeeId = 2, TrainingProgramId = 10 }]);
        Assert.Null(await service.OpenVideoAsync(1, 100));
    }

    [Fact]
    public async Task Second_Video_Is_Locked_Until_First_Is_Complete()
    {
        var service = Service(assignments: [new() { EmployeeId = 1, TrainingProgramId = 10 }]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenVideoAsync(1, 101));
    }

    [Fact]
    public async Task Completed_First_Video_Unlocks_Second()
    {
        var service = Service(assignments: [new() { EmployeeId = 1, TrainingProgramId = 10 }],
            progress: [new() { EmployeeId = 1, TrainingVideoId = 100, IsCompleted = true }]);
        Assert.NotNull(await service.OpenVideoAsync(1, 101));
    }

    [Fact]
    public async Task OpenVideo_Returns_Resume_Position()
    {
        var service = Service(assignments: [new() { EmployeeId = 1, TrainingProgramId = 10 }],
            progress: [new() { EmployeeId = 1, TrainingVideoId = 100, CurrentPositionSeconds = 42 }]);
        Assert.Equal(42, (await service.OpenVideoAsync(1, 100))!.CurrentPositionSeconds);
    }

    [Fact]
    public async Task Valid_Heartbeat_Saves_Progress()
    {
        var saved = new VideoProgress { EmployeeId = 1, TrainingVideoId = 100, UpdatedAt = DateTime.UtcNow.AddSeconds(-8) };
        var service = Service(assignments: [new() { EmployeeId = 1, TrainingProgramId = 10 }], progress: [saved]);
        var result = await service.RecordHeartbeatAsync(1, new() { VideoId = 100, PositionSeconds = 8, PlaybackState = "playing" });
        Assert.Equal(8, result.AcceptedPositionSeconds);
    }

    [Fact]
    public async Task Large_Playback_Jump_Is_Rejected()
    {
        var saved = new VideoProgress { EmployeeId = 1, TrainingVideoId = 100, CurrentPositionSeconds = 10, UpdatedAt = DateTime.UtcNow.AddSeconds(-7) };
        var service = Service(assignments: [new() { EmployeeId = 1, TrainingProgramId = 10 }], progress: [saved]);
        await Assert.ThrowsAsync<ITC.TrainingPortal.Domain.Exceptions.PlaybackTimingException>(() => service.RecordHeartbeatAsync(1,
            new() { VideoId = 100, PositionSeconds = 90, PlaybackState = "playing" }));
    }

    [Fact]
    public async Task Employee_Cannot_Update_Another_Employees_Progress()
    {
        var saved = new VideoProgress { EmployeeId = 2, TrainingVideoId = 100, UpdatedAt = DateTime.UtcNow.AddSeconds(-8) };
        var service = Service(assignments: [new() { EmployeeId = 2, TrainingProgramId = 10 }], progress: [saved]);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RecordHeartbeatAsync(1,
            new() { VideoId = 100, PositionSeconds = 5, PlaybackState = "playing" }));
        Assert.Equal(0, saved.CurrentPositionSeconds);
    }

    [Fact]
    public async Task NinetyFivePercent_Does_Not_Complete_Video()
    {
        var saved = new VideoProgress { EmployeeId = 1, TrainingVideoId = 100, CurrentPositionSeconds = 90, UpdatedAt = DateTime.UtcNow.AddSeconds(-8) };
        var service = Service(assignments: [new() { EmployeeId = 1, TrainingProgramId = 10 }], progress: [saved]);
        var result = await service.RecordHeartbeatAsync(1, new() { VideoId = 100, PositionSeconds = 95, PlaybackState = "playing" });
        Assert.False(result.IsCompleted);
    }

    [Fact]
    public async Task Invalid_Playback_State_Is_Rejected()
    {
        var service = Service(assignments: [new() { EmployeeId = 1, TrainingProgramId = 10 }]);
        await Assert.ThrowsAsync<ArgumentException>(() => service.RecordHeartbeatAsync(1,
            new() { VideoId = 100, PositionSeconds = 1, PlaybackState = "seeking" }));
    }

    [Fact]
    public async Task Full_Duration_Completes_Video()
    {
        var saved = new VideoProgress { EmployeeId=1, TrainingVideoId=100, CurrentPositionSeconds=95, UpdatedAt=DateTime.UtcNow.AddSeconds(-6) };
        var service=Service(assignments:[new(){EmployeeId=1,TrainingProgramId=10}],progress:[saved]);
        Assert.True((await service.RecordHeartbeatAsync(1,new(){VideoId=100,PositionSeconds=100,PlaybackState="ended"})).IsCompleted);
    }

    [Fact]
    public async Task Ended_Event_Within_Final_Second_Completes_Video()
    {
        var saved = new VideoProgress { EmployeeId=1, TrainingVideoId=100, CurrentPositionSeconds=98,
            PlaybackState="playing", UpdatedAt=DateTime.UtcNow.AddSeconds(-2) };
        var service=Service(assignments:[new(){EmployeeId=1,TrainingProgramId=10}],progress:[saved]);
        var result=await service.RecordHeartbeatAsync(1,new(){VideoId=100,PositionSeconds=99,PlaybackState="ended"});
        Assert.True(result.IsCompleted);
        Assert.Equal(100,result.AcceptedPositionSeconds);
    }

    [Fact]
    public async Task Paused_Event_Within_Final_Second_Does_Not_Complete_Video()
    {
        var saved = new VideoProgress { EmployeeId=1, TrainingVideoId=100, CurrentPositionSeconds=98,
            PlaybackState="playing", UpdatedAt=DateTime.UtcNow.AddSeconds(-2) };
        var service=Service(assignments:[new(){EmployeeId=1,TrainingProgramId=10}],progress:[saved]);
        var result=await service.RecordHeartbeatAsync(1,new(){VideoId=100,PositionSeconds=99,PlaybackState="paused"});
        Assert.False(result.IsCompleted);
        Assert.Equal(99,result.AcceptedPositionSeconds);
    }

    [Fact]
    public async Task Ended_Event_More_Than_One_Second_Early_Does_Not_Complete_Video()
    {
        var saved = new VideoProgress { EmployeeId=1, TrainingVideoId=100, CurrentPositionSeconds=94,
            PlaybackState="playing", UpdatedAt=DateTime.UtcNow.AddSeconds(-5) };
        var service=Service(assignments:[new(){EmployeeId=1,TrainingProgramId=10}],progress:[saved]);
        var result=await service.RecordHeartbeatAsync(1,new(){VideoId=100,PositionSeconds=95,PlaybackState="ended"});
        Assert.False(result.IsCompleted);
    }

    [Fact]
    public async Task Pausing_Saves_The_Last_Played_Position()
    {
        var saved = new VideoProgress { EmployeeId=1, TrainingVideoId=100, UpdatedAt=DateTime.UtcNow.AddSeconds(-6) };
        var service=Service(assignments:[new(){EmployeeId=1,TrainingProgramId=10}],progress:[saved]);
        Assert.Equal(5,(await service.RecordHeartbeatAsync(1,new(){VideoId=100,PositionSeconds=5,PlaybackState="paused"})).AcceptedPositionSeconds);
    }

    [Fact]
    public async Task Repeated_Paused_Requests_Cannot_Manufacture_Progress()
    {
        var saved = new VideoProgress { EmployeeId=1, TrainingVideoId=100, PlaybackState="paused", UpdatedAt=DateTime.UtcNow.AddSeconds(-6) };
        var service=Service(assignments:[new(){EmployeeId=1,TrainingProgramId=10}],progress:[saved]);
        await Assert.ThrowsAsync<ITC.TrainingPortal.Domain.Exceptions.PlaybackTimingException>(()=>service.RecordHeartbeatAsync(1,new(){VideoId=100,PositionSeconds=3,PlaybackState="paused"}));
    }

    [Fact]
    public async Task Idle_Time_Cannot_Be_Converted_Into_A_Completed_Video()
    {
        var saved = new VideoProgress { EmployeeId=1, TrainingVideoId=100, UpdatedAt=DateTime.UtcNow.AddMinutes(-3) };
        var service=Service(assignments:[new(){EmployeeId=1,TrainingProgramId=10}],progress:[saved]);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.RecordHeartbeatAsync(1,new(){VideoId=100,PositionSeconds=100,PlaybackState="ended"}));
    }

    [Fact]
    public async Task Normal_Playback_Tolerates_Fractional_Heartbeat_Timing()
    {
        var saved = new VideoProgress { EmployeeId=1, TrainingVideoId=100, PlaybackState="playing", UpdatedAt=DateTime.UtcNow.AddSeconds(-1.8) };
        var service=Service(assignments:[new(){EmployeeId=1,TrainingProgramId=10}],progress:[saved]);
        var result=await service.RecordHeartbeatAsync(1,new(){VideoId=100,PositionSeconds=2,PlaybackState="playing"});
        Assert.Equal(2,result.AcceptedPositionSeconds);
        Assert.True(saved.PlaybackCreditSeconds < 0);
    }

    [Fact]
    public async Task Delayed_Heartbeat_Within_Session_Limit_Is_Accepted()
    {
        var saved=new VideoProgress { EmployeeId=1,TrainingVideoId=100,PlaybackState="playing",UpdatedAt=DateTime.UtcNow.AddSeconds(-12) };
        var service=Service(assignments:[new(){EmployeeId=1,TrainingProgramId=10}],progress:[saved]);
        Assert.Equal(12,(await service.RecordHeartbeatAsync(1,new(){VideoId=100,PositionSeconds=12,PlaybackState="playing"})).AcceptedPositionSeconds);
    }

    [Fact]
    public async Task Timing_Margin_Cannot_Be_Reused_To_Manufacture_Progress()
    {
        var saved=new VideoProgress { EmployeeId=1,TrainingVideoId=100,PlaybackState="paused",UpdatedAt=DateTime.UtcNow };
        var service=Service(assignments:[new(){EmployeeId=1,TrainingProgramId=10}],progress:[saved]);
        await service.RecordHeartbeatAsync(1,new(){VideoId=100,PositionSeconds=1,PlaybackState="paused"});
        Assert.Equal(-1,saved.PlaybackCreditSeconds);
        await service.OpenVideoAsync(1,100);
        Assert.Equal(-1,saved.PlaybackCreditSeconds);
        await service.RecordHeartbeatAsync(1,new(){VideoId=100,PositionSeconds=1,PlaybackState="paused"});
        await Assert.ThrowsAsync<ITC.TrainingPortal.Domain.Exceptions.PlaybackTimingException>(()=>service.RecordHeartbeatAsync(1,new(){VideoId=100,PositionSeconds=2,PlaybackState="paused"}));
    }

    private static TrainingPlaybackService Service(TrainingAssignment[]? assignments = null, VideoProgress[]? progress = null) =>
        new(new FakeRepository<TrainingAssignment>(assignments ?? []),
            new FakeRepository<TrainingProgram>(new TrainingProgram { Id = 10, Title = "Safety" }),
            new FakeRepository<TrainingVideo>(
                new TrainingVideo { Id = 100, TrainingProgramId = 10, Title = "One", SequenceOrder = 1, DurationSeconds = 100, VideoUrl = "https://example.com/one.mp4" },
                new TrainingVideo { Id = 101, TrainingProgramId = 10, Title = "Two", SequenceOrder = 2, DurationSeconds = 100, VideoUrl = "https://example.com/two.mp4" }),
            new FakeRepository<VideoProgress>(progress ?? []),
            new FakeRepository<QuizResult>());
}
