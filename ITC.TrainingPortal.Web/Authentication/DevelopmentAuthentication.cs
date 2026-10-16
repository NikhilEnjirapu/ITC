using System.Security.Claims;
using ITC.TrainingPortal.Application.Identity;
using ITC.TrainingPortal.Web.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ITC.TrainingPortal.Web.Authentication;

public static class DevelopmentAuthentication
{
    public const string CookieName = ".ITC.TrainingPortal.Development";
    public const string AdminEmail = "local-admin@training-portal.test";
    public const string EmployeeEmail = "local-employee@training-portal.test";
    public const string AdminUserId = "training-portal-dev-admin";
    public const string EmployeeUserId = "training-portal-dev-employee";

    public static string TestEmployeeEmail(int number) => number == 1 ? EmployeeEmail : $"local-employee-{number}@training-portal.test";
    public static string TestEmployeeUserId(int number) => number == 1 ? EmployeeUserId : $"training-portal-dev-employee-{number}";

    public static ClaimsPrincipal CreatePrincipal(PortalRole role, PortalAuthorizationOptions authorization, EmployeeIdentityOptions identity, int employeeNumber = 1)
    {
        if (employeeNumber is < 1 or > 5)
            throw new InvalidOperationException("Choose one of the five development employees.");
        if (authorization.AllowDevelopmentBypass)
            throw new InvalidOperationException("Development sign-in requires AllowDevelopmentBypass to remain false.");
        if (string.IsNullOrWhiteSpace(authorization.RoleClaimType) ||
            string.IsNullOrWhiteSpace(authorization.AdministratorRole) || string.IsNullOrWhiteSpace(authorization.EmployeeRole) ||
            string.Equals(authorization.AdministratorRole, authorization.EmployeeRole, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Configure distinct Administrator and Employee role values and a role claim type for local sign-in.");

        if (!Enum.TryParse<EmployeeIdentityMatchMode>(identity.MatchMode, true, out var mode) ||
            mode is not (EmployeeIdentityMatchMode.Email or EmployeeIdentityMatchMode.UserId))
            throw new InvalidOperationException("Development sign-in requires EmployeeIdentity matching by Email or UserId.");

        var administrator = role == PortalRole.Administrator;
        var claimType = mode == EmployeeIdentityMatchMode.Email ? identity.EmailClaimType : identity.UserIdClaimType;
        if (string.IsNullOrWhiteSpace(claimType) || claimType == authorization.RoleClaimType ||
            authorization.RoleClaimType is ClaimTypes.Name or ClaimTypes.NameIdentifier)
            throw new InvalidOperationException("Configure a distinct employee identifier claim and role claim for local sign-in.");

        var identifier = mode == EmployeeIdentityMatchMode.Email
            ? administrator ? AdminEmail : TestEmployeeEmail(employeeNumber)
            : administrator ? AdminUserId : TestEmployeeUserId(employeeNumber);
        var claims = new Dictionary<string, string>
        {
            [ClaimTypes.NameIdentifier] = administrator ? AdminUserId : TestEmployeeUserId(employeeNumber),
            [ClaimTypes.Name] = administrator ? "Local Development Administrator" : $"Local Development Employee {employeeNumber}",
            [authorization.RoleClaimType] = administrator ? authorization.AdministratorRole : authorization.EmployeeRole,
            [claimType] = identifier
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims.Select(x => new Claim(x.Key, x.Value)),
            CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, authorization.RoleClaimType));
    }
}
