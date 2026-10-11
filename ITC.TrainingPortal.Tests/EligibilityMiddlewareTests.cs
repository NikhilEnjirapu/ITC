using System.Security.Claims;
using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Identity;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed class EligibilityMiddlewareTests
{
    [Theory]
    [InlineData("T123", "Employee")]
    [InlineData("123", "Intern")]
    [InlineData("123", "Unknown")]
    public async Task Excluded_Authenticated_Account_Is_Denied_And_Audited(string id, string type)
    {
        await using var db = Database();
        db.Employees.Add(new Employee { Id = 1, EmployeeId = id, WorkerType = type }); await db.SaveChangesAsync();
        var called = false;
        var middleware = new EligibilityMiddleware(_ => { called = true; return Task.CompletedTask; });
        var context = Context();
        await middleware.InvokeAsync(context, new Accessor(), db);
        Assert.False(called);
        Assert.Equal(403, context.Response.StatusCode);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "AccessDenied" && x.EmployeeId == id);
    }

    [Fact]
    public async Task New_Exclusion_Blocks_An_Existing_SignedIn_Session()
    {
        await using var db = Database();
        db.Employees.Add(new Employee { Id = 1, EmployeeId = "123", WorkerType = "Employee" }); await db.SaveChangesAsync();
        var calls = 0;
        var middleware = new EligibilityMiddleware(_ => { calls++; return Task.CompletedTask; });
        await middleware.InvokeAsync(Context(), new Accessor(), db);
        Assert.Equal(1, calls);
        db.EligibilityRules.Add(new EligibilityRule { RuleType = "EmployeeId", RuleValue = "123" }); await db.SaveChangesAsync();
        await middleware.InvokeAsync(Context(), new Accessor(), db);
        Assert.Equal(1, calls);
    }

    private static ApplicationDbContext Database() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static DefaultHttpContext Context()
    {
        var services = new ServiceCollection().AddSingleton<IAuthenticationService, AuthenticationStub>().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services, User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "employee")], "test")) };
        context.Request.Method = "POST"; context.Request.Path = "/employee/video/heartbeat";
        return context;
    }
    private sealed class Accessor : ICurrentEmployeeAccessor
    {
        public Task<EmployeeIdentityResolution> ResolveAsync(ClaimsPrincipal principal) => Task.FromResult(EmployeeIdentityResolution.Resolved(new EmployeeDto { Id = 1 }));
    }
    private sealed class AuthenticationStub : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    }
}
