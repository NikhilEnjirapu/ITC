using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ITC.TrainingPortal.Web.Authorization;

public sealed class PortalRoleAuthorizationHandler : AuthorizationHandler<PortalRoleRequirement>
{
    private readonly PortalAuthorizationOptions _options;

    public PortalRoleAuthorizationHandler(IOptions<PortalAuthorizationOptions> options)
    {
        _options = options.Value;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PortalRoleRequirement requirement)
    {
        var configuredRole = requirement.Role == PortalRole.Administrator
            ? _options.AdministratorRole
            : _options.EmployeeRole;

        if (context.User.Identity?.IsAuthenticated == true &&
            !string.IsNullOrWhiteSpace(_options.RoleClaimType) &&
            !string.IsNullOrWhiteSpace(configuredRole) &&
            context.User.Claims.Any(claim =>
                claim.Type == _options.RoleClaimType &&
                string.Equals(claim.Value, configuredRole, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
