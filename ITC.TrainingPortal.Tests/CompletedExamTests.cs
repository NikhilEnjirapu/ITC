using System.Net;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Completed_Exam_Shows_Result_And_Cannot_Start_Again(bool passed, bool active)
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
            Assert.DoesNotContain(">Start Exam<",html);
        }
        using var intro=await client.GetAsync("/employee/exams/10");
        Assert.Equal($"/employee/results/{resultId}",intro.Headers.Location?.OriginalString);
        var page=await client.GetStringAsync("/employee/exams");
        using var start=await client.PostAsync("/employee/exams/10/start",Form(Token(page)));
        Assert.Equal(HttpStatusCode.Redirect,start.StatusCode);
        using var verification=factory.Services.CreateScope();
        Assert.Equal(1,await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().QuizResults.CountAsync());
    }
}
