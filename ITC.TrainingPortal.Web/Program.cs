using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Domain.Interfaces;
using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using ITC.TrainingPortal.Web.Authorization;
using ITC.TrainingPortal.Web.Authentication;
using ITC.TrainingPortal.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Keep setup/development runnable without depending on machine-wide Windows event-log permissions.
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration).ReadFrom.Services(services)
    .Enrich.FromLogContext().WriteTo.Console()
    .WriteTo.File(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Logs", "portal-.log"),
        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

// Add services to the container.
builder.Services.AddControllersWithViews(options => options.Conventions.Add(
    new DevelopmentLoginConvention(builder.Environment.IsDevelopment() && !builder.Configuration.GetValue<bool>("EntraId:Enabled"))));
builder.Services.AddScoped<ITC.TrainingPortal.Web.Services.ComplianceReportService>();
builder.Services.AddScoped<ITC.TrainingPortal.Web.Services.VideoUploadService>();
builder.Services.Configure<EntraIdOptions>(builder.Configuration.GetSection(EntraIdOptions.SectionName));
builder.Services.Configure<EmployeeIdentityOptions>(builder.Configuration.GetSection(EmployeeIdentityOptions.SectionName));
builder.Services.Configure<PortalAuthorizationOptions>(builder.Configuration.GetSection(PortalAuthorizationOptions.SectionName));

var entraOptions = builder.Configuration.GetSection(EntraIdOptions.SectionName).Get<EntraIdOptions>() ?? new();
var authorizationOptions = builder.Configuration.GetSection(PortalAuthorizationOptions.SectionName).Get<PortalAuthorizationOptions>() ?? new();
if (entraOptions.Enabled)
{
    builder.Services
        .AddAuthentication()
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(EntraIdOptions.SectionName));
    builder.Services.PostConfigure<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>(
        Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            var previous = options.Events.OnTokenValidated;
            options.Events.OnTokenValidated = async context =>
            {
                if (previous != null) await previous(context);
                if (context.Result != null) return;
                var services = context.HttpContext.RequestServices;
                var accessor = services.GetRequiredService<ICurrentEmployeeAccessor>();
                var db = services.GetRequiredService<ApplicationDbContext>();
                var resolution = await accessor.ResolveAsync(context.Principal!);
                var employee = resolution.Employee == null ? null : await db.Employees.FindAsync(resolution.Employee.Id);
                var reason = employee == null ? "Employee identity could not be resolved." :
                    EligibilityEvaluator.DenialReason(employee, await db.EligibilityRules.AsNoTracking().Where(x => x.IsActive).ToListAsync());
                db.AuditLogs.Add(new ITC.TrainingPortal.Domain.Entities.AuditLog
                {
                    Action = reason == null ? "LoginAllowed" : "LoginDenied", EmployeeId = employee?.EmployeeId,
                    UserId = context.Principal?.Identity?.Name, Details = reason ?? "Corporate authentication and eligibility checks passed.",
                    IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await db.SaveChangesAsync();
                if (reason != null) context.Fail("This account is not eligible for the training portal.");
            };
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse(); context.Response.Redirect("/access-denied"); return Task.CompletedTask;
            };
        });
}
else
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/access-denied";
        });
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<IDataProtectionProvider>((options, protection) =>
            {
                options.Cookie.Name = DevelopmentAuthentication.CookieName;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = false;
                // Separate ticket protection prevents a local test cookie being accepted by production or Entra.
                options.TicketDataFormat = new Microsoft.AspNetCore.Authentication.TicketDataFormat(
                    protection.CreateProtector("ITC.TrainingPortal.DevelopmentLogin", "Cookies", "v1"));
            });
    }
}
var configuredKeyPath = builder.Configuration["DataProtection:KeyPath"];
if (!string.IsNullOrWhiteSpace(configuredKeyPath))
{
    var protection = builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(configuredKeyPath));
    if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "ITC.TrainingPortal.Keys")));
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PortalPolicies.AuthenticatedUser, policy => policy.RequireAuthenticatedUser());
    if (!entraOptions.Enabled && builder.Environment.IsDevelopment() && authorizationOptions.AllowDevelopmentBypass)
    {
        options.AddPolicy(PortalPolicies.Administrator, policy => policy.RequireAssertion(_ => true));
        options.AddPolicy(PortalPolicies.Employee, policy => policy.RequireAssertion(_ => true));
        return;
    }
    options.AddPolicy(PortalPolicies.Administrator, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PortalRoleRequirement(PortalRole.Administrator));
    });
    options.AddPolicy(PortalPolicies.Employee, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PortalRoleRequirement(PortalRole.Employee));
    });
});
builder.Services.AddSingleton<IAuthorizationHandler, PortalRoleAuthorizationHandler>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Application Services
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ITrainingProgramService, TrainingProgramService>();
builder.Services.AddScoped<ITC.TrainingPortal.Web.Services.AdminWorkflowService>();
builder.Services.AddScoped<ITC.TrainingPortal.Web.Services.BatchAssignmentService>();
builder.Services.AddScoped<ITrainingProgramDeletion, TrainingProgramDeletion>();
builder.Services.AddScoped<ITrainingVideoService, TrainingVideoService>();
builder.Services.AddScoped<ITrainingAssignmentService, TrainingAssignmentService>();
builder.Services.AddScoped<ITrainingPlaybackService, TrainingPlaybackService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IEmployeeIdentityResolver, EmployeeIdentityResolver>();
builder.Services.AddScoped<ICurrentEmployeeAccessor, CurrentEmployeeAccessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<EligibilityMiddleware>();
app.UseAuthorization();
app.UseMiddleware<ITC.TrainingPortal.Web.Services.VideoUploadLimitMiddleware>();

app.MapControllers();

MapAdmin("employees", "Employee");
MapAdmin("departments", "Department");
MapAdmin("training-programs", "TrainingProgram");
MapAdmin("training-videos", "TrainingVideo");
MapAdmin("training-assignments", "TrainingAssignment");
MapAdmin("exams", "Quiz");
MapAdmin("progress", "Progress");
MapAdmin("reports", "Report");
MapAdmin("audit", "Audit");
MapAdmin("eligibility", "Eligibility");

void MapAdmin(string path, string controller) => app.MapControllerRoute(
    name: $"admin-{path}", pattern: $"admin/{path}/{{action=Index}}/{{id?}}", defaults: new { controller });

app.Run();

public partial class Program { }
