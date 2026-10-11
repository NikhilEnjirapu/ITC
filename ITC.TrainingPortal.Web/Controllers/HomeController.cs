using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ITC.TrainingPortal.Web.Models;
using ITC.TrainingPortal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using ITC.TrainingPortal.Web.Authorization;

namespace ITC.TrainingPortal.Web.Controllers;

[Authorize(Policy = PortalPolicies.Administrator)]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ITC.TrainingPortal.Web.Services.ComplianceReportService _reports;
    private readonly IEmployeeService _employees;
    private readonly ITrainingProgramService _programs;
    private readonly ITrainingVideoService _videos;
    private readonly ITrainingAssignmentService _assignments;

    public HomeController(ILogger<HomeController> logger, IEmployeeService employees, ITrainingProgramService programs, ITrainingVideoService videos, ITrainingAssignmentService assignments, ITC.TrainingPortal.Web.Services.ComplianceReportService reports)
    {
        _logger = logger; _reports = reports;
        _employees=employees;_programs=programs;_videos=videos;_assignments=assignments;
    }

    [HttpGet("/admin"), HttpGet("/admin/dashboard")]
    public async Task<IActionResult> Index()
    {
        var model=new DashboardViewModel();
        try
        {
            var employees=await _employees.GetAllAsync();var programs=await _programs.GetAllAsync();var videos=await _videos.GetAllAsync();var assignments=await _assignments.GetAllAsync();
            var compliance=await _reports.GetAsync(new());model.CompletedAssignments=compliance.Count(x=>x.Status=="Completed");model.OverdueAssignments=compliance.Count(x=>x.Status=="Overdue");
            model.IsDataAvailable=true;model.TotalEmployees=employees.Count();model.TotalPrograms=programs.Count();model.TotalVideos=videos.Count();model.TotalAssignments=assignments.Count();model.RecentAssignments=assignments.Take(5).ToList();
        }
        catch(Exception ex){_logger.LogError(ex,"Unable to load dashboard data.");}
        return View(model);
    }

    [AllowAnonymous, HttpGet("/privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous, HttpGet("/error")]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
