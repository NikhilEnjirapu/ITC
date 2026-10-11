using System.Net;
using System.Text.RegularExpressions;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Fact]
    public async Task Admin_Can_Follow_Program_Video_Exam_Question_Unlock_Workflow()
    {
        using var factory = new LocalFactory();
        await factory.AddIsolatedTestEmployees();
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        var html = await client.GetStringAsync("/admin/training-programs/Create");
        using var created = await client.PostAsync("/admin/training-programs/Create", new FormUrlEncodedContent(ProgramFormFields(html, "Workflow program", "Setup")));
        int programId;
        using(var scope = factory.Services.CreateScope()) programId = (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingPrograms.SingleAsync()).Id;
        var programUrl = $"/admin/training-programs/Details/{programId}";
        Assert.Equal(programUrl, created.Headers.Location?.OriginalString);
        html = await client.GetStringAsync(programUrl);
        Assert.Contains("Add Video", html);
        Assert.DoesNotContain($"/admin/exams/Create?programId={programId}", html);

        html = await client.GetStringAsync($"/admin/training-videos/Create?programId={programId}");
        Assert.Contains($"href=\"{programUrl}\"", html);
        var fields = ProgramFormFields(html, "First video", "");
        fields["TrainingProgramId"] = programId.ToString(); fields["SequenceOrder"] = "1";
        fields["DurationSeconds"] = "100"; fields["VideoUrl"] = "existing.mp4";
        using var video = await client.PostAsync("/admin/training-videos/Create", new FormUrlEncodedContent(fields));
        Assert.Equal(programUrl, video.Headers.Location?.OriginalString);
        html = await client.GetStringAsync(programUrl);
        Assert.Contains("First video", html); Assert.Contains("Add Another Video", html);
        Assert.Contains($"/admin/exams/Create?programId={programId}", html);
        int videoId;
        using(var scope = factory.Services.CreateScope()) videoId = (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingVideos.SingleAsync()).Id;
        html = await client.GetStringAsync($"/admin/training-videos/Edit/{videoId}?fromProgram=true");
        Assert.Contains($"href=\"{programUrl}\"", html);

        html = await client.GetStringAsync($"/admin/exams/Create?programId={programId}");
        Assert.Matches($"<option[^>]*selected=\"selected\"[^>]*value=\"{programId}\"", html);
        fields = ProgramFormFields(html, "Workflow exam", "");
        fields["TrainingProgramId"] = programId.ToString(); fields["PassingScorePercentage"] = "80"; fields["IsActive"] = "true";
        using var exam = await client.PostAsync("/admin/exams/Create", new FormUrlEncodedContent(fields));
        int examId;
        using(var scope = factory.Services.CreateScope()) examId = (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Quizzes.SingleAsync()).Id;
        var examUrl = $"/admin/exams/{examId}/questions";
        Assert.Equal(examUrl, exam.Headers.Location?.OriginalString);
        html = await client.GetStringAsync(examUrl);
        Assert.Contains("Workflow program", html); Assert.Contains("Add question", html);
        Assert.Contains("Back to Program", html); Assert.Contains("Configure Exam Unlock", html);
        html = await client.GetStringAsync(programUrl);
        Assert.Contains("Manage Exam", html);
        Assert.Matches($"href=\"{examUrl}\">Workflow exam</a>", html);
        html = await client.GetStringAsync($"/admin/exams/{examId}/questions/add");
        fields = ProgramFormFields(html, "", "");
        fields["QuizId"] = examId.ToString(); fields["QuestionText"] = "Safe?";
        fields["OptionA"]="Yes"; fields["OptionB"]="No"; fields["OptionC"]="Maybe"; fields["OptionD"]="Unknown";
        fields["CorrectOption"]="A"; fields["SequenceOrder"]="1";
        using var question = await client.PostAsync($"/admin/exams/{examId}/questions/add", new FormUrlEncodedContent(fields));
        Assert.Equal(examUrl, question.Headers.Location?.OriginalString);
        foreach(var condition in new[]{"1","0"})
        {
            html=await client.GetStringAsync($"/admin/training-programs/Edit/{programId}");
            fields=ProgramFormFields(html,"Workflow program","Setup");
            fields["ExamUnlockCondition"]=condition; fields["ExamUnlockVideoId"]=videoId.ToString();
            using var saved=await client.PostAsync($"/admin/training-programs/Edit/{programId}",new FormUrlEncodedContent(fields));
            Assert.Equal(programUrl,saved.Headers.Location?.OriginalString);
            using var scope=factory.Services.CreateScope();
            var program=await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingPrograms.FindAsync(programId);
            Assert.Equal(condition=="1" ? videoId : (int?)null,program!.ExamUnlockVideoId);
        }
    }

    [Fact]
    public async Task Contextual_Forms_Preserve_Context_On_Validation_Failure_And_Global_Entry_Remains()
    {
        using var factory=new LocalFactory(); await SeedUnlockProgram(factory,ExamUnlockCondition.AllVideos,0);
        using var client=Browser(factory); using var login=await SignIn(client,"admin");
        foreach(var controller in new[]{"training-videos","exams"})
        {
            var url=$"/admin/{controller}/Create";
            var html=await client.GetStringAsync(url+"?programId=10");
            var fields=ProgramFormFields(html,"",""); fields["TrainingProgramId"]="10";
            using var invalid=await client.PostAsync(url,new FormUrlEncodedContent(fields));
            Assert.Equal(HttpStatusCode.OK,invalid.StatusCode);
            Assert.Contains("name=\"returnProgramId\" value=\"10\"",await invalid.Content.ReadAsStringAsync());
            html=await client.GetStringAsync(url);
            Assert.DoesNotContain("name=\"returnProgramId\"",html);
            using var missing=await client.GetAsync(url+"?programId=99999"); Assert.Equal(HttpStatusCode.NotFound,missing.StatusCode);
        }
    }

    [Fact]
    public async Task Global_Video_Returns_To_List_And_Exam_Continues_To_Questions()
    {
        using var factory=new LocalFactory(); await SeedUnlockProgram(factory,ExamUnlockCondition.AllVideos,0);
        using var client=Browser(factory); using var login=await SignIn(client,"admin");
        var html=await client.GetStringAsync("/admin/training-videos/Create");
        var fields=ProgramFormFields(html,"Global video","");
        fields["TrainingProgramId"]="11";fields["SequenceOrder"]="2";fields["DurationSeconds"]="100";fields["VideoUrl"]="existing.mp4";
        using var video=await client.PostAsync("/admin/training-videos/Create",new FormUrlEncodedContent(fields));
        Assert.Equal("/admin/training-videos",video.Headers.Location?.OriginalString);
        html=await client.GetStringAsync("/admin/exams/Create");fields=ProgramFormFields(html,"Global exam","");
        fields["TrainingProgramId"]="11";fields["PassingScorePercentage"]="80";fields["IsActive"]="true";
        using var exam=await client.PostAsync("/admin/exams/Create",new FormUrlEncodedContent(fields));
        Assert.Matches(@"/admin/exams/\d+/questions",exam.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Active_Exam_Blocks_Content_Player_Media_And_Impersonated_Start()
    {
        using var factory=new LocalFactory(); await SeedUnlockProgram(factory,ExamUnlockCondition.SpecificVideo,2);
        using var client=Browser(factory); using var login=await SignIn(client,"employee");
        var html=await client.GetStringAsync("/employee/exams/10");
        using var start=await client.PostAsync("/employee/exams/10/start",Form(Token(html)));
        Assert.Equal(HttpStatusCode.OK,start.StatusCode);
        foreach(var path in new[]{"/employee/training/10","/employee/video/103"})
        { using var response=await client.GetAsync(path); Assert.Equal(HttpStatusCode.Redirect,response.StatusCode); }
        using var media=await client.GetAsync("/employee/media/103"); Assert.Equal(HttpStatusCode.Forbidden,media.StatusCode);
        using(var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Quizzes.Add(new(){Id=11,TrainingProgramId=11,Title="Unassigned",IsActive=true}); await db.SaveChangesAsync();
        }
        using var forged=await client.PostAsync("/employee/exams/11/start",new FormUrlEncodedContent(new Dictionary<string,string>{["__RequestVerificationToken"]=Token(html),["employeeId"]="1"}));
        Assert.Equal(HttpStatusCode.Redirect,forged.StatusCode);
        Assert.Contains("/access-denied",forged.Headers.Location!.OriginalString);
        using var verification=factory.Services.CreateScope();
        Assert.False(await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().QuizResults.AnyAsync(x=>x.QuizId==11));
    }
}
