using System.Net;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Fact]
    public async Task Assignment_IST_Dates_RoundTrip_Through_UTC_And_Validation()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 0);
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        var html = await client.GetStringAsync("/admin/training-assignments/Create?programId=10&employeeId=1");
        Assert.Contains("India Standard Time (IST)", html);
        var fields = ProgramFormFields(html, "", "");
        fields["EmployeeId"] = "1"; fields["TrainingProgramId"] = "10";
        fields["AssignedAt"] = "2026-10-14T01:00";
        fields["DueDate"] = "2026-10-15T23:30";
        using var created = await client.PostAsync("/admin/training-assignments/Create", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        int id;
        using(var scope = factory.Services.CreateScope())
        {
            var saved = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingAssignments.SingleAsync(x => x.EmployeeId == 1);
            id = saved.Id;
            Assert.Equal(new DateTime(2026,10,13,19,30,0), saved.AssignedAt);
            Assert.Equal(new DateTime(2026,10,15,18,0,0), saved.DueDate);
        }
        html = await client.GetStringAsync($"/admin/training-assignments/Edit/{id}");
        fields = ProgramFormFields(html, "", "");
        Assert.Equal("2026-10-14T01:00", fields["AssignedAt"]);
        Assert.Equal("2026-10-15T23:30", fields["DueDate"]);
        fields["EmployeeId"] = "1"; fields["TrainingProgramId"] = "10";
        fields["DueDate"] = "2026-10-13T23:30";
        using var invalid = await client.PostAsync($"/admin/training-assignments/Edit/{id}", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Equal("2026-10-14T01:00", ProgramFormFields(await invalid.Content.ReadAsStringAsync(), "", "")["AssignedAt"]);
        fields["DueDate"] = "";
        using var edited = await client.PostAsync($"/admin/training-assignments/Edit/{id}", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, edited.StatusCode);
        using var verify = factory.Services.CreateScope();
        var updated = await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingAssignments.FindAsync(id);
        Assert.Equal(new DateTime(2026,10,13,19,30,0), updated!.AssignedAt);
        Assert.Null(updated.DueDate);
    }
}
