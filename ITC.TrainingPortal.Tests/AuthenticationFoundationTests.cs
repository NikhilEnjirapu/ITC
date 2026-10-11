using System.Security.Claims;
using ITC.TrainingPortal.Application.Identity;
using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Web.Authentication;
using ITC.TrainingPortal.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using ITC.TrainingPortal.Web.Controllers;

namespace ITC.TrainingPortal.Tests;

public class AuthenticationFoundationTests
{
    [Fact]
    public async Task CurrentEmployeeAccessor_FailsClosed_WhenMatchingIsNotConfigured()
    {
        var resolver = new StubIdentityResolver();
        var accessor = new CurrentEmployeeAccessor(resolver, Options.Create(new EmployeeIdentityOptions()));
        var principal = AuthenticatedPrincipal(new Claim("any", "value"));

        var result = await accessor.ResolveAsync(principal);

        Assert.Equal(EmployeeIdentityResolutionStatus.NotConfigured, result.Status);
        Assert.Equal(0, resolver.CallCount);
    }

    [Fact]
    public async Task CurrentEmployeeAccessor_UsesOnlyTheConfiguredClaim()
    {
        var resolver = new StubIdentityResolver();
        var options = new EmployeeIdentityOptions { MatchMode = "Email", EmailClaimType = "client-email-claim" };
        var accessor = new CurrentEmployeeAccessor(resolver, Options.Create(options));
        var principal = AuthenticatedPrincipal(new Claim("client-email-claim", "user@example.com"));

        await accessor.ResolveAsync(principal);

        Assert.Equal(EmployeeIdentityMatchMode.Email, resolver.MatchMode);
        Assert.Equal("user@example.com", resolver.Identifier);
    }

    [Fact]
    public async Task EmployeeIdentityResolver_ResolvesEmployeeByEmail()
    {
        var employees = new FakeRepository<Employee>(new Employee
        {
            Id = 3, EmployeeId = "E3", Email = "user@example.com", DepartmentId = 4
        });
        var departments = new FakeRepository<Department>(new Department { Id = 4, Name = "Operations" });
        var resolver = new EmployeeIdentityResolver(employees, departments);

        var result = await resolver.ResolveAsync(EmployeeIdentityMatchMode.Email, " user@example.com ");

        Assert.Equal(EmployeeIdentityResolutionStatus.Resolved, result.Status);
        Assert.Equal("Operations", result.Employee?.DepartmentName);
    }

    [Fact]
    public async Task PortalRoleHandler_FailsClosed_WhenRoleMappingIsEmpty()
    {
        var handler = new PortalRoleAuthorizationHandler(Options.Create(new PortalAuthorizationOptions()));
        var requirement = new PortalRoleRequirement(PortalRole.Administrator);
        var context = new AuthorizationHandlerContext([requirement], AuthenticatedPrincipal(new Claim("roles", "Admin")), null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task PortalRoleHandler_GrantsConfiguredRole()
    {
        var options = new PortalAuthorizationOptions
        {
            RoleClaimType = "client-role-claim", AdministratorRole = "PortalAdmins"
        };
        var handler = new PortalRoleAuthorizationHandler(Options.Create(options));
        var requirement = new PortalRoleRequirement(PortalRole.Administrator);
        var principal = AuthenticatedPrincipal(new Claim("client-role-claim", "PortalAdmins"));
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Theory]
    [InlineData(typeof(EmployeeController))]
    [InlineData(typeof(DepartmentController))]
    [InlineData(typeof(TrainingProgramController))]
    [InlineData(typeof(TrainingVideoController))]
    [InlineData(typeof(TrainingAssignmentController))]
    public void AdministratorControllers_Require_AdministratorPolicy(Type controller)
    {
        var attribute = Assert.Single(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(PortalPolicies.Administrator, attribute.Policy);
    }

    [Fact]
    public void EmployeeTrainingController_Requires_EmployeePolicy()
    {
        var attribute = Assert.Single(typeof(TrainingController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(PortalPolicies.Employee, attribute.Policy);
    }

    private static ClaimsPrincipal AuthenticatedPrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "TestAuthentication"));

    private sealed class StubIdentityResolver : IEmployeeIdentityResolver
    {
        public int CallCount { get; private set; }
        public EmployeeIdentityMatchMode MatchMode { get; private set; }
        public string? Identifier { get; private set; }

        public Task<EmployeeIdentityResolution> ResolveAsync(EmployeeIdentityMatchMode matchMode, string identifier)
        {
            CallCount++;
            MatchMode = matchMode;
            Identifier = identifier;
            return Task.FromResult(EmployeeIdentityResolution.EmployeeNotFound());
        }
    }
}
