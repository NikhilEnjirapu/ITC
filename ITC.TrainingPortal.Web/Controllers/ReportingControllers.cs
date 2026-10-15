using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Web.Authentication;
using ITC.TrainingPortal.Web.Authorization;
using ITC.TrainingPortal.Web.Services;
using ITC.TrainingPortal.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ITC.TrainingPortal.Web.Controllers;

[Authorize(Policy = PortalPolicies.Administrator)]
public sealed class ReportController(ComplianceReportService reports, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(ReportFilter filter, int page = 1)
    {
        ViewBag.Employees = new SelectList(await db.Employees.AsNoTracking().OrderBy(x => x.EmployeeId).ToListAsync(), "Id", "EmployeeId", filter.EmployeeId);
        ViewBag.Departments = new SelectList(await db.Departments.AsNoTracking().OrderBy(x => x.Name).ToListAsync(), "Id", "Name", filter.DepartmentId);
        ViewBag.Programs = new SelectList(await db.TrainingPrograms.AsNoTracking().OrderBy(x => x.Title).ToListAsync(), "Id", "Title", filter.TrainingProgramId);
        ViewBag.Filter = filter;
        page = Math.Clamp(page, 1, 100000);
        var rows = await reports.GetAsync(filter, (page - 1) * 100, 101);
        ViewBag.Page = page;
        ViewBag.HasNext = rows.Count > 100;
        return View(rows.Take(100).ToList());
    }
    public async Task<IActionResult> Csv(ReportFilter filter) => File(ReportExporter.Csv(await reports.GetAsync(filter)),
        "text/csv; charset=utf-8", "training-compliance.csv");
}

public sealed class ProgressController(ComplianceReportService reports, ICurrentEmployeeAccessor accessor) : Controller
{
    [Authorize(Policy = PortalPolicies.Administrator)]
    public IActionResult Index(ReportFilter filter) => RedirectToAction("Index", "Report", new
    {
        filter.EmployeeId, filter.DepartmentId, filter.TrainingProgramId, filter.Status
    });
    [Authorize(Policy = PortalPolicies.Employee), HttpGet("/employee/progress")]
    public async Task<IActionResult> Mine()
    {
        var employee = await accessor.ResolveAsync(User);
        return employee.Status != EmployeeIdentityResolutionStatus.Resolved ? Forbid() :
            View(await reports.GetAsync(new ReportFilter { EmployeeId = employee.Employee!.Id }));
    }
}

[Authorize(Policy = PortalPolicies.Administrator)]
public sealed class AuditController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? employeeId, string? actionFilter, int page = 1, DateTime? from = null, DateTime? to = null)
    {
        page = Math.Clamp(page, 1, 100000);
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value.Date);
        if (to.HasValue) { var end = to.Value.Date.AddDays(1); query = query.Where(x => x.CreatedAt < end); }
        ViewBag.From = from?.ToString("yyyy-MM-dd"); ViewBag.To = to?.ToString("yyyy-MM-dd");
        if (!string.IsNullOrWhiteSpace(employeeId)) query = query.Where(x => x.EmployeeId == employeeId);
        if (!string.IsNullOrWhiteSpace(actionFilter)) query = query.Where(x => x.Action.Contains(actionFilter!));
        ViewBag.Page = page; ViewBag.EmployeeId = employeeId; ViewBag.ActionFilter = actionFilter;
        ViewBag.HasNext = await query.CountAsync() > page * 100;
        return View(await query.OrderByDescending(x => x.Id).Skip((page - 1) * 100).Take(100).ToListAsync());
    }
}
