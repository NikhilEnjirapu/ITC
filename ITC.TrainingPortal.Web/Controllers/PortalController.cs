using ITC.TrainingPortal.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITC.TrainingPortal.Web.Controllers;

public sealed class PortalController : Controller
{
    private readonly IAuthorizationService _authorization;
    public PortalController(IAuthorizationService authorization) => _authorization = authorization;

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true) return Redirect("/login");
        if ((await _authorization.AuthorizeAsync(User, PortalPolicies.Administrator)).Succeeded) return Redirect("/admin");
        if ((await _authorization.AuthorizeAsync(User, PortalPolicies.Employee)).Succeeded) return Redirect("/employee");
        return Redirect("/access-denied");
    }
}
