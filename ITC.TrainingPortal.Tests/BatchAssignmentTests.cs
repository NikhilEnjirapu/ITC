using System.Net;
using System.Text;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Batch_Preview_Classifies_And_Confirmation_Assigns_Only_Valid_Employees(bool csv)
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 0);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Employees.Add(new() { Id = 3, EmployeeId = "T0003", FirstName = "Excluded", WorkerType = "Contractor", Email = "excluded@example.test", DepartmentId = 1 });
            await db.SaveChangesAsync();
        }
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        var form = await client.GetStringAsync("/admin/training-assignments/Create?programId=10");
        Assert.Contains("Select all matching employees", form);
        Assert.Contains("Download CSV template", form);
        var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(form), ["TrainingProgramId"] = "10",
            ["AssignedAt"] = "2026-10-14T09:00", ["DueDate"] = "2026-10-20T17:00", ["selectionMode"] = csv ? "csv" : "filter" };
        HttpContent content;
        if (csv)
        {
            var multipart = new MultipartFormDataContent();
            foreach (var field in fields) multipart.Add(new StringContent(field.Value), field.Key);
            multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("EmployeeId\r\n900001\r\n900002\r\nT0003\r\nmissing\r\n900001\r\n")), "csvFile", "employees.csv");
            content = multipart;
        }
        else
        {
            var posted = fields.ToList();
            foreach(var id in new[] { "1", "2", "3", "999", "1" }) posted.Add(new("employeeIds", id));
            content = new FormUrlEncodedContent(posted);
        }
        using (content)
        using (var response = await client.PostAsync("/admin/training-assignments/Preview", content))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var preview = await response.Content.ReadAsStringAsync();
            Assert.Contains("1 ready to assign", preview);
            Assert.Contains("Already assigned", preview);
            Assert.Contains("Employee ID not found", preview);
            Assert.Contains("Duplicate in selection", preview);
            using(var scope = factory.Services.CreateScope())
                Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingAssignments.CountAsync());
            var confirmation = ProgramFormFields(preview, "", "");
            using var saved = await client.PostAsync("/admin/training-assignments/Confirm", new FormUrlEncodedContent(confirmation));
            Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
            // Replaying a valid preview never creates duplicate assignments.
            using var replay = await client.PostAsync("/admin/training-assignments/Confirm", new FormUrlEncodedContent(confirmation));
            Assert.Equal(HttpStatusCode.Redirect, replay.StatusCode);
            using var verify = factory.Services.CreateScope();
            var db = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(2, await db.TrainingAssignments.CountAsync());
            var assignment = await db.TrainingAssignments.SingleAsync(x => x.EmployeeId == 1);
            Assert.Equal(new DateTime(2026, 10, 14, 3, 30, 0), assignment.AssignedAt);
            Assert.Equal(new DateTime(2026, 10, 20, 11, 30, 0), assignment.DueDate);
        }
    }

    [Fact]
    public async Task Batch_Rejects_Invalid_Dates_And_Tokens_And_Rechecks_Eligibility()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 0);
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        var html = await client.GetStringAsync("/admin/training-assignments/Create");
        var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(html), ["TrainingProgramId"] = "10",
            ["AssignedAt"] = "2026-10-14T09:00", ["DueDate"] = "2026-10-13T09:00", ["selectionMode"] = "filter", ["employeeIds"] = "1" };
        using var invalid = await client.PostAsync("/admin/training-assignments/Preview", new FormUrlEncodedContent(fields));
        Assert.Contains("Due date cannot be earlier", await invalid.Content.ReadAsStringAsync());
        fields["DueDate"] = "";
        using var preview = await client.PostAsync("/admin/training-assignments/Preview", new FormUrlEncodedContent(fields));
        var confirmation = ProgramFormFields(await preview.Content.ReadAsStringAsync(), "", "");
        var validToken = confirmation["token"];
        confirmation["token"] = "tampered";
        using var tampered = await client.PostAsync("/admin/training-assignments/Confirm", new FormUrlEncodedContent(confirmation));
        Assert.Equal(HttpStatusCode.BadRequest, tampered.StatusCode);
        confirmation["token"] = validToken;
        // Assign the same employee elsewhere between preview and confirmation.
        using(var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.TrainingAssignments.Add(new() { EmployeeId = 1, TrainingProgramId = 10 });
            await db.SaveChangesAsync();
        }
        using var saved = await client.PostAsync("/admin/training-assignments/Confirm", new FormUrlEncodedContent(confirmation));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        using var employee = Browser(factory);
        using var employeeLogin = await SignIn(employee, "employee");
        using var denied = await employee.GetAsync("/admin/training-assignments/CsvTemplate");
        Assert.Contains("access-denied", denied.Headers.Location!.ToString());
        using var verify = factory.Services.CreateScope();
        Assert.Equal(2, await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingAssignments.CountAsync());
    }

    [Theory]
    [InlineData("Name\nSomeone", false)]
    [InlineData("EmployeeId\n", false)]
    [InlineData("EmployeeId\n\"unclosed", false)]
    [InlineData("EmployeeId\n1,2", false)]
    [InlineData("EmployeeId,Name\n\"00123\",\"Name, Example\"", true)]
    public void Batch_Csv_Validates_Format_And_Preserves_Leading_Zeros(string csv, bool valid)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        if (valid) Assert.Equal("00123", Assert.Single(BatchAssignmentService.ReadCsv(stream)));
        else Assert.Throws<InvalidOperationException>(() => BatchAssignmentService.ReadCsv(stream));
    }
}
