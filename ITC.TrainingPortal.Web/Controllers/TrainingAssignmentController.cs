using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Cryptography;
using ITC.TrainingPortal.Web.Models;
using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Domain.Exceptions;
using ITC.TrainingPortal.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ITC.TrainingPortal.Web.Services;

namespace ITC.TrainingPortal.Web.Controllers;

[Authorize(Policy = PortalPolicies.Administrator)]
public sealed class TrainingAssignmentController : Controller
{
    private readonly ITrainingAssignmentService _service;private readonly IEmployeeService _employees;private readonly ITrainingProgramService _programs;
    public TrainingAssignmentController(ITrainingAssignmentService service,IEmployeeService employees,ITrainingProgramService programs){_service=service;_employees=employees;_programs=programs;}
    public async Task<IActionResult> Index()=>View(await _service.GetAllAsync());
    public async Task<IActionResult> Details(int id)=>await _service.GetByIdAsync(id)is{}dto?View(dto):NotFound();
    public async Task<IActionResult> Create(int? programId, int? employeeId, [FromServices] AdminWorkflowService workflow)
    {
        var programs = await workflow.ProgramsAsync();
        if (!programs.Any(x => x.IsReady) || (programId.HasValue && !programs.Any(x => x.Id == programId && x.IsReady)))
        {
            ViewData["SetupMessage"] = "Create a program, add videos and exam questions, and review exam availability before assigning training.";
            ViewData["SetupController"] = "TrainingProgram"; ViewData["SetupAction"] = programs.Count == 0 ? "Create" : "Index";
            ViewData["SetupLabel"] = programs.Count == 0 ? "Create your first program" : "Continue program setup";
            return View("_SetupRequired");
        }
        if (!(await workflow.EligibleEmployeesAsync()).Any())
        {
            ViewData["SetupMessage"] = "Add an eligible employee before assigning training. Check worker categories and eligibility rules if employees already exist.";
            ViewData["SetupController"] = "Employee"; ViewData["SetupAction"] = "Index"; ViewData["SetupLabel"] = "Manage employees";
            return View("_SetupRequired");
        }
        await SetLists(employeeId, programId);
        return View(new TrainingAssignmentDto { AssignedAt=PortalTime.ToLocal(DateTime.UtcNow), TrainingProgramId=programId ?? 0, EmployeeId=employeeId ?? 0 });
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainingAssignmentDto dto, [FromServices] AdminWorkflowService workflow, int[]? employeeIds)
    {
        var selected = (employeeIds is { Length: > 0 } ? employeeIds : new[] { dto.EmployeeId }).Distinct().ToArray();
        if(employeeIds is { Length: > 0 }) { dto.EmployeeId = selected[0]; ModelState.Remove(nameof(dto.EmployeeId)); }
        if(ModelState.IsValid)
        {
            var db = HttpContext.RequestServices.GetRequiredService<ITC.TrainingPortal.Infrastructure.Data.ApplicationDbContext>();
            await using var transaction = Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.IsRelational(db.Database)
                ? await db.Database.BeginTransactionAsync() : null;
            try
            {
                foreach(var employee in selected)
                {
                    await workflow.ValidateAssignmentAsync(employee,dto.TrainingProgramId);
                    if(db.TrainingAssignments.Any(x => x.EmployeeId == employee && x.TrainingProgramId == dto.TrainingProgramId))
                        throw new InvalidOperationException("One or more selected employees already have this program. Remove them from the selection and try again.");
                }
                foreach(var employee in selected) { dto.EmployeeId=employee; await _service.CreateAsync(ToStoredAssignment(dto)); }
                if(transaction != null) await transaction.CommitAsync();
                TempData["Success"]=$"Training assigned to {selected.Length} employee(s).";
                return RedirectToAction(nameof(Index));
            }
            catch(InvalidOperationException ex) { if(transaction != null) await transaction.RollbackAsync(); db.ChangeTracker.Clear(); ModelState.AddModelError("",ex.Message); }
            catch(Microsoft.EntityFrameworkCore.DbUpdateException) { if(transaction != null) await transaction.RollbackAsync(); db.ChangeTracker.Clear(); ModelState.AddModelError("", "The assignments changed while saving. Reload and try again."); }
        }
        await SetLists(dto.EmployeeId,dto.TrainingProgramId); return View(dto);
    }
    [HttpGet]
    public IActionResult CsvTemplate() => File(System.Text.Encoding.UTF8.GetBytes("EmployeeId\r\n"), "text/csv", "assignment-employees.csv");

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(256 * 1024)]
    public async Task<IActionResult> Preview(TrainingAssignmentDto dto, int[]? employeeIds, string selectionMode,
        IFormFile? csvFile, [FromServices] BatchAssignmentService batch, [FromServices] IDataProtectionProvider protection)
    {
        ModelState.Remove(nameof(dto.EmployeeId));
        if (dto.DueDate.HasValue && dto.DueDate < dto.AssignedAt)
            ModelState.AddModelError(nameof(dto.DueDate), "Due date cannot be earlier than the assignment date.");
        if (ModelState.IsValid)
        {
            try
            {
                List<string>? codes = null;
                if (selectionMode == "csv")
                {
                    if (csvFile == null || csvFile.Length == 0 || csvFile.Length > BatchAssignmentService.MaximumCsvBytes ||
                        !string.Equals(Path.GetExtension(csvFile.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Choose a CSV file no larger than 128 KB.");
                    using var stream = csvFile.OpenReadStream();
                    codes = BatchAssignmentService.ReadCsv(stream);
                }
                else if (selectionMode != "filter") throw new InvalidOperationException("Choose filter selection or CSV upload.");
                var resolved = await batch.ResolveAsync(dto.TrainingProgramId, employeeIds, codes);
                var stored = ToStoredAssignment(dto);
                var plan = new AssignmentBatch(dto.TrainingProgramId, stored.AssignedAt, stored.DueDate,
                    resolved.Rows.Where(x => x.SkipReason == null).Select(x => x.Id!.Value).ToArray(), DateTime.UtcNow.AddMinutes(30));
                var token = protection.CreateProtector("AssignmentBatch.v1").Protect(JsonSerializer.Serialize(plan));
                return View(new AssignmentPreview(resolved.Program, dto.AssignedAt, dto.DueDate, resolved.Rows, token));
            }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        }
        await SetLists(dto.EmployeeId, dto.TrainingProgramId);
        return View("Create", dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(string token, [FromServices] BatchAssignmentService batch,
        [FromServices] IDataProtectionProvider protection, [FromServices] ITC.TrainingPortal.Infrastructure.Data.ApplicationDbContext db)
    {
        AssignmentBatch? plan;
        try { plan = JsonSerializer.Deserialize<AssignmentBatch>(protection.CreateProtector("AssignmentBatch.v1").Unprotect(token ?? "")); }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException)
        { return BadRequest("The preview is invalid. Select employees and preview again."); }
        if (plan == null || plan.ExpiresAt < DateTime.UtcNow || plan.EmployeeIds.Length == 0 || plan.EmployeeIds.Length > BatchAssignmentService.MaximumEmployees)
            return BadRequest("The preview is empty or expired. Select employees and preview again.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        try
        {
            // Recheck the exact previewed IDs, never expand the selection after approval.
            var resolved = await batch.ResolveAsync(plan.ProgramId, plan.EmployeeIds);
            var ready = resolved.Rows.Where(x => x.SkipReason == null).ToList();
            foreach (var employee in ready)
                db.TrainingAssignments.Add(new() { EmployeeId = employee.Id!.Value, TrainingProgramId = plan.ProgramId,
                    AssignedAt = plan.AssignedAt, DueDate = plan.DueDate });
            await db.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();
            TempData["Success"] = $"Training assigned to {ready.Count} employee(s). {resolved.Rows.Count - ready.Count} skipped because they are already assigned or no longer eligible.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
        {
            if (transaction != null) await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            TempData["BatchError"] = "No assignments were saved. The program or employee records changed. Preview the selection again.";
            return RedirectToAction(nameof(Create), new { programId = plan.ProgramId });
        }
    }

    public async Task<IActionResult>Edit(int id){var dto=await _service.GetByIdAsync(id);if(dto==null)return NotFound();dto.AssignedAt=PortalTime.ToLocal(dto.AssignedAt);dto.DueDate=dto.DueDate.HasValue ? PortalTime.ToLocal(dto.DueDate.Value) : null;await SetLists(dto.EmployeeId,dto.TrainingProgramId);return View(dto);}
    [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult>Edit(int id,TrainingAssignmentDto dto,[FromServices] AdminWorkflowService workflow){if(id!=dto.Id)return BadRequest();if(ModelState.IsValid)try{await workflow.ValidateAssignmentAsync(dto.EmployeeId,dto.TrainingProgramId,id);await _service.UpdateAsync(ToStoredAssignment(dto));TempData["Success"]="Assignment saved.";return RedirectToAction(nameof(Index));}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);}catch(ConcurrencyConflictException ex){ModelState.AddModelError("",ex.Message);var current=await _service.GetByIdAsync(id);if(current==null)return NotFound();dto.RowVersion=current.RowVersion;ModelState.Remove(nameof(dto.RowVersion));}await SetLists(dto.EmployeeId,dto.TrainingProgramId);return View(dto);}
    public async Task<IActionResult>Delete(int id)=>await _service.GetByIdAsync(id)is{}dto?View(dto):NotFound();
    [HttpPost,ActionName("Delete"),ValidateAntiForgeryToken]public async Task<IActionResult>DeleteConfirmed(int id){await _service.DeleteAsync(id);TempData["Success"]="Assignment removed. Learning history has been retained.";return RedirectToAction(nameof(Index));}
    // Forms use IST; application services and persisted dates always use UTC.
    private static TrainingAssignmentDto ToStoredAssignment(TrainingAssignmentDto form) => new()
    {
        Id = form.Id, EmployeeId = form.EmployeeId, TrainingProgramId = form.TrainingProgramId,
        RowVersion = form.RowVersion, AssignedAt = PortalTime.ToUtc(form.AssignedAt),
        DueDate = form.DueDate.HasValue ? PortalTime.ToUtc(form.DueDate.Value) : null
    };

    private async Task SetLists(int? employee=null,int? program=null)
    {
        var workflow = HttpContext.RequestServices.GetRequiredService<AdminWorkflowService>();
        var eligibleIds = (await workflow.EligibleEmployeesAsync()).Select(x => x.Id).ToHashSet();
        ViewBag.EmployeeDirectory = (await _employees.GetAllAsync()).ToList();
        ViewBag.Employees = (await _employees.GetAllAsync()).Select(x => new SelectListItem($"{x.EmployeeId} - {x.FirstName} {x.LastName}" + (eligibleIds.Contains(x.Id) ? "" : " (not eligible)"), x.Id.ToString(), x.Id == employee) { Disabled = !eligibleIds.Contains(x.Id) && x.Id != employee }).ToList();
        if(Request.HasFormContentType && Request.Form["employeeIds"].Count > 0)
            foreach(SelectListItem item in ViewBag.Employees) item.Selected=Request.Form["employeeIds"].Contains(item.Value);
        ViewBag.Programs = (await workflow.ProgramsAsync()).Select(x => new SelectListItem(x.Title + (x.IsReady ? "" : $" ({x.Missing})"), x.Id.ToString(), x.Id == program) { Disabled = !x.IsReady && x.Id != program }).ToList();
    }
}
