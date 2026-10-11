using ITC.TrainingPortal.Application.Identity;
using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Web.Authentication;
using ITC.TrainingPortal.Web.Models;
using ITC.TrainingPortal.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ITC.TrainingPortal.Web.Controllers;
[Authorize(Policy = PortalPolicies.Employee)] public sealed class ProfileController:Controller{[HttpGet("/employee/profile")]public IActionResult Index()=>View();}
[Authorize(Policy = PortalPolicies.Employee)]
public sealed class TrainingController : Controller
{
    private readonly ICurrentEmployeeAccessor _employeeAccessor;
    private readonly ITrainingAssignmentService _assignments;
    private readonly ITrainingPlaybackService _playback;
    private readonly IQuizService _quizzes;

    public TrainingController(ICurrentEmployeeAccessor employeeAccessor, ITrainingAssignmentService assignments, ITrainingPlaybackService playback, IQuizService quizzes)
    {
        _employeeAccessor = employeeAccessor;
        _assignments = assignments;
        _playback = playback;
        _quizzes = quizzes;
    }

    [HttpGet("/employee"), HttpGet("/employee/dashboard")]
    public async Task<IActionResult> Dashboard([FromServices] ITC.TrainingPortal.Web.Services.ComplianceReportService reports)
    {
        var employee = await RequireEmployeeAsync(); if(employee == null) return Forbid();
        return View(await reports.GetAsync(new ITC.TrainingPortal.Web.Services.ReportFilter { EmployeeId=employee.Id }));
    }

    [HttpGet("/employee/training")]
    public async Task<IActionResult> MyTraining()
    {
        var resolution = await _employeeAccessor.ResolveAsync(User);
        var training = resolution.Status == EmployeeIdentityResolutionStatus.Resolved
            ? await _assignments.GetEmployeeTrainingAsync(resolution.Employee!.Id)
            : [];
        return View(new MyTrainingViewModel
        {
            IdentityStatus = resolution.Status,
            ExamResults = resolution.Status == EmployeeIdentityResolutionStatus.Resolved
                ? (await _quizzes.GetEmployeeExamsAsync(resolution.Employee!.Id)).Where(x=>x.Results.Count>0 && !x.ActiveAttemptId.HasValue).ToDictionary(x=>x.TrainingProgramId,x=>x.Results[0])
                : new Dictionary<int,QuizResultDto>(),
            EmployeeName = resolution.Employee == null ? null : $"{resolution.Employee.FirstName} {resolution.Employee.LastName}",
            Assignments = training
        });
    }

    [HttpGet("/employee/training/{id:int}")]
    public async Task<IActionResult> Content(int id)
    {
        var employee = await RequireEmployeeAsync();
        if (employee == null) return Forbid();
        try { var content = await _playback.GetAssignedContentAsync(employee.Id, id); if(content == null) return NotFound(); await ExamAsync(employee.Id, id); return View(content); }
        catch (InvalidOperationException ex) { TempData["TrainingMessage"] = ex.Message; return RedirectToAction(nameof(MyTraining)); }
    }

    [HttpGet("/employee/video/{id:int}")]
    public async Task<IActionResult> Player(int id)
    {
        var employee = await RequireEmployeeAsync();
        if (employee == null) return Forbid();
        try { var player = await _playback.OpenVideoAsync(employee.Id, id); if(player == null) return NotFound(); await ExamAsync(employee.Id, player.TrainingProgramId); return View(player); }
        catch (InvalidOperationException ex) { TempData["TrainingMessage"] = ex.Message; return RedirectToAction(nameof(MyTraining)); }
    }

    [HttpPost("/employee/video/heartbeat"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Heartbeat([FromBody] VideoHeartbeatRequest request)
    {
        var employee = await RequireEmployeeAsync();
        if (employee == null) return Forbid();
        try { return Json(await _playback.RecordHeartbeatAsync(employee.Id, request)); }
        catch (ITC.TrainingPortal.Domain.Exceptions.PlaybackTimingException ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<TrainingController>>().LogWarning(
                "Playback resynchronization for video {VideoId}: accepted {Accepted}, requested {Requested}, elapsed {Elapsed}, credit {Credit}",
                request.VideoId, ex.AcceptedPositionSeconds, ex.RequestedPositionSeconds, ex.ElapsedSeconds, ex.CreditSeconds);
            return UnprocessableEntity(new { code = "playback_resync", acceptedPositionSeconds = ex.AcceptedPositionSeconds,
                message = "Playback was returned to your last saved position. Press Play to continue at normal speed." });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { message = ex.Message }); }
    }

    private async Task ExamAsync(int employeeId, int programId) => ViewData["ExamAccess"] =
        (await _quizzes.GetEmployeeExamsAsync(employeeId)).SingleOrDefault(x => x.TrainingProgramId == programId);

    private async Task<ITC.TrainingPortal.Application.DTOs.EmployeeDto?> RequireEmployeeAsync()
    {
        var resolution = await _employeeAccessor.ResolveAsync(User);
        return resolution.Status == EmployeeIdentityResolutionStatus.Resolved ? resolution.Employee : null;
    }
}
