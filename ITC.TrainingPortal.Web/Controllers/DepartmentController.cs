using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ITC.TrainingPortal.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using ITC.TrainingPortal.Web.Authorization;

namespace ITC.TrainingPortal.Web.Controllers;

[Authorize(Policy = PortalPolicies.Administrator)]
public class DepartmentController : Controller
{
    private readonly IDepartmentService _departmentService;

    public DepartmentController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    public async Task<IActionResult> Index()
    {
        var depts = await _departmentService.GetAllAsync();
        return View(depts);
    }

    public IActionResult Create(bool returnToEmployee = false)
    {
        ViewBag.ReturnToEmployee = returnToEmployee;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentDto dto, bool returnToEmployee = false)
    {
        ViewBag.ReturnToEmployee = returnToEmployee;
        if (ModelState.IsValid)
        {
            await _departmentService.CreateAsync(dto);
            TempData["Success"] = "Department created. You can now add employees.";
            return returnToEmployee ? RedirectToAction("Create", "Employee") : RedirectToAction(nameof(Index));
        }
        return View(dto);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var dept = await _departmentService.GetByIdAsync(id);
        if (dept == null) return NotFound();
        return View(dept);
    }

    public async Task<IActionResult> Details(int id)
    {
        var department=await _departmentService.GetByIdAsync(id);
        return department==null?NotFound():View(department);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DepartmentDto dto)
    {
        if (id != dto.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            try
            {
                await _departmentService.UpdateAsync(dto);
                TempData["Success"] = "Department saved.";
                return RedirectToAction(nameof(Index));
            }
            catch (ConcurrencyConflictException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var current = await _departmentService.GetByIdAsync(dto.Id);
                if (current == null) return NotFound();
                dto.RowVersion = current.RowVersion;
                ModelState.Remove(nameof(dto.RowVersion));
            }
        }
        return View(dto);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var dept = await _departmentService.GetByIdAsync(id);
        if (dept == null) return NotFound();
        return View(dept);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _departmentService.DeleteAsync(id);
            TempData["Success"] = "Department deleted.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            var dept = await _departmentService.GetByIdAsync(id);
            return View(dept);
        }
    }
}
