using System.Net;
using ITC.TrainingPortal.Web.Authentication;
using ITC.TrainingPortal.Web.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ITC.TrainingPortal.Web.Controllers;

[Route("/login/dev")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DevelopmentLoginController(
    IWebHostEnvironment environment,
    IOptions<EntraIdOptions> entra,
    IOptions<PortalAuthorizationOptions> authorization,
    IOptions<EmployeeIdentityOptions> identity) : Controller
{
    // GET links lead to the explicit, CSRF-protected choices; GET never signs a user in.
    [HttpGet("admin"), HttpGet("employee")]
    public IActionResult Choose() => Available() ? RedirectToAction("Login", "Account") : NotFound();

    [HttpPost("admin"), ValidateAntiForgeryToken]
    public Task<IActionResult> Administrator() => SignInAsync(PortalRole.Administrator, "/admin");

    [HttpPost("employee"), ValidateAntiForgeryToken]
    public Task<IActionResult> Employee() => SignInAsync(PortalRole.Employee, "/employee");

    private bool Available() => environment.IsDevelopment() && !entra.Value.Enabled &&
        HttpContext.Connection.RemoteIpAddress is { } address && IPAddress.IsLoopback(address);

    private async Task<IActionResult> SignInAsync(PortalRole role, string destination)
    {
        if (!Available()) return NotFound();
        try
        {
            var principal = DevelopmentAuthentication.CreatePrincipal(role, authorization.Value, identity.Value);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = false, AllowRefresh = false, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) });
            return LocalRedirect(destination);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
