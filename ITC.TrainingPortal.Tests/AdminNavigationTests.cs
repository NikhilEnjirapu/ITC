using System.Net;
using System.Text.RegularExpressions;
using ITC.TrainingPortal.Domain.Entities;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Fact]
    public async Task Admin_Navigation_Follows_Workflow_And_Progress_Redirect_Preserves_Filters()
    {
        using var factory=new LocalFactory();
        await SeedUnlockProgram(factory,ExamUnlockCondition.AllVideos,0);
        using var client=Browser(factory);using var login=await SignIn(client,"admin");
        var expected=new[]{"Dashboard","Training Programs","Training Videos","Exams","Assignments","Departments","Employees","Eligibility Rules","Progress & Reports","Audit Logs"};
        foreach(var path in new[]{"/admin","/admin/training-programs","/admin/training-videos","/admin/exams","/admin/training-assignments","/admin/departments","/admin/employees","/admin/eligibility","/admin/reports","/admin/audit"})
        {
            var html=await client.GetStringAsync(path);
            var sidebar=Regex.Match(html,"<aside[\\s\\S]*?</aside>").Value;
            var labels=Regex.Matches(sidebar,"<span class=\"nav-label\">([^<]+)</span>").Select(x=>WebUtility.HtmlDecode(x.Groups[1].Value)).ToArray();
            Assert.Equal(expected,labels);
            Assert.Single(Regex.Matches(sidebar,"class=\"nav-entry active\""));
        }
        using var old=await client.GetAsync("/admin/progress?TrainingProgramId=10&Status=Completed");
        Assert.Equal(HttpStatusCode.Redirect,old.StatusCode);
        var destination=old.Headers.Location!.OriginalString;
        Assert.StartsWith("/admin/reports?",destination);
        Assert.Contains("TrainingProgramId=10",destination);Assert.Contains("Status=Completed",destination);
        var report=await client.GetStringAsync(destination);
        Assert.Contains("Progress &amp; Reports",report);
        Assert.Contains("Export Excel",report);Assert.Contains("Export PDF",report);
    }
}
