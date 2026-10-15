using System.Net;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Theory]
    [InlineData(true, 100)]
    [InlineData(false, 50)]
    public async Task Typed_Answer_Can_Be_Authored_Submitted_And_Manually_Graded(bool correct, int score)
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 3);
        using var admin = Browser(factory);
        using var employee = Browser(factory);
        using var adminLogin = await SignIn(admin, "admin");
        using var employeeLogin = await SignIn(employee, "employee");
        var form = await admin.GetStringAsync("/admin/exams/10/questions/add");
        using var added = await admin.PostAsync("/admin/exams/10/questions/add", new FormUrlEncodedContent(new Dictionary<string,string> {
            ["__RequestVerificationToken"] = Token(form), ["QuizId"] = "10", ["QuestionText"] = "Explain the procedure", ["SequenceOrder"] = "2", ["IsTypedAnswer"] = "true"
        }));
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
        var intro = await employee.GetStringAsync("/employee/exams/10");
        using var started = await employee.PostAsync("/employee/exams/10/start", Form(Token(intro)));
        var attemptHtml = await started.Content.ReadAsStringAsync();
        Assert.Contains("textarea", attemptHtml);
        int attemptId, questionId;
        using(var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            attemptId = (await db.QuizResults.SingleAsync()).Id;
            questionId = (await db.QuizQuestions.SingleAsync(x => x.IsTypedAnswer)).Id;
        }
        using var submitted = await employee.PostAsync("/employee/exams/submit", new FormUrlEncodedContent(new Dictionary<string,string> {
            ["__RequestVerificationToken"] = Token(attemptHtml), ["attemptId"] = attemptId.ToString(), ["answers[20]"] = "A", [$"answers[{questionId}]"] = "Isolate the equipment first."
        }));
        Assert.Equal(HttpStatusCode.Redirect, submitted.StatusCode);
        Assert.Contains("Awaiting review", await employee.GetStringAsync($"/employee/results/{attemptId}"));
        using(var scope = factory.Services.CreateScope())
        {
            var row = Assert.Single(await scope.ServiceProvider.GetRequiredService<ComplianceReportService>().GetAsync(new()));
            Assert.Equal("Awaiting review", row.Exam);
            Assert.Null(row.Score);
        }
        using var forbidden = await employee.GetAsync($"/admin/exam-reviews/{attemptId}");
        Assert.Equal(HttpStatusCode.Redirect, forbidden.StatusCode);
        Assert.Contains("access-denied", forbidden.Headers.Location!.ToString());
        var review = await admin.GetStringAsync($"/admin/exam-reviews/{attemptId}");
        Assert.Contains("Isolate the equipment first.", review);
        using var noToken = await admin.PostAsync($"/admin/exam-reviews/{attemptId}", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
        var fields = new Dictionary<string,string> { ["__RequestVerificationToken"] = Token(review), ["rowVersion"] = "" };
        using var missing = await admin.PostAsync($"/admin/exam-reviews/{attemptId}", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, missing.StatusCode);
        fields[$"marks[{questionId}]"] = correct.ToString();
        using var graded = await admin.PostAsync($"/admin/exam-reviews/{attemptId}", new FormUrlEncodedContent(fields));
        Assert.True(graded.StatusCode == HttpStatusCode.Redirect, string.Join(" | ", System.Text.RegularExpressions.Regex.Matches(await graded.Content.ReadAsStringAsync(), "<li>.*?</li>").Select(x => x.Value)));
        using var duplicate = await admin.PostAsync($"/admin/exam-reviews/{attemptId}", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var verification = factory.Services.CreateScope();
        var saved = await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().QuizResults.SingleAsync();
        Assert.False(saved.RequiresReview);
        Assert.Equal(score, saved.ScorePercentage);
        Assert.Equal(correct, saved.IsPassed);
        Assert.NotNull(saved.ReviewedAt);
        Assert.False(string.IsNullOrWhiteSpace(saved.ReviewedBy));
        Assert.Contains($"{score}%", await employee.GetStringAsync($"/employee/results/{attemptId}"));
    }
}
