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
        return View(new TrainingAssignmentDto { AssignedAt=DateTime.UtcNow, TrainingProgramId=programId ?? 0, EmployeeId=employeeId ?? 0 });
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
                foreach(var employee in selected) { dto.EmployeeId=employee; await _service.CreateAsync(dto); }
                if(transaction != null) await transaction.CommitAsync();
                TempData["Success"]=$"Training assigned to {selected.Length} employee(s).";
                return RedirectToAction(nameof(Index));
            }
            catch(InvalidOperationException ex) { if(transaction != null) await transaction.RollbackAsync(); db.ChangeTracker.Clear(); ModelState.AddModelError("",ex.Message); }
            catch(Microsoft.EntityFrameworkCore.DbUpdateException) { if(transaction != null) await transaction.RollbackAsync(); db.ChangeTracker.Clear(); ModelState.AddModelError("", "The assignments changed while saving. Reload and try again."); }
        }
        await SetLists(dto.EmployeeId,dto.TrainingProgramId); return View(dto);
    }
    public async Task<IActionResult>Edit(int id){var dto=await _service.GetByIdAsync(id);if(dto==null)return NotFound();await SetLists(dto.EmployeeId,dto.TrainingProgramId);return View(dto);}
    [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult>Edit(int id,TrainingAssignmentDto dto,[FromServices] AdminWorkflowService workflow){if(id!=dto.Id)return BadRequest();if(ModelState.IsValid)try{await workflow.ValidateAssignmentAsync(dto.EmployeeId,dto.TrainingProgramId,id);await _service.UpdateAsync(dto);TempData["Success"]="Assignment saved.";return RedirectToAction(nameof(Index));}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);}catch(ConcurrencyConflictException ex){ModelState.AddModelError("",ex.Message);var current=await _service.GetByIdAsync(id);if(current==null)return NotFound();dto.RowVersion=current.RowVersion;ModelState.Remove(nameof(dto.RowVersion));}await SetLists(dto.EmployeeId,dto.TrainingProgramId);return View(dto);}
    public async Task<IActionResult>Delete(int id)=>await _service.GetByIdAsync(id)is{}dto?View(dto):NotFound();
    [HttpPost,ActionName("Delete"),ValidateAntiForgeryToken]public async Task<IActionResult>DeleteConfirmed(int id){await _service.DeleteAsync(id);TempData["Success"]="Assignment removed. Learning history has been retained.";return RedirectToAction(nameof(Index));}
    private async Task SetLists(int? employee=null,int? program=null)
    {
        var workflow = HttpContext.RequestServices.GetRequiredService<AdminWorkflowService>();
        var eligibleIds = (await workflow.EligibleEmployeesAsync()).Select(x => x.Id).ToHashSet();
        ViewBag.Employees = (await _employees.GetAllAsync()).Select(x => new SelectListItem($"{x.EmployeeId} - {x.FirstName} {x.LastName}" + (eligibleIds.Contains(x.Id) ? "" : " (not eligible)"), x.Id.ToString(), x.Id == employee) { Disabled = !eligibleIds.Contains(x.Id) && x.Id != employee }).ToList();
        if(Request.HasFormContentType && Request.Form["employeeIds"].Count > 0)
            foreach(SelectListItem item in ViewBag.Employees) item.Selected=Request.Form["employeeIds"].Contains(item.Value);
        ViewBag.Programs = (await workflow.ProgramsAsync()).Select(x => new SelectListItem(x.Title + (x.IsReady ? "" : $" ({x.Missing})"), x.Id.ToString(), x.Id == program) { Disabled = !x.IsReady && x.Id != program }).ToList();
    }
}
