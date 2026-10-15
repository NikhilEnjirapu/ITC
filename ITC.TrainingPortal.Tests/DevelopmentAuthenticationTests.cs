using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Web.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Theory]
    [InlineData("admin", "Administrator", DevelopmentAuthentication.AdminEmail, "/admin")]
    [InlineData("employee", "Employee", DevelopmentAuthentication.EmployeeEmail, "/employee")]
    public async Task SignIn_Issues_Real_Authenticated_Cookie_With_Only_The_Selected_Role(
        string choice, string role, string email, string destination)
    {
        using var factory = new LocalFactory();
        using var client = Browser(factory);
        using var response = await SignIn(client, choice);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(destination, response.Headers.Location?.OriginalString);
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = options.TicketDataFormat.Unprotect(CookieValue(response));
        Assert.NotNull(ticket);
        Assert.True(ticket.Principal.Identity!.IsAuthenticated);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, ticket.AuthenticationScheme);
        Assert.Equal(role, Assert.Single(ticket.Principal.FindAll("roles")).Value);
        Assert.Equal(email, ticket.Principal.FindFirstValue("preferred_username"));
        Assert.True(ticket.Principal.IsInRole(role));
        Assert.False(ticket.Properties.IsPersistent);
        Assert.False(ticket.Properties.AllowRefresh);
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/employees")]
    [InlineData("/admin/training-programs")]
    [InlineData("/admin/exams")]
    public async Task SignedIn_Admin_Can_Access_Admin_Pages(string path)
    {
        using var factory = new LocalFactory();
        await factory.AddIsolatedTestEmployees();
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        using var page = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
    }

    [Theory]
    [InlineData("/employee")]
    [InlineData("/employee/training")]
    [InlineData("/employee/exams")]
    [InlineData("/employee/results")]
    public async Task SignedIn_Employee_Can_Access_Employee_Pages(string path)
    {
        using var factory = new LocalFactory();
        await factory.AddIsolatedTestEmployees();
        using var client = Browser(factory);
        using var login = await SignIn(client, "employee");
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        using var page = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
    }

    [Theory]
    [InlineData("employee", "/admin")]
    [InlineData("admin", "/employee")]
    public async Task Signing_In_Does_Not_Grant_The_Other_Role(string choice, string path)
    {
        using var factory = new LocalFactory();
        await factory.AddIsolatedTestEmployees();
        using var client = Browser(factory);
        using var login = await SignIn(client, choice);
        using var denied = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Contains("/access-denied", denied.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("Production", "admin")]
    [InlineData("Production", "employee")]
    [InlineData("Staging", "admin")]
    [InlineData("Staging", "employee")]
    public async Task NonDevelopment_Routes_Return_404_For_Get_And_Post(string environment, string choice)
    {
        using var factory = new LocalFactory(environment);
        using var client = Browser(factory);
        using var get = await client.GetAsync("/login/dev/" + choice);
        using var post = await client.PostAsync("/login/dev/" + choice, new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        var html = await client.GetStringAsync("/login");
        Assert.DoesNotContain("Development Testing Only", html);
        Assert.DoesNotContain("/login/dev/", html);
    }

    [Fact]
    public async Task Entra_Enabled_Hides_Local_Controls_And_Removes_Development_Routes()
    {
        using var factory = new LocalFactory(entraEnabled: true);
        using var client = Browser(factory);
        var html = await client.GetStringAsync("/login");
        Assert.Contains("Sign in with Corporate Account", html);
        Assert.DoesNotContain("Development Testing Only", html);
        foreach (var choice in new[] { "admin", "employee" })
        {
            using var get = await client.GetAsync("/login/dev/" + choice);
            using var post = await client.PostAsync("/login/dev/" + choice, new FormUrlEncodedContent([]));
            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        }
    }

    [Fact]
    public async Task Logout_Deletes_The_Development_Cookie_And_Requires_Login_Again()
    {
        using var factory = new LocalFactory();
        await factory.AddIsolatedTestEmployees();
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        var token = Token(await client.GetStringAsync("/admin"));
        using var logout = await client.PostAsync("/logout", Form(token));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal("/login", logout.Headers.Location?.OriginalString);
        Assert.Contains(logout.Headers.GetValues("Set-Cookie"), value => value.StartsWith(DevelopmentAuthentication.CookieName + "=;") && value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        using var denied = await client.GetAsync("/admin");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Contains("/login?ReturnUrl=", denied.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Anonymous_Admin_Access_Is_Still_Protected_In_Development()
    {
        using var factory = new LocalFactory();
        using var client = Browser(factory);
        using var response = await client.GetAsync("/admin");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login?ReturnUrl=", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Get_Does_Not_Sign_In_And_Post_Requires_Antiforgery()
    {
        using var factory = new LocalFactory();
        using var client = Browser(factory);
        using var get = await client.GetAsync("/login/dev/admin");
        Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
        Assert.Equal("/login", get.Headers.Location?.OriginalString);
        Assert.False(get.Headers.Contains("Set-Cookie"));
        using var post = await client.PostAsync("/login/dev/admin", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
        Assert.False(post.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Remote_Connections_Cannot_Use_Local_SignIn()
    {
        using var factory = new LocalFactory(remoteAddress: IPAddress.Parse("192.0.2.10"));
        using var client = Browser(factory);
        using var response = await SignIn(client, "admin");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Theory]
    [InlineData("admin", "/admin")]
    [InlineData("employee", "/employee")]
    public async Task Missing_Employee_Record_Is_Not_Seeded_Or_Bypassed(string choice, string destination)
    {
        using var factory = new LocalFactory();
        using var client = Browser(factory);
        using var login = await SignIn(client, choice);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        using var denied = await client.GetAsync(destination);
        Assert.Equal("/access-denied", denied.Headers.Location?.OriginalString);
        using var scope = factory.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Employees.CountAsync());
    }

    [Theory]
    [InlineData("admin", "/admin")]
    [InlineData("employee", "/employee")]
    public async Task Configured_Custom_Roles_And_UserId_Matching_Are_Honored(string choice, string destination)
    {
        using var factory = new LocalFactory(overrides: new Dictionary<string, string?>
        {
            ["PortalAuthorization:RoleClaimType"] = "local-role",
            ["PortalAuthorization:AdministratorRole"] = "LocalAdmins",
            ["PortalAuthorization:EmployeeRole"] = "LocalEmployees",
            ["EmployeeIdentity:MatchMode"] = "UserId",
            ["EmployeeIdentity:UserIdClaimType"] = "local-user-id"
        });
        await factory.AddIsolatedTestEmployees();
        using var client = Browser(factory);
        using var login = await SignIn(client, choice);
        using var page = await client.GetAsync(destination);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = options.TicketDataFormat.Unprotect(CookieValue(login))!.Principal;
        Assert.Equal(choice == "admin" ? "LocalAdmins" : "LocalEmployees", principal.FindFirstValue("local-role"));
        Assert.Equal(choice == "admin" ? DevelopmentAuthentication.AdminUserId : DevelopmentAuthentication.EmployeeUserId, principal.FindFirstValue("local-user-id"));
    }

    [Fact]
    public async Task Development_Cookie_Cannot_Be_Reused_As_A_Production_Cookie_Even_With_Shared_Keys()
    {
        var keys = new EphemeralDataProtectionProvider();
        using var development = new LocalFactory(protection: keys);
        using var browser = Browser(development);
        using var login = await SignIn(browser, "admin");
        using var production = new LocalFactory("Production", protection: keys);
        var productionOptions = production.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        Assert.Null(productionOptions.TicketDataFormat.Unprotect(CookieValue(login)));
        Assert.NotEqual(DevelopmentAuthentication.CookieName, productionOptions.Cookie.Name);
    }

    [Fact]
    public async Task Misconfigured_Roles_Do_Not_Issue_A_Cookie()
    {
        using var factory = new LocalFactory(overrides: new Dictionary<string, string?> { ["PortalAuthorization:EmployeeRole"] = "Administrator" });
        using var client = Browser(factory);
        using var response = await SignIn(client, "admin");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Admin_Can_Submit_The_Rendered_Training_Program_Create_Form()
    {
        using var factory = new LocalFactory();
        await factory.AddIsolatedTestEmployees();
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        var html = await client.GetStringAsync("/admin/training-programs/Create");
        var fields = ProgramFormFields(html, "Safety training", "Local regression test");
        using var response = await client.PostAsync("/admin/training-programs/Create", new FormUrlEncodedContent(fields));
        Assert.True(response.StatusCode == HttpStatusCode.Redirect,
            $"Expected successful save redirect; received {response.StatusCode}. Hidden Id='{fields.GetValueOrDefault("Id")}', RowVersion='{fields.GetValueOrDefault("RowVersion")}'.");
        using var scope = factory.Services.CreateScope();
        var program = Assert.Single(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingPrograms.ToListAsync());
        Assert.Equal("Safety training", program.Title);
        Assert.Equal("Local regression test", program.Description);
    }

    [Fact]
    public async Task Training_Program_Edit_Form_Preserves_The_Concurrency_Token()
    {
        using var factory = new LocalFactory();
        await factory.AddIsolatedTestEmployees();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.TrainingPrograms.Add(new TrainingProgram { Id = 10, Title = "Original", RowVersion = [1, 2, 3, 4, 5, 6, 7, 8] });
            await db.SaveChangesAsync();
        }
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        var fields = ProgramFormFields(await client.GetStringAsync("/admin/training-programs/Edit/10"), "Updated", "Updated description");
        Assert.Equal("10", fields["Id"]);
        Assert.Equal(Convert.ToBase64String([1, 2, 3, 4, 5, 6, 7, 8]), fields["RowVersion"]);
        using var response = await client.PostAsync("/admin/training-programs/Edit/10", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var verification = factory.Services.CreateScope();
        Assert.Equal("Updated", (await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingPrograms.FindAsync(10))!.Title);
    }

    [Theory]
    [InlineData("admin", "lesson.mp4", true, false, false, true)]
    [InlineData("admin", "lesson.webm", true, false, false, true)]
    [InlineData("admin", "lesson.exe", true, false, false, false)]
    [InlineData("admin", "lesson.mp4", false, false, false, false)]
    [InlineData("admin", "lesson.mp4", true, true, false, false)]
    [InlineData("employee", "lesson.mp4", true, false, false, false)]
    [InlineData("admin", "lesson.mp4", true, false, true, false)]
    public async Task Video_Upload_Enforces_Validation_And_Access_And_Cleans_Failed_Saves(
        string role, string filename, bool validHeader, bool duplicate, bool omitToken, bool succeeds)
    {
        var root = Path.Combine(Path.GetTempPath(), "portal-upload-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var factory = new LocalFactory(overrides: new() { ["VideoRepository:Path"] = root });
            await factory.AddIsolatedTestEmployees();
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.TrainingPrograms.Add(new TrainingProgram { Id = 10, Title = "Upload test" });
                if (duplicate) db.TrainingVideos.Add(new TrainingVideo { Id = 10, Title = "Existing", TrainingProgramId = 10, SequenceOrder = 1, DurationSeconds = 10, VideoUrl = "existing.mp4" });
                await db.SaveChangesAsync();
            }
            using var client = Browser(factory);
            using var login = await SignIn(client, role);
            var html = await client.GetStringAsync(role == "admin" ? "/admin/training-videos/Create" : "/login");
            using var form = new MultipartFormDataContent();
            if (!omitToken) form.Add(new StringContent(Token(html)), "__RequestVerificationToken");
            form.Add(new StringContent("Uploaded video"), "Title");
            form.Add(new StringContent("10"), "TrainingProgramId");
            form.Add(new StringContent("1"), "SequenceOrder");
            form.Add(new StringContent("10"), "DurationSeconds");
            // Container header fixtures test format screening, not full media decoding.
            byte[] bytes = validHeader
                ? filename.EndsWith(".webm") ? [0x1a, 0x45, 0xdf, 0xa3, 0, 0, 0, 0, 0, 0, 0, 0]
                    : [0, 0, 0, 20, 102, 116, 121, 112, 105, 115, 111, 109, 0, 0, 0, 0, 105, 115, 111, 109]
                : [1, 2, 3];
            form.Add(new ByteArrayContent(bytes), "videoFile", filename);
            using var response = await client.PostAsync("/admin/training-videos/Create", form);
            using var verification = factory.Services.CreateScope();
            var database = verification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var videos = await database.TrainingVideos.ToListAsync();
            if (succeeds)
            {
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                var video = Assert.Single(videos);
                Assert.NotEqual(filename, video.VideoUrl);
                Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(root, video.VideoUrl)));
                using var media = await client.GetAsync("/admin/media/" + video.Id);
                Assert.Equal(HttpStatusCode.OK, media.StatusCode);
                using var staticFile = await client.GetAsync("/App_Data/Videos/" + video.VideoUrl);
                Assert.Equal(HttpStatusCode.NotFound, staticFile.StatusCode);
            }
            else
            {
                Assert.Equal(duplicate ? 1 : 0, videos.Count);
                Assert.Empty(Directory.Exists(root) ? Directory.GetFiles(root) : []);
                if (omitToken) Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                else if (role == "employee") Assert.Contains("access-denied", response.Headers.Location!.ToString());
                else Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
        finally
        {
            if (Directory.Exists(root))
            {
                foreach (var file in Directory.GetFiles(root)) File.Delete(file);
                Directory.Delete(root);
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Video_Edit_Preserves_Existing_File_And_Progress_Restrictions(bool replace, bool started)
    {
        var root = Path.Combine(Path.GetTempPath(), "portal-edit-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "original.mp4"), "existing content");
        try
        {
            using var factory = new LocalFactory(overrides: new() { ["VideoRepository:Path"] = root });
            await factory.AddIsolatedTestEmployees();
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.TrainingPrograms.Add(new TrainingProgram { Id = 10, Title = "Edit test" });
                db.TrainingVideos.Add(new TrainingVideo { Id = 10, Title = "Existing", TrainingProgramId = 10, SequenceOrder = 1, DurationSeconds = 10, VideoUrl = "original.mp4", RowVersion = [1,2,3,4,5,6,7,8] });
                if (started) db.VideoProgresses.Add(new VideoProgress { EmployeeId = 2, TrainingVideoId = 10 });
                await db.SaveChangesAsync();
            }
            using var client = Browser(factory);
            using var login = await SignIn(client, "admin");
            var html = await client.GetStringAsync("/admin/training-videos/Edit/10");
            var fields = ProgramFormFields(html, "Updated", "Description");
            fields["TrainingProgramId"] = "10";
            fields.Remove("videoFile");
            using var form = new MultipartFormDataContent();
            foreach (var field in fields) form.Add(new StringContent(field.Value), field.Key);
            if (replace) form.Add(new ByteArrayContent([0,0,0,20,102,116,121,112,105,115,111,109,0,0,0,0,105,115,111,109]), "videoFile", "replacement.mp4");
            using var response = await client.PostAsync("/admin/training-videos/Edit/10", form);
            Assert.Equal(started ? HttpStatusCode.OK : HttpStatusCode.Redirect, response.StatusCode);
            using var verification = factory.Services.CreateScope();
            var video = await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().TrainingVideos.FindAsync(10);
            Assert.Equal(replace && !started ? 2 : 1, Directory.GetFiles(root).Length);
            Assert.Equal("existing content", await File.ReadAllTextAsync(Path.Combine(root, "original.mp4")));
            Assert.Equal(started ? "Existing" : "Updated", video!.Title);
            if (started || !replace) Assert.Equal("original.mp4", video.VideoUrl);
            else Assert.NotEqual("original.mp4", video.VideoUrl);
        }
        finally
        {
            foreach (var file in Directory.GetFiles(root)) File.Delete(file);
            Directory.Delete(root);
        }
    }

    private static Dictionary<string, string> ProgramFormFields(string html, string title, string description)
    {
        var fields = new Dictionary<string, string>();
        foreach (Match input in Regex.Matches(html, "<input\\b[^>]*>"))
        {
            var name = Regex.Match(input.Value, "name=\"([^\"]+)\"");
            var value = Regex.Match(input.Value, "value=\"([^\"]*)\"");
            // Multi-value employee selection is supplied explicitly by each test.
            if (name.Success && name.Groups[1].Value != "employeeIds") fields[WebUtility.HtmlDecode(name.Groups[1].Value)] = WebUtility.HtmlDecode(value.Groups[1].Value);
        }
        fields["Title"] = title;
        fields["Description"] = description;
        return fields;
    }

    private static HttpClient Browser(LocalFactory factory) => factory.CreateClient(new WebApplicationFactoryClientOptions
    { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });
    private static async Task<HttpResponseMessage> SignIn(HttpClient client, string choice)
    {
        var html = await client.GetStringAsync("/login");
        Assert.Contains("Development Testing Only", html);
        return await client.PostAsync("/login/dev/" + choice, Form(Token(html)));
    }
    private static FormUrlEncodedContent Form(string token) => new(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
    private static string Token(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "The rendered form must contain an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    private static string CookieValue(HttpResponseMessage response) => response.Headers.GetValues("Set-Cookie")
        .Single(x => x.StartsWith(DevelopmentAuthentication.CookieName + "=", StringComparison.Ordinal))
        .Split(';')[0][(DevelopmentAuthentication.CookieName.Length + 1)..];

    private sealed class LocalFactory(
        string environment = "Development", bool entraEnabled = false,
        IPAddress? remoteAddress = null, IDataProtectionProvider? protection = null,
        Dictionary<string, string?>? overrides = null) : WebApplicationFactory<Program>
    {
        private readonly string _database = Guid.NewGuid().ToString();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["EntraId:Enabled"] = entraEnabled.ToString(),
                    // Synthetic identifiers used only to initialize the isolated test host. No SSO request is made.
                    ["EntraId:TenantId"] = "11111111-1111-1111-1111-111111111111",
                    ["EntraId:ClientId"] = "22222222-2222-2222-2222-222222222222",
                    ["PortalAuthorization:AllowDevelopmentBypass"] = "false",
                    ["PortalAuthorization:RoleClaimType"] = "roles",
                    ["PortalAuthorization:AdministratorRole"] = "Administrator",
                    ["PortalAuthorization:EmployeeRole"] = "Employee",
                    ["EmployeeIdentity:MatchMode"] = "Email",
                    ["EmployeeIdentity:EmailClaimType"] = "preferred_username"
                };
                if (overrides != null) foreach (var item in overrides) values[item.Key] = item.Value;
                configuration.AddInMemoryCollection(values);
            });
            builder.ConfigureTestServices(services =>
            {
                // EF 10 retains provider configuration separately from DbContextOptions.
                services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_database));
                services.AddSingleton<IDataProtectionProvider>(protection ?? new EphemeralDataProtectionProvider());
                services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                services.AddSingleton<IStartupFilter>(new LoopbackConnection(remoteAddress ?? IPAddress.Loopback));
            });
        }

        public async Task AddIsolatedTestEmployees()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal("Microsoft.EntityFrameworkCore.InMemory", db.Database.ProviderName);
            db.Departments.Add(new Department { Id = 1, Name = "Isolated test department" });
            db.Employees.AddRange(
                new Employee { Id = 1, EmployeeId = "900001", WorkerType = "Employee", FirstName = "Local", LastName = "Admin", DepartmentId = 1,
                    Email = DevelopmentAuthentication.AdminEmail, UserId = DevelopmentAuthentication.AdminUserId },
                new Employee { Id = 2, EmployeeId = "900002", WorkerType = "Employee", FirstName = "Local", LastName = "Employee", DepartmentId = 1,
                    Email = DevelopmentAuthentication.EmployeeEmail, UserId = DevelopmentAuthentication.EmployeeUserId });
            await db.SaveChangesAsync();
        }
    }

    private sealed class LoopbackConnection(IPAddress address) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, continuation) => { context.Connection.RemoteIpAddress = address; return continuation(context); });
            next(app);
        };
    }
}

