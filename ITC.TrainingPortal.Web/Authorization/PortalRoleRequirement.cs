using Microsoft.AspNetCore.Authorization;

namespace ITC.TrainingPortal.Web.Authorization;

public enum PortalRole
{
    Administrator,
    Employee
}

public sealed record PortalRoleRequirement(PortalRole Role) : IAuthorizationRequirement;
