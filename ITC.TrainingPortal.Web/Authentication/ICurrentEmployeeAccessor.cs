using System.Security.Claims;
using ITC.TrainingPortal.Application.Identity;

namespace ITC.TrainingPortal.Web.Authentication;

public interface ICurrentEmployeeAccessor
{
    Task<EmployeeIdentityResolution> ResolveAsync(ClaimsPrincipal principal);
}
