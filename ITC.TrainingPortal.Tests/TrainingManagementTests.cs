using System.ComponentModel.DataAnnotations;
using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Domain.Exceptions;

namespace ITC.TrainingPortal.Tests;

public class TrainingManagementTests
{
    [Fact]
    public void TrainingVideoDto_RejectsInvalidUrlAndSequence()
    {
        var dto=new TrainingVideoDto{Title="Video",VideoUrl="",SequenceOrder=0,TrainingProgramId=0};var results=Validate(dto);
        Assert.Contains(results,x=>x.MemberNames.Contains(nameof(dto.VideoUrl)));Assert.Contains(results,x=>x.MemberNames.Contains(nameof(dto.SequenceOrder)));
    }

    [Fact]
    public void TrainingAssignmentDto_RejectsDueDateBeforeAssignment()
    {
        var dto=new TrainingAssignmentDto{EmployeeId=1,TrainingProgramId=1,AssignedAt=new DateTime(2026,1,2),DueDate=new DateTime(2026,1,1)};
        Assert.Contains(Validate(dto),x=>x.MemberNames.Contains(nameof(dto.DueDate)));
    }

    [Fact]
    public async Task TrainingVideoService_RejectsDuplicateSequenceWithinProgram()
    {
        var videos=new FakeRepository<TrainingVideo>(new TrainingVideo{Id=1,TrainingProgramId=2,SequenceOrder=1});
        var programs=new FakeRepository<TrainingProgram>(new TrainingProgram{Id=2,Title="Program"});
        var service=new TrainingVideoService(videos,programs,new FakeRepository<VideoProgress>());
        var dto=new TrainingVideoDto{Title="Second",VideoUrl="video.mp4",SequenceOrder=1,TrainingProgramId=2};
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.CreateAsync(dto));
    }

    [Fact]
    public async Task TrainingVideoService_AllowsSameSequenceInDifferentPrograms()
    {
        var videos=new FakeRepository<TrainingVideo>(new TrainingVideo{Id=1,TrainingProgramId=1,SequenceOrder=1});
        var programs=new FakeRepository<TrainingProgram>(new TrainingProgram{Id=1,Title="One"},new TrainingProgram{Id=2,Title="Two"});
        var service=new TrainingVideoService(videos,programs,new FakeRepository<VideoProgress>());
        await service.CreateAsync(new TrainingVideoDto{Title="Video",VideoUrl="video.mp4",SequenceOrder=1,TrainingProgramId=2});
        Assert.Equal(2,(await videos.GetAllAsync()).Count());
    }

    [Fact]
    public async Task TrainingAssignmentService_RejectsDuplicateEmployeeProgramAssignment()
    {
        var assignments=new FakeRepository<TrainingAssignment>(new TrainingAssignment{Id=1,EmployeeId=2,TrainingProgramId=3});
        var service=new TrainingAssignmentService(assignments,new FakeRepository<Employee>(new Employee{Id=2}),new FakeRepository<Department>(),new FakeRepository<TrainingProgram>(new TrainingProgram{Id=3}),new FakeRepository<TrainingVideo>(),new FakeRepository<VideoProgress>(),new FakeRepository<Quiz>());
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.CreateAsync(new TrainingAssignmentDto{EmployeeId=2,TrainingProgramId=3}));
    }

    [Fact]
    public async Task TrainingProgramService_RejectsStaleUpdate()
    {
        var programs=new FakeRepository<TrainingProgram>(new TrainingProgram{Id=1,Title="Program",RowVersion=[2]});
        var service=new TrainingProgramService(programs,new FakeRepository<TrainingVideo>(),new FakeRepository<TrainingAssignment>(),new FakeRepository<Quiz>(),null!);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(()=>service.UpdateAsync(new TrainingProgramDto{Id=1,Title="Changed",RowVersion=[1]}));
    }

    [Fact]
    public async Task EmployeeCatalog_UsesOnlyAssignedProgramsAndActualCompletedProgress()
    {
        var service = new TrainingAssignmentService(
            new FakeRepository<TrainingAssignment>(new TrainingAssignment { Id=1, EmployeeId=7, TrainingProgramId=10 }),
            new FakeRepository<Employee>(new Employee { Id=7 }), new FakeRepository<Department>(),
            new FakeRepository<TrainingProgram>(new TrainingProgram { Id=10, Title="Assigned" }, new TrainingProgram { Id=11, Title="Not assigned" }),
            new FakeRepository<TrainingVideo>(new TrainingVideo { Id=20, TrainingProgramId=10 }, new TrainingVideo { Id=21, TrainingProgramId=10 }, new TrainingVideo { Id=22, TrainingProgramId=11 }),
            new FakeRepository<VideoProgress>(new VideoProgress { EmployeeId=7, TrainingVideoId=20, IsCompleted=true }),
            new FakeRepository<Quiz>(new Quiz { TrainingProgramId=10, Title="Related exam" }));

        var catalog = await service.GetEmployeeTrainingAsync(7);

        var item = Assert.Single(catalog);
        Assert.Equal("Assigned", item.TrainingProgramTitle);
        Assert.Equal(2, item.VideoCount);
        Assert.Equal(1, item.CompletedVideoCount);
        Assert.Equal(50, item.ProgressPercentage);
        Assert.Equal("Exam configured", item.ExamStatus);
    }

    private static List<ValidationResult> Validate(object value){var results=new List<ValidationResult>();Validator.TryValidateObject(value,new ValidationContext(value),results,true);return results;}
}
