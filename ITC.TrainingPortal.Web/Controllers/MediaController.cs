using ITC.TrainingPortal.Application.Identity;
using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Web.Authentication;
using ITC.TrainingPortal.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITC.TrainingPortal.Web.Controllers;

[Authorize(Policy = PortalPolicies.AuthenticatedUser)]
public sealed class MediaController(ApplicationDbContext db, ICurrentEmployeeAccessor accessor, IConfiguration configuration, IWebHostEnvironment environment) : Controller
{
    [Authorize(Policy = PortalPolicies.Employee), HttpGet("/employee/media/{id:int}")]
    public Task<IActionResult> Video(int id) => Serve(id, false);

    [Authorize(Policy = PortalPolicies.Administrator), HttpGet("/admin/media/{id:int}")]
    public Task<IActionResult> Preview(int id) => Serve(id, true);

    private async Task<IActionResult> Serve(int id, bool administrator)
    {
        var resolution = await accessor.ResolveAsync(User);
        if (resolution.Status != EmployeeIdentityResolutionStatus.Resolved) return Forbid();
        var employeeId = resolution.Employee!.Id;
        if (await db.QuizResults.AnyAsync(x => x.EmployeeId == employeeId && !x.IsCompleted)) return StatusCode(403);
        var video = await db.TrainingVideos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (video == null || (!administrator && !await db.TrainingAssignments.AnyAsync(x => x.EmployeeId == employeeId && x.TrainingProgramId == video.TrainingProgramId))) return NotFound();
        if (!administrator && await db.TrainingVideos.AnyAsync(x => x.TrainingProgramId == video.TrainingProgramId && x.SequenceOrder < video.SequenceOrder &&
            !db.VideoProgresses.Any(p => p.EmployeeId == employeeId && p.TrainingVideoId == x.Id && p.IsCompleted))) return StatusCode(403);
        var root = Path.GetFullPath(configuration["VideoRepository:Path"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "Videos"));
        // Only repository-relative MP4/WebM files are accepted; never fetch arbitrary URLs or expose static media.
        var name = video.VideoUrl;
        if (Path.IsPathRooted(name) || name.Contains(':') || name.Split('/', '\\').Any(x => x is ".." or ".")) return NotFound();
        var path = Path.GetFullPath(Path.Combine(root, name));
        if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(path)) return NotFound();
        var type = Path.GetExtension(path).ToLowerInvariant() switch { ".mp4" => "video/mp4", ".webm" => "video/webm", _ => null };
        if (type == null) return NotFound();
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(path, type, enableRangeProcessing: true);
    }

    [HttpGet("/employee/playback-status")]
    public async Task<IActionResult> Status()
    {
        var resolution = await accessor.ResolveAsync(User);
        if (resolution.Status != EmployeeIdentityResolutionStatus.Resolved) return StatusCode(403);
        Response.Headers.CacheControl = "no-store";
        return Json(new { allowed = !await db.QuizResults.AnyAsync(x => x.EmployeeId == resolution.Employee!.Id && !x.IsCompleted) });
    }
}
