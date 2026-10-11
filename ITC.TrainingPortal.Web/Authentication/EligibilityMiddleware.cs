using ITC.TrainingPortal.Application.Identity;
using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ITC.TrainingPortal.Web.Authentication;

public sealed class EligibilityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentEmployeeAccessor accessor, ApplicationDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated != true || context.Request.Path == "/access-denied" || context.Request.Path == "/logout")
        { await next(context); return; }
        var resolution = await accessor.ResolveAsync(context.User);
        var employee = resolution.Status == EmployeeIdentityResolutionStatus.Resolved
            ? await db.Employees.FindAsync(resolution.Employee!.Id) : null;
        var reason = employee == null ? "Employee identity could not be resolved." :
            EligibilityEvaluator.DenialReason(employee, await db.EligibilityRules.AsNoTracking().Where(x => x.IsActive).ToListAsync());
        db.AuditUserId = context.User.Identity.Name;
        db.AuditEmployeeId = employee?.EmployeeId;
        db.AuditIpAddress = context.Connection.RemoteIpAddress?.ToString();
        if (reason != null)
        {
            db.AuditLogs.Add(new AuditLog { Action = "AccessDenied", UserId = db.AuditUserId,
                EmployeeId = employee?.EmployeeId, Details = reason, IpAddress = db.AuditIpAddress });
            await db.SaveChangesAsync();
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (context.Request.Method == "GET" && !context.Request.Path.StartsWithSegments("/employee/media")) context.Response.Redirect("/access-denied");
            else context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        // These records include viewing and assessment reads; mutations are captured atomically by DbContext.
        db.AuditLogs.Add(new AuditLog { Action = "PortalAccess", UserId = db.AuditUserId, EmployeeId = db.AuditEmployeeId,
            IpAddress = db.AuditIpAddress, Details = $"{context.Request.Method} {context.Request.Path}" });
        await db.SaveChangesAsync();
        await next(context);
    }
}
