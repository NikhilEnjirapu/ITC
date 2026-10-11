using ITC.TrainingPortal.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ITC.TrainingPortal.Web.Controllers;
public sealed class AccountController:Controller
{
    private readonly EntraIdOptions _options;
    private readonly IWebHostEnvironment _environment;
    public AccountController(IOptions<EntraIdOptions> options, IWebHostEnvironment environment)
    { _options = options.Value; _environment = environment; }
    [HttpGet("/login")] public IActionResult Login(string? message=null)
    {
        ViewData["Message"] = message;
        ViewData["EntraEnabled"] = _options.Enabled;
        ViewData["DevelopmentLoginEnabled"] = _environment.IsDevelopment() && !_options.Enabled;
        return View();
    }
    [HttpGet("/access-denied")] public IActionResult AccessDenied()=>View();
    [HttpGet("/login/sso")] public IActionResult SignIn()=>_options.Enabled?new ChallengeResult(OpenIdConnectDefaults.AuthenticationScheme,new AuthenticationProperties{RedirectUri="/"}):new RedirectToActionResult(nameof(Login),"Account",new{message="Corporate sign-in is not configured yet."});
    [HttpPost("/logout"), ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        if (_options.Enabled)
            return new SignOutResult([CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme], new AuthenticationProperties { RedirectUri = "/" });
        if (_environment.IsDevelopment())
            return new SignOutResult([CookieAuthenticationDefaults.AuthenticationScheme], new AuthenticationProperties { RedirectUri = "/login" });
        return new RedirectToActionResult(nameof(Login), "Account", null);
    }
}
