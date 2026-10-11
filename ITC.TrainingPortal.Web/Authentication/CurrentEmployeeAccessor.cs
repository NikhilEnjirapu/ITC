using System.Security.Claims;
using ITC.TrainingPortal.Application.Identity;
using Microsoft.Extensions.Options;

namespace ITC.TrainingPortal.Web.Authentication;

public sealed class CurrentEmployeeAccessor : ICurrentEmployeeAccessor
{
    private readonly IEmployeeIdentityResolver _resolver;
    private readonly EmployeeIdentityOptions _options;

    public CurrentEmployeeAccessor(
        IEmployeeIdentityResolver resolver,
        IOptions<EmployeeIdentityOptions> options)
    {
        _resolver = resolver;
        _options = options.Value;
    }

    public Task<EmployeeIdentityResolution> ResolveAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult(EmployeeIdentityResolution.Unauthenticated());
        }

        if (!Enum.TryParse<EmployeeIdentityMatchMode>(_options.MatchMode, true, out var matchMode) ||
            matchMode == EmployeeIdentityMatchMode.NotConfigured)
        {
            return Task.FromResult(EmployeeIdentityResolution.NotConfigured());
        }

        var claimType = matchMode == EmployeeIdentityMatchMode.UserId
            ? _options.UserIdClaimType
            : _options.EmailClaimType;

        if (string.IsNullOrWhiteSpace(claimType))
        {
            return Task.FromResult(EmployeeIdentityResolution.NotConfigured());
        }

        var identifier = principal.FindFirstValue(claimType);
        return string.IsNullOrWhiteSpace(identifier)
            ? Task.FromResult(EmployeeIdentityResolution.MissingIdentifier())
            : _resolver.ResolveAsync(matchMode, identifier);
    }
}
