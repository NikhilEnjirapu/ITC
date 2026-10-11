using System.Net;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Theory]
    [InlineData("empty")]
    [InlineData("video")]
    [InlineData("assignment")]
    [InlineData("exam")]
    [InlineData("all")]
    public async Task Program_Delete_Removes_Linked_Records(string dependency)
    {
        using var factory = new LocalFactory();
        await factory.AddIsolatedTestEmployees();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.TrainingPrograms.Add(new TrainingProgram { Id = 10, Title = "Deletion test" });
            db.TrainingPrograms.Add(new TrainingProgram { Id = 11, Title = "Keep" });
            db.TrainingVideos.Add(new TrainingVideo { Id = 21, TrainingProgramId = 11, Title = "Keep video", VideoUrl = "keep.mp4", SequenceOrder = 1 });
            db.TrainingAssignments.Add(new TrainingAssignment { EmployeeId = 2, TrainingProgramId = 11 });
            db.Quizzes.Add(new Quiz { Id = 31, TrainingProgramId = 11, Title = "Keep exam" });
            db.VideoProgresses.Add(new VideoProgress { EmployeeId = 2, TrainingVideoId = 21 });
            db.QuizResults.Add(new QuizResult { EmployeeId = 2, QuizId = 31, IsCompleted = true });
            db.QuizQuestions.Add(new QuizQuestion { QuizId = 31, QuestionText = "Keep question" });
            if (dependency == "all")
            {
                db.TrainingVideos.Add(new TrainingVideo { Id = 20, TrainingProgramId = 10, Title = "Video", VideoUrl = "test.mp4", SequenceOrder = 1 });
                db.TrainingAssignments.Add(new TrainingAssignment { EmployeeId = 2, TrainingProgramId = 10 });
                db.Quizzes.Add(new Quiz { Id = 30, TrainingProgramId = 10, Title = "Exam" });
                db.VideoProgresses.Add(new VideoProgress { EmployeeId = 2, TrainingVideoId = 20 });
                db.QuizResults.Add(new QuizResult { EmployeeId = 2, QuizId = 30, IsCompleted = true });
                db.QuizQuestions.Add(new QuizQuestion { QuizId = 30, QuestionText = "Delete question" });
                var program = await db.TrainingPrograms.FindAsync(10);
                program!.ExamUnlockCondition = ExamUnlockCondition.SpecificVideo;
                program.ExamUnlockVideoId = 20;
            }
            if (dependency == "video") db.TrainingVideos.Add(new TrainingVideo { Id = 20, TrainingProgramId = 10, Title = "Video", VideoUrl = "test.mp4", SequenceOrder = 1 });
            if (dependency == "assignment") db.TrainingAssignments.Add(new TrainingAssignment { EmployeeId = 2, TrainingProgramId = 10 });
            if (dependency == "exam") db.Quizzes.Add(new Quiz { TrainingProgramId = 10, Title = "Exam" });
            await db.SaveChangesAsync();
        }

        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        const string url = "/admin/training-programs/Delete/10";
        var html = await client.GetStringAsync(url);
        Assert.Contains("Deletion test", html);
        Assert.Contains("This permanently deletes", html);
        Assert.Matches("<form[^>]*method=\"post\"", html);
        using (var scope = factory.Services.CreateScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingPrograms.AnyAsync(x => x.Id == 10));

        using var invalid = await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string> { ["Id"] = "10" }));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var response = await client.PostAsync(url, Form(Token(html)));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/admin/training-programs", response.Headers.Location?.OriginalString);
            using var missing = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var verification = factory.Services.CreateScope();
        var remaining = verification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(11, (await remaining.TrainingPrograms.SingleAsync()).Id);
        Assert.Equal(21, (await remaining.TrainingVideos.SingleAsync()).Id);
        Assert.Equal(11, (await remaining.TrainingAssignments.SingleAsync()).TrainingProgramId);
        Assert.Equal(31, (await remaining.Quizzes.SingleAsync()).Id);
        Assert.Equal(21, (await remaining.VideoProgresses.SingleAsync()).TrainingVideoId);
        Assert.Equal(31, (await remaining.QuizResults.SingleAsync()).QuizId);
        Assert.Equal(31, (await remaining.QuizQuestions.SingleAsync()).QuizId);
        Assert.Equal(2, await remaining.Employees.CountAsync());
    }
}
