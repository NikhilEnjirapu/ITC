using System.Net;
using System.Text.RegularExpressions;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Fact]
    public async Task Empty_Catalog_Provides_Setup_Actions_Instead_Of_Empty_Forms()
    {
        using var factory = new LocalFactory();
        await factory.AddIsolatedTestEmployees();
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        var dashboard = await client.GetStringAsync("/admin");
        Assert.Contains("Create your first program", dashboard);
        foreach (var path in new[] { "training-assignments", "training-videos", "exams" })
        {
            var html = await client.GetStringAsync($"/admin/{path}/Create");
            Assert.Contains("Complete setup first", html);
            Assert.Contains("/admin/training-programs/Create", html);
            Assert.DoesNotContain("name=\"TrainingProgramId\"", html);
        }
        var create = await client.GetStringAsync("/admin/training-programs/Create");
        Assert.DoesNotContain("<select", create);
        Assert.Contains("Save and add videos", create);
    }

    [Fact]
    public async Task Assignment_Post_Rejects_Draft_And_Excluded_Employee_Then_Accepts_Ready_Program()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 0);
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        var html = await client.GetStringAsync("/admin/training-assignments/Create?programId=10&employeeId=1");
        Assert.Matches("<option[^>]*selected=\"selected\"[^>]*value=\"10\"", html);
        var fields = ProgramFormFields(html, "", "");
        fields["EmployeeId"] = "1"; fields["TrainingProgramId"] = "11";
        using var blocked = await client.PostAsync("/admin/training-assignments/Create", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, blocked.StatusCode);
        Assert.Contains("Finish program setup", await blocked.Content.ReadAsStringAsync());
        fields["TrainingProgramId"] = "10";
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Employees.Add(new Employee { Id=3, EmployeeId="T0003", WorkerType="Contractor", Email="excluded@example.test", DepartmentId=1 });
            await db.SaveChangesAsync();
        }
        fields["EmployeeId"] = "3";
        using var excluded = await client.PostAsync("/admin/training-assignments/Create", new FormUrlEncodedContent(fields));
        Assert.Contains("Select an eligible employee", await excluded.Content.ReadAsStringAsync());
        fields["EmployeeId"] = "1";
        using var accepted = await client.PostAsync("/admin/training-assignments/Create", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, accepted.StatusCode);
        using var verify = factory.Services.CreateScope();
        Assert.Equal(2, await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingAssignments.CountAsync());
    }

    [Fact]
    public async Task Started_Assignment_Cannot_Move_And_Readiness_Requires_Questions()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 1);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var workflow = scope.ServiceProvider.GetRequiredService<AdminWorkflowService>();
        var assignment = await db.TrainingAssignments.SingleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.ValidateAssignmentAsync(1,10,assignment.Id));
        await workflow.ValidateAssignmentAsync(2,10,assignment.Id);
        Assert.True((await workflow.ProgramsAsync()).Single(x => x.Id == 10).IsReady);
        db.QuizQuestions.RemoveRange(await db.QuizQuestions.ToListAsync());
        await db.SaveChangesAsync();
        Assert.Equal("Add exam questions", (await workflow.ProgramsAsync()).Single(x => x.Id == 10).Missing);
    }

    [Fact]
    public async Task Video_Form_Has_One_Program_Selector_And_Suggests_Next_Order()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 0);
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        var html = await client.GetStringAsync("/admin/training-videos/Create?programId=10");
        Assert.Single(Regex.Matches(html, "<select[^>]*name=\"TrainingProgramId\""));
        Assert.Equal("4", ProgramFormFields(html, "", "")["SequenceOrder"]);
        html = await client.GetStringAsync("/admin/exams/10/questions/add");
        Assert.Equal("2", ProgramFormFields(html, "", "")["SequenceOrder"]);
    }

    [Fact]
    public async Task Draft_Exam_Requires_Questions_Then_Allows_Availability_Activation_And_Bulk_Assignment()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 0);
        using var client = Browser(factory); using var login = await SignIn(client,"admin");
        var fields = ProgramFormFields(await client.GetStringAsync("/admin/exams/Create?programId=11"), "New exam", "");
        fields["TrainingProgramId"]="11"; fields["PassingScorePercentage"]="80"; fields["IsActive"]="true";
        using var created=await client.PostAsync("/admin/exams/Create",new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect,created.StatusCode);
        int examId;
        using(var scope=factory.Services.CreateScope())
        {
            var exam=await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Quizzes.SingleAsync(x=>x.TrainingProgramId==11);
            examId=exam.Id; Assert.False(exam.IsActive);
        }
        var edit=$"/admin/exams/Edit/{examId}";
        fields=ProgramFormFields(await client.GetStringAsync(edit),"New exam","");
        fields["TrainingProgramId"]="11";fields["IsActive"]="true";
        using var premature=await client.PostAsync(edit,new FormUrlEncodedContent(fields));
        Assert.Contains("before activating",await premature.Content.ReadAsStringAsync());
        var questionUrl=$"/admin/exams/{examId}/questions/add";
        var question=ProgramFormFields(await client.GetStringAsync(questionUrl),"","");
        question["QuestionText"]="Ready?";question["OptionA"]="Yes";question["OptionB"]="No";question["OptionC"]="Maybe";question["OptionD"]="Unknown";question["CorrectOption"]="A";
        using var added=await client.PostAsync(questionUrl,new FormUrlEncodedContent(question));
        Assert.Equal(HttpStatusCode.Redirect,added.StatusCode);
        var availability="/admin/training-programs/Availability/11";
        var unlock=ProgramFormFields(await client.GetStringAsync(availability),"","");
        unlock["ExamUnlockCondition"]="1";unlock["ExamUnlockVideoId"]="201";
        using var configured=await client.PostAsync(availability,new FormUrlEncodedContent(unlock));
        Assert.Equal(HttpStatusCode.Redirect,configured.StatusCode);
        using var activated=await client.PostAsync(edit,new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect,activated.StatusCode);
        var assign=ProgramFormFields(await client.GetStringAsync("/admin/training-assignments/Create?programId=11"),"","");
        assign["TrainingProgramId"]="11";
        var batch=assign.ToList();batch.Add(new("employeeIds","1"));batch.Add(new("employeeIds","2"));
        using var assigned=await client.PostAsync("/admin/training-assignments/Create",new FormUrlEncodedContent(batch));
        Assert.Equal(HttpStatusCode.Redirect,assigned.StatusCode);
        using var verification=factory.Services.CreateScope();
        var db=verification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2,await db.TrainingAssignments.CountAsync(x=>x.TrainingProgramId==11));
        Assert.Equal(201,(await db.TrainingPrograms.FindAsync(11))!.ExamUnlockVideoId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Reorder_Respects_Existing_Employee_Progress(int completed)
    {
        using var factory=new LocalFactory();await SeedUnlockProgram(factory,ExamUnlockCondition.AllVideos,completed);
        using var client=Browser(factory);using var login=await SignIn(client,"admin");
        var html=await client.GetStringAsync("/admin/training-programs/Details/10");
        using var response=await client.PostAsync("/admin/training-videos/Reorder",new FormUrlEncodedContent(new Dictionary<string,string>{["id"]="102",["direction"]="up",["__RequestVerificationToken"]=Token(html)}));
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        using var verification=factory.Services.CreateScope();
        var db=verification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(completed==0?1:2,(await db.TrainingVideos.FindAsync(102))!.SequenceOrder);
        Assert.Equal(completed==0?2:1,(await db.TrainingVideos.FindAsync(101))!.SequenceOrder);
    }

    [Fact]
    public async Task Eligibility_Preview_Does_Not_Save_And_Employee_Delete_Explains_History()
    {
        using var factory=new LocalFactory();await SeedUnlockProgram(factory,ExamUnlockCondition.AllVideos,1);
        using var client=Browser(factory);using var login=await SignIn(client,"admin");
        var html=await client.GetStringAsync("/admin/eligibility/Create");
        using var preview=await client.PostAsync("/admin/eligibility/Save",new FormUrlEncodedContent(new Dictionary<string,string>{["RuleType"]="DepartmentId",["RuleValue"]="1",["IsActive"]="true",["preview"]="true",["__RequestVerificationToken"]=Token(html)}));
        Assert.Contains("2 additional employees",await preview.Content.ReadAsStringAsync());
        html=await client.GetStringAsync("/admin/employees/Delete/2");
        using var deleted=await client.PostAsync("/admin/employees/Delete/2",Form(Token(html)));
        Assert.Contains("cannot be deleted",await deleted.Content.ReadAsStringAsync());
        using var verification=factory.Services.CreateScope();var db=verification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.EligibilityRules.ToListAsync());Assert.Equal(2,await db.Employees.CountAsync());
    }
}
