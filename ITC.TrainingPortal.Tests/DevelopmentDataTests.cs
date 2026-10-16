using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Web.Authentication;
using ITC.TrainingPortal.Web.Authorization;
using ITC.TrainingPortal.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ITC.TrainingPortal.Tests;

public class DevelopmentDataTests
{
    [Theory]
    [InlineData("Email", "preferred_username")]
    [InlineData("UserId", "local-user-id")]
    public void Five_Employees_Have_Distinct_Employee_Only_Identities(string mode, string claim)
    {
        var identifiers = new HashSet<string>();
        for (var number = 1; number <= 5; number++)
        {
            var principal = DevelopmentAuthentication.CreatePrincipal(PortalRole.Employee,
                new PortalAuthorizationOptions { RoleClaimType = "roles", AdministratorRole = "Administrator", EmployeeRole = "Employee" },
                new EmployeeIdentityOptions { MatchMode = mode, EmailClaimType = claim, UserIdClaimType = claim }, number);
            Assert.True(principal.IsInRole("Employee"));
            Assert.False(principal.IsInRole("Administrator"));
            Assert.True(identifiers.Add(principal.FindFirst(claim)!.Value));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Arbitrary_Employee_Selections_Are_Rejected(int number) =>
        Assert.Throws<InvalidOperationException>(() => DevelopmentAuthentication.CreatePrincipal(PortalRole.Employee,
            new PortalAuthorizationOptions(), new EmployeeIdentityOptions(), number));

    [Fact]
    public async Task Supplied_Assets_Seed_Complete_Assignments_And_Preserve_Progress_On_Rerun()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ITC.TrainingPortal.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var contentRoot = Path.Combine(directory.FullName, "ITC.TrainingPortal.Web");
        // Use the source video repository to avoid duplicating large files in this test.
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["VideoRepository:Path"] = Path.Combine(directory.FullName, "Videos") }).Build();
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await DevelopmentDataSeeder.SeedAsync(db, contentRoot, configuration);
        Assert.Equal(6, await db.Employees.CountAsync());
        Assert.Equal(3, await db.TrainingPrograms.CountAsync());
        Assert.Equal(3, await db.TrainingVideos.CountAsync());
        Assert.Equal(3, await db.Quizzes.CountAsync());
        Assert.Equal(30, await db.QuizQuestions.CountAsync());
        Assert.Equal(15, await db.TrainingAssignments.CountAsync());
        var assignment = await db.TrainingAssignments.FirstAsync();
        db.VideoProgresses.Add(new() { EmployeeId = assignment.EmployeeId, TrainingVideoId = (await db.TrainingVideos.FirstAsync()).Id, CurrentPositionSeconds = 42 });
        await db.SaveChangesAsync();
        await DevelopmentDataSeeder.SeedAsync(db, contentRoot, configuration);
        Assert.Equal(6, await db.Employees.CountAsync());
        Assert.Equal(3, await db.TrainingPrograms.CountAsync());
        Assert.Equal(30, await db.QuizQuestions.CountAsync());
        Assert.Equal(15, await db.TrainingAssignments.CountAsync());
        Assert.Equal(42, (await db.VideoProgresses.SingleAsync()).CurrentPositionSeconds);
    }
}
