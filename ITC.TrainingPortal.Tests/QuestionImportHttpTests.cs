using System.Net;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Fact]
    public async Task Csv_Import_Requires_Admin_And_Antiforgery_And_Validates_Before_Saving()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 3);
        using var admin = Browser(factory);
        using var employee = Browser(factory);
        using var login = await SignIn(admin, "admin");
        using var employeeLogin = await SignIn(employee, "employee");
        const string url = "/admin/exams/10/questions/import";
        using var forbidden = await employee.GetAsync(url);
        Assert.Equal(HttpStatusCode.Redirect, forbidden.StatusCode);
        Assert.Contains("access-denied", forbidden.Headers.Location!.ToString());
        var html = await admin.GetStringAsync(url);
        var token = Token(html);
        using var noToken = await admin.PostAsync(url, Upload(null, QuestionCsvReader.Template));
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
        var valid = "Describe isolation.,Typed,,,,,,isolate;power supply,2";
        using var invalid = await admin.PostAsync(url, Upload(token, QuestionCsvReader.Header + "\n" + valid + "\nBad,MCQ,A,B,C,D,Z,,3"));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Contains("correct option", await invalid.Content.ReadAsStringAsync());
        using (var scope = factory.Services.CreateScope())
            Assert.Single(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().QuizQuestions.ToListAsync());
        using var imported = await admin.PostAsync(url, Upload(token, QuestionCsvReader.Header + "\n" + valid));
        Assert.Equal(HttpStatusCode.Redirect, imported.StatusCode);
        using var verification = factory.Services.CreateScope();
        var questions = await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().QuizQuestions.ToListAsync();
        Assert.Equal(2, questions.Count);
        Assert.Equal("isolate;power supply", questions.Single(x => x.IsTypedAnswer).AnswerKeywords);
        using var repeated = await admin.PostAsync(url, Upload(token, QuestionCsvReader.Header + "\n" + valid));
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Contains("order must be unique", await repeated.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Manual_Review_Preserves_Automatically_Awarded_Keyword_Marks()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 3);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.QuizQuestions.AddRange(
                new QuizQuestion { Id = 21, QuizId = 10, SequenceOrder = 2, QuestionText = "Explain", IsTypedAnswer = true, AnswerKeywords = "isolate" },
                new QuizQuestion { Id = 22, QuizId = 10, SequenceOrder = 3, QuestionText = "Legacy", IsTypedAnswer = true });
            await db.SaveChangesAsync();
        }
        using var employee = Browser(factory);
        using var employeeLogin = await SignIn(employee, "employee");
        var intro = await employee.GetStringAsync("/employee/exams/10");
        using var started = await employee.PostAsync("/employee/exams/10/start", Form(Token(intro)));
        var attemptHtml = await started.Content.ReadAsStringAsync();
        Assert.DoesNotContain("isolate", attemptHtml);
        int attemptId;
        using (var scope = factory.Services.CreateScope())
            attemptId = (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().QuizResults.SingleAsync()).Id;
        using var submitted = await employee.PostAsync("/employee/exams/submit", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["__RequestVerificationToken"] = Token(attemptHtml), ["attemptId"] = attemptId.ToString(),
            ["answers[20]"] = "A", ["answers[21]"] = "Isolate equipment", ["answers[22]"] = "My explanation"
        }));
        Assert.Equal(HttpStatusCode.Redirect, submitted.StatusCode);
        using var admin = Browser(factory);
        using var login = await SignIn(admin, "admin");
        var review = await admin.GetStringAsync($"/admin/exam-reviews/{attemptId}");
        Assert.Contains("Keyword marking: Correct", review);
        Assert.DoesNotContain("name=\"marks[21]\"", review);
        using var published = await admin.PostAsync($"/admin/exam-reviews/{attemptId}", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["__RequestVerificationToken"] = Token(review), ["rowVersion"] = "", ["marks[22]"] = "true"
        }));
        Assert.Equal(HttpStatusCode.Redirect, published.StatusCode);
        using var verification = factory.Services.CreateScope();
        var result = await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().QuizResults.SingleAsync();
        Assert.Equal(100, result.ScorePercentage);
        Assert.False(result.RequiresReview);
    }

    private static MultipartFormDataContent Upload(string? token, string csv)
    {
        var form = new MultipartFormDataContent();
        if (token != null) form.Add(new StringContent(token), "__RequestVerificationToken");
        form.Add(new StringContent(csv, System.Text.Encoding.UTF8, "text/csv"), "file", "questions.csv");
        return form;
    }
}
