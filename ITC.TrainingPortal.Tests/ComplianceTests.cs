using System.Text;
using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace ITC.TrainingPortal.Tests;

public sealed class ComplianceTests
{
    [Theory]
    [InlineData("T100", "Employee")]
    [InlineData("s100", "Employee")]
    [InlineData("C123", "Contractor")]
    [InlineData("123", "Contractor")]
    [InlineData("123", "ThirdParty")]
    [InlineData("123", "Intern")]
    [InlineData("123", "Temporary")]
    [InlineData("123", "Unknown")]
    [InlineData("", "Employee")]
    public void Mandatory_Exclusions_Cannot_Be_Disabled(string id, string workerType) =>
        Assert.NotNull(EligibilityEvaluator.DenialReason(new Employee { EmployeeId = id, WorkerType = workerType }, []));

    [Fact]
    public void Verified_Employee_Is_Eligible() => Assert.Null(EligibilityEvaluator.DenialReason(Employee(), []));

    [Theory]
    [InlineData("EmployeeIdPrefix", "12")]
    [InlineData("EmployeeId", "123")]
    [InlineData("EmailDomain", "example.com")]
    [InlineData("WorkerType", "Employee")]
    [InlineData("DepartmentId", "1")]
    public void Admin_Rules_Are_Evaluated_And_Deactivation_Takes_Effect(string type, string value)
    {
        var rule = new EligibilityRule { RuleType = type, RuleValue = value };
        Assert.NotNull(EligibilityEvaluator.DenialReason(Employee(), [rule]));
        rule.IsActive = false;
        Assert.Null(EligibilityEvaluator.DenialReason(Employee(), [rule]));
    }

    [Fact]
    public async Task Changes_Record_Actor_And_Before_After_Values()
    {
        await using var db = Database();
        db.AuditEmployeeId = "999"; db.AuditUserId = "administrator";
        var employee = Employee(); db.Employees.Add(employee); await db.SaveChangesAsync();
        employee.WorkerType = "Temporary"; await db.SaveChangesAsync();
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "Employee.Modified");
        Assert.Equal("999", audit.EmployeeId);
        Assert.Contains("Temporary", audit.Details);
        Assert.Contains("Before", audit.Details);
    }

    [Fact]
    public async Task Completion_Requires_All_Videos_And_Passed_Exam_And_Filters_Are_Applied()
    {
        await using var db = Database();
        db.Departments.Add(new Department { Id = 1, Name = "Operations" });
        db.Employees.Add(Employee());
        db.TrainingPrograms.Add(new TrainingProgram { Id = 1, Title = "Safety" });
        db.TrainingAssignments.Add(new TrainingAssignment { EmployeeId = 1, TrainingProgramId = 1 });
        db.TrainingVideos.Add(new TrainingVideo { Id = 1, TrainingProgramId = 1, DurationSeconds = 100 });
        db.VideoProgresses.Add(new VideoProgress { EmployeeId = 1, TrainingVideoId = 1, CurrentPositionSeconds = 100, IsCompleted = true });
        db.Quizzes.Add(new Quiz { Id = 1, TrainingProgramId = 1 });
        await db.SaveChangesAsync();
        var service = new ComplianceReportService(db);
        Assert.NotEqual("Completed", Assert.Single(await service.GetAsync(new())).Status);
        db.QuizResults.Add(new QuizResult { EmployeeId = 1, QuizId = 1, IsPassed = true, IsCompleted = true, ScorePercentage = 100 });
        await db.SaveChangesAsync();
        Assert.Equal("Completed", Assert.Single(await service.GetAsync(new())).Status);
        Assert.Empty(await service.GetAsync(new ReportFilter { EmployeeId = 2 }));
        Assert.Empty(await service.GetAsync(new ReportFilter { DepartmentId = 2 }));
        Assert.Empty(await service.GetAsync(new ReportFilter { Status = "Overdue" }));
    }

    [Fact]
    public async Task Compliance_Report_Shows_Highest_Score_Rather_Than_Latest_Score()
    {
        await using var db = Database();
        db.Departments.Add(new Department { Id = 1, Name = "Operations" });
        db.Employees.Add(Employee());
        db.TrainingPrograms.Add(new TrainingProgram { Id = 1, Title = "Safety" });
        db.TrainingAssignments.Add(new TrainingAssignment { EmployeeId = 1, TrainingProgramId = 1 });
        db.Quizzes.Add(new Quiz { Id = 1, TrainingProgramId = 1 });
        db.QuizResults.AddRange(
            new QuizResult { EmployeeId = 1, QuizId = 1, IsCompleted = true, IsPassed = true, ScorePercentage = 90, AttemptDate = DateTime.UtcNow.AddMinutes(-2) },
            new QuizResult { EmployeeId = 1, QuizId = 1, IsCompleted = true, ScorePercentage = 50, AttemptDate = DateTime.UtcNow.AddMinutes(-1) });
        await db.SaveChangesAsync();

        var row = Assert.Single(await new ComplianceReportService(db).GetAsync(new()));

        Assert.Equal(90, row.Score);
        Assert.Equal("Passed", row.Exam);
    }

    [Fact]
    public void Csv_Is_Utf8_And_User_Text_Is_Not_A_Formula()
    {
        var bytes = ReportExporter.Csv([Row("=1+1 < & Employee")]);
        Assert.True(bytes.Take(3).SequenceEqual(Encoding.UTF8.GetPreamble()));
        var csv = Encoding.UTF8.GetString(bytes);
        Assert.Contains("\"'=1+1 < & Employee\"", csv);
        Assert.Contains("\"Best score\"", csv);
    }

    private static Employee Employee() => new() { Id = 1, EmployeeId = "123", FirstName = "Test", LastName = "Employee", Email = "test@example.com", DepartmentId = 1, WorkerType = "Employee" };
    private static ApplicationDbContext Database() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static ComplianceRow Row(string name) => new("123", name, "Operations", "Safety", 1, 1, "Passed", 100, "Completed", null);
}
