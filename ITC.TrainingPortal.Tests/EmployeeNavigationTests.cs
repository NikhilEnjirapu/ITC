using System.Text.RegularExpressions;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Fact]
    public async Task Employee_Tabs_Have_Distinct_Pages_And_Contextual_Selection()
    {
        using var factory=new LocalFactory();
        await SeedUnlockProgram(factory,ExamUnlockCondition.AllVideos,3);
        using var client=Browser(factory);using var login=await SignIn(client,"employee");
        foreach(var path in new[]{"/employee","/employee/training","/employee/training/10","/employee/video/101","/employee/exams","/employee/results","/employee/progress"})
        {
            var html=await client.GetStringAsync(path);
            var sidebar=Regex.Match(html,"<aside[\\s\\S]*?</aside>").Value;
            Assert.Equal(5,Regex.Matches(sidebar,"class=\"nav-entry").Count);
            Assert.Single(Regex.Matches(sidebar,"class=\"nav-entry active\""));
            if(path.Contains("/training") || path.Contains("/video"))
                Assert.Contains("class=\"nav-entry active\" href=\"/employee/training\"",sidebar);
            if(path=="/employee") Assert.Contains("Learning dashboard",html);
            if(path=="/employee/results") Assert.Contains("No completed exams yet",html);
        }
        using(var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.QuizResults.Add(new QuizResult{EmployeeId=2,QuizId=10,IsCompleted=true,IsPassed=true,ScorePercentage=100});
            await db.SaveChangesAsync();
        }
        var exams=await client.GetStringAsync("/employee/exams");
        Assert.Contains("No pending exams",exams);
        Assert.DoesNotContain(">Start Exam<",exams);
        var results=await client.GetStringAsync("/employee/results");
        Assert.Contains("Safety exam",results);Assert.Contains("100%",results);
        Assert.Contains("All assigned training is complete",await client.GetStringAsync("/employee"));
    }
}
