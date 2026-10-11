using System.Net;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Theory]
    [InlineData(ExamUnlockCondition.AllVideos, 1, false)]
    [InlineData(ExamUnlockCondition.AllVideos, 2, false)]
    [InlineData(ExamUnlockCondition.AllVideos, 3, true)]
    [InlineData(ExamUnlockCondition.SpecificVideo, 1, false)]
    [InlineData(ExamUnlockCondition.SpecificVideo, 2, true)]
    public async Task Exam_Unlock_Is_Server_Enforced_Across_Refresh_And_Login(ExamUnlockCondition condition, int completed, bool unlocked)
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, condition, completed);
        using var client = Browser(factory);
        using var login = await SignIn(client, "employee");
        var path = "/employee/exams/10";
        foreach(var round in Enumerable.Range(0, 3))
        {
            var html = await client.GetStringAsync(path);
            Assert.Equal(unlocked, html.Contains(">Start Exam</button>"));
            if(!unlocked) Assert.Contains(condition == ExamUnlockCondition.SpecificVideo ? "Emergency Procedures" : "Complete all training videos", html);
            var availability = await client.GetStringAsync(path + "/availability");
            Assert.Equal(unlocked, availability.Contains("Start Exam"));
            if(round == 1)
            {
                using var logout = await client.PostAsync("/logout", Form(Token(html)));
                Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
                using var relogin = await SignIn(client, "employee");
                Assert.Equal(HttpStatusCode.Redirect, relogin.StatusCode);
            }
        }
        using(var scope = factory.Services.CreateScope())
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().QuizResults.ToListAsync());
        var introduction = await client.GetStringAsync(path);
        using var start = await client.PostAsync(path + "/start", Form(Token(introduction)));
        Assert.Equal(unlocked ? HttpStatusCode.OK : HttpStatusCode.Redirect, start.StatusCode);
        using var verification = factory.Services.CreateScope();
        Assert.Equal(unlocked ? 1 : 0, await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().QuizResults.CountAsync());
    }

    [Fact]
    public async Task Admin_Form_Filters_Orders_And_Validates_Unlock_Videos()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 0);
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        var html = await client.GetStringAsync("/admin/training-programs/Edit/10");
        Assert.Contains("When can employees take this exam?", html);
        Assert.DoesNotContain("Other program video", html);
        Assert.True(html.IndexOf("1. First video", StringComparison.Ordinal) < html.IndexOf("2. Emergency Procedures", StringComparison.Ordinal));
        Assert.True(html.IndexOf("2. Emergency Procedures", StringComparison.Ordinal) < html.IndexOf("3. Last video", StringComparison.Ordinal));
        foreach(var invalid in new[] { "", "201", "999" })
        {
            var fields = ProgramFormFields(html, "Safety", "");
            fields["ExamUnlockCondition"] = "1";
            fields["ExamUnlockVideoId"] = invalid;
            using var response = await client.PostAsync("/admin/training-programs/Edit/10", new FormUrlEncodedContent(fields));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Select an unlock video belonging to this training program", await response.Content.ReadAsStringAsync());
        }
        var valid = ProgramFormFields(html, "Safety", "");
        valid["ExamUnlockCondition"] = "1";
        valid["ExamUnlockVideoId"] = "102";
        using var saved = await client.PostAsync("/admin/training-programs/Edit/10", new FormUrlEncodedContent(valid));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        var details = await client.GetStringAsync("/admin/training-programs/Details/10");
        Assert.Contains("Exam Unlock", details);
        Assert.Contains("Emergency Procedures", details);
        html = await client.GetStringAsync("/admin/training-programs/Edit/10");
        valid = ProgramFormFields(html, "Safety", "");
        valid["ExamUnlockCondition"] = "0";
        valid["ExamUnlockVideoId"] = "102";
        using var cleared = await client.PostAsync("/admin/training-programs/Edit/10", new FormUrlEncodedContent(valid));
        Assert.Equal(HttpStatusCode.Redirect, cleared.StatusCode);
        using var scope = factory.Services.CreateScope();
        var program = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingPrograms.FindAsync(10);
        Assert.Null(program!.ExamUnlockVideoId);
        Assert.Equal(ExamUnlockCondition.AllVideos, program.ExamUnlockCondition);
    }

    [Fact]
    public async Task Availability_Updates_From_Persisted_Completion_Without_Starting_Attempt()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.SpecificVideo, 1);
        using var client = Browser(factory);
        using var login = await SignIn(client, "employee");
        Assert.DoesNotContain("Start Exam", await client.GetStringAsync("/employee/exams/10/availability"));
        using(var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.VideoProgresses.Add(new() { EmployeeId = 2, TrainingVideoId = 102, IsCompleted = true });
            await db.SaveChangesAsync();
        }
        Assert.Contains("Start Exam", await client.GetStringAsync("/employee/exams/10/availability"));
        Assert.Contains("Start Exam", await client.GetStringAsync("/employee/training/10"));
        using var verification = factory.Services.CreateScope();
        Assert.Empty(await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().QuizResults.ToListAsync());
    }

    private static async Task SeedUnlockProgram(LocalFactory factory, ExamUnlockCondition condition, int completed)
    {
        await factory.AddIsolatedTestEmployees();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.TrainingPrograms.AddRange(new TrainingProgram { Id = 10, Title = "Safety", ExamUnlockCondition = condition,
            ExamUnlockVideoId = condition == ExamUnlockCondition.SpecificVideo ? 102 : null }, new TrainingProgram { Id = 11, Title = "Other" });
        // Deliberately insert out of sequence to verify the dropdown's order.
        foreach(var n in new[] { 3, 1, 2 }) db.TrainingVideos.Add(new() { Id = 100 + n, TrainingProgramId = 10, SequenceOrder = n,
            Title = n == 1 ? "First video" : n == 2 ? "Emergency Procedures" : "Last video", VideoUrl = "v.mp4", DurationSeconds = 100 });
        db.TrainingVideos.Add(new() { Id = 201, TrainingProgramId = 11, SequenceOrder = 1, Title = "Other program video", VideoUrl = "v.mp4" });
        db.TrainingAssignments.Add(new() { EmployeeId = 2, TrainingProgramId = 10 });
        db.Quizzes.Add(new() { Id = 10, TrainingProgramId = 10, Title = "Safety exam", IsActive = true });
        db.QuizQuestions.Add(new() { Id = 20, QuizId = 10, SequenceOrder = 1, QuestionText = "Safe?", OptionA = "Yes", OptionB = "No", OptionC = "Maybe", OptionD = "Unknown", CorrectOption = "A" });
        for(var n = 1; n <= completed; n++) db.VideoProgresses.Add(new() { EmployeeId = 2, TrainingVideoId = 100 + n, IsCompleted = true });
        await db.SaveChangesAsync();
    }
}
