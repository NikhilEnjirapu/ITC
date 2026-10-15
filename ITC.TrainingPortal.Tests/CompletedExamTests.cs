using System.Net;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Fact]
    public async Task Results_Page_Retains_Completed_Exams_Without_Assignment()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 3);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.TrainingAssignments.RemoveRange(db.TrainingAssignments.Where(x => x.EmployeeId == 2));
            db.QuizResults.Add(new QuizResult { EmployeeId = 2, QuizId = 10, IsCompleted = true, IsPassed = true, ScorePercentage = 90 });
            await db.SaveChangesAsync();
        }
        using var client = Browser(factory);
        using var login = await SignIn(client, "employee");
        Assert.Contains("90% - Passed", await client.GetStringAsync("/employee/results"));
        using var denied = await client.GetAsync("/employee/exams/10");
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
    }

    [Fact]
    public async Task Active_Exam_Can_Be_Resumed_From_Ui_After_Assignment_Removal()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 3);
        using var client = Browser(factory);
        using var login = await SignIn(client, "employee");
        var intro = await client.GetStringAsync("/employee/exams/10");
        using var started = await client.PostAsync("/employee/exams/10/start", Form(Token(intro)));
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.TrainingAssignments.RemoveRange(db.TrainingAssignments.Where(x => x.EmployeeId == 2));
            (await db.Quizzes.FindAsync(10))!.IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Contains("Resume exam", await client.GetStringAsync("/employee/exams"));
        intro = await client.GetStringAsync("/employee/exams/10");
        Assert.Contains("Resume exam</button>", intro);
        using var resumed = await client.PostAsync("/employee/exams/10/start", Form(Token(intro)));
        Assert.Equal(HttpStatusCode.OK, resumed.StatusCode);
    }

    [Fact]
    public async Task Best_Of_Three_Keeps_Earlier_Pass_In_Results_And_Reports_And_Blocks_Fourth()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 3);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            foreach (var (score, index) in new[] { 90, 40, 60 }.Select((score, index) => (score, index)))
                db.QuizResults.Add(new QuizResult { EmployeeId = 2, QuizId = 10, IsCompleted = true,
                    IsPassed = score >= 80, ScorePercentage = score,
                    AttemptDate = DateTime.UtcNow.AddMinutes(index - 3), CompletedAt = DateTime.UtcNow.AddMinutes(index - 3) });
            await db.SaveChangesAsync();
            var rows = await scope.ServiceProvider.GetRequiredService<ITC.TrainingPortal.Web.Services.ComplianceReportService>()
                .GetAsync(new() { EmployeeId = 2 });
            var row = Assert.Single(rows);
            Assert.Equal(90, row.Score);
            Assert.Equal("Passed", row.Exam);
            Assert.Equal("Completed", row.Status);
        }
        using var client = Browser(factory);
        using var login = await SignIn(client, "employee");
        var results = await client.GetStringAsync("/employee/results");
        Assert.Contains("Best of three attempts", results);
        Assert.Contains("90% - Passed", results);
        Assert.Contains("40% - Failed", results);
        Assert.Contains("60% - Failed", results);
        Assert.Contains("3 of 3 attempts used", results);
        var intro = await client.GetStringAsync("/employee/exams/10");
        using var fourth = await client.PostAsync("/employee/exams/10/start", Form(Token(intro)));
        Assert.Equal(HttpStatusCode.Redirect, fourth.StatusCode);
        using var verification = factory.Services.CreateScope();
        Assert.Equal(3, await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().QuizResults.CountAsync());
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Completed_Exam_Shows_Best_Result_And_Allows_Another_Attempt_When_Active(bool passed, bool active)
    {
        using var factory=new LocalFactory();
        await SeedUnlockProgram(factory,ExamUnlockCondition.AllVideos,3);
        int resultId;
        using(var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var result=new QuizResult{EmployeeId=2,QuizId=10,IsCompleted=true,IsPassed=passed,ScorePercentage=passed?100:0,AttemptDate=DateTime.UtcNow,CompletedAt=DateTime.UtcNow};
            db.QuizResults.Add(result);(await db.Quizzes.FindAsync(10))!.IsActive=active;
            await db.SaveChangesAsync();resultId=result.Id;
        }
        using var client=Browser(factory);using var login=await SignIn(client,"employee");
        foreach(var path in new[]{"/employee/training","/employee/results","/employee/exams/10/availability"})
        {
            var html=await client.GetStringAsync(path);
            Assert.Contains($"/employee/results/{resultId}",html);
        }
        var intro=await client.GetStringAsync("/employee/exams/10");
        Assert.Contains("1 of 3 used", intro);
        using var start=await client.PostAsync("/employee/exams/10/start",Form(Token(intro)));
        Assert.Equal(active ? HttpStatusCode.OK : HttpStatusCode.Redirect,start.StatusCode);
        using var verification=factory.Services.CreateScope();
        Assert.Equal(active ? 2 : 1,await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().QuizResults.CountAsync());
    }
}
