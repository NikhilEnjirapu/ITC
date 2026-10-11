using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ITC.TrainingPortal.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using ITC.TrainingPortal.Web.Authorization;

namespace ITC.TrainingPortal.Web.Controllers;

[Authorize(Policy = PortalPolicies.Administrator)]
public class EmployeeController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly IDepartmentService _departmentService;

    public EmployeeController(IEmployeeService employeeService, IDepartmentService departmentService)
    {
        _employeeService = employeeService;
        _departmentService = departmentService;
    }

    public async Task<IActionResult> Index(int? departmentId)
    {
        var emps = await _employeeService.GetAllAsync();
        if(departmentId.HasValue) { emps=emps.Where(x=>x.DepartmentId==departmentId); ViewBag.DepartmentFilter=departmentId; }
        return View(emps);
    }

    public async Task<IActionResult> Create()
    {
        var departments = (await _departmentService.GetAllAsync()).ToList();
        if (departments.Count == 0)
        {
            ViewData["SetupMessage"] = "Create a department before adding employees.";
            ViewData["SetupController"] = "Department"; ViewData["SetupAction"] = "Create"; ViewData["SetupLabel"] = "Create department";
            ViewData["ReturnToEmployee"] = true;
            return View("_SetupRequired");
        }
        ViewBag.Departments = new SelectList(departments, "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeDto dto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _employeeService.CreateAsync(dto);
                TempData["Success"] = "Employee created. You can now assign ready training programs from employee details.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }
        ViewBag.Departments = new SelectList(await _departmentService.GetAllAsync(), "Id", "Name", dto.DepartmentId);
        return View(dto);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var emp = await _employeeService.GetByIdAsync(id);
        if (emp == null) return NotFound();
        ViewBag.Departments = new SelectList(await _departmentService.GetAllAsync(), "Id", "Name", emp.DepartmentId);
        return View(emp);
    }

    public async Task<IActionResult> Details(int id)
    {
        var employee=await _employeeService.GetByIdAsync(id);
        return employee==null?NotFound():View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeDto dto)
    {
        if (id != dto.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            try
            {
                await _employeeService.UpdateAsync(dto);
                TempData["Success"] = "Employee saved.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (ConcurrencyConflictException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var current = await _employeeService.GetByIdAsync(dto.Id);
                if (current == null) return NotFound();
                dto.RowVersion = current.RowVersion;
                ModelState.Remove(nameof(dto.RowVersion));
            }
        }
        ViewBag.Departments = new SelectList(await _departmentService.GetAllAsync(), "Id", "Name", dto.DepartmentId);
        return View(dto);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var emp = await _employeeService.GetByIdAsync(id);
        if (emp == null) return NotFound();
        return View(emp);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var db = HttpContext.RequestServices.GetRequiredService<ITC.TrainingPortal.Infrastructure.Data.ApplicationDbContext>();
        if (db.TrainingAssignments.Any(x => x.EmployeeId == id) || db.QuizResults.Any(x => x.EmployeeId == id) || db.VideoProgresses.Any(x => x.EmployeeId == id))
        {
            ModelState.AddModelError("", "This employee has training assignments or learning history and cannot be deleted. Review their assignments and eligibility instead.");
            var employee = await _employeeService.GetByIdAsync(id);
            return employee == null ? NotFound() : View("Delete", employee);
        }
        try { await _employeeService.DeleteAsync(id); TempData["Success"] = "Employee deleted."; return RedirectToAction(nameof(Index)); }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException) { ModelState.AddModelError("", "The employee is still referenced by other records. Reload and review their training history."); return View("Delete", await _employeeService.GetByIdAsync(id)); }
    }
}
