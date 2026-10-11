using System.ComponentModel.DataAnnotations;
using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITC.TrainingPortal.Web.Controllers;

[Authorize(Policy = PortalPolicies.Administrator)]
public sealed class EligibilityController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.EligibilityRules.AsNoTracking().OrderBy(x => x.Id).ToListAsync());
    public IActionResult Create() => View("Edit", new EligibilityRuleForm());
    public async Task<IActionResult> Edit(int id) => await db.EligibilityRules.FindAsync(id) is { } rule
        ? View(new EligibilityRuleForm { Id = rule.Id, RuleType = rule.RuleType, RuleValue = rule.RuleValue, IsActive = rule.IsActive }) : NotFound();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(EligibilityRuleForm form, bool preview = false)
    {
        if (!EligibilityEvaluator.RuleTypes.Contains(form.RuleType)) ModelState.AddModelError(nameof(form.RuleType), "Select a supported rule type.");
        if (form.RuleType == "DepartmentId" && (!int.TryParse(form.RuleValue, out var department) || !await db.Departments.AnyAsync(x => x.Id == department)))
            ModelState.AddModelError(nameof(form.RuleValue), "Enter an existing department ID.");
        if (form.RuleType == "WorkerType" && !EligibilityEvaluator.WorkerTypes.Contains(form.RuleValue))
            ModelState.AddModelError(nameof(form.RuleValue), "Enter a supported worker category.");
        if (!ModelState.IsValid) return View("Edit", form);
        if (preview)
        {
            var candidate = new EligibilityRule { Id = form.Id, RuleType = form.RuleType, RuleValue = form.RuleValue.Trim(), IsActive = form.IsActive };
            var existing = await db.EligibilityRules.AsNoTracking().Where(x => x.Id != form.Id).ToListAsync();
            var employees = await db.Employees.AsNoTracking().ToListAsync();
            ViewBag.Affected = employees.Where(x => EligibilityEvaluator.DenialReason(x, existing) == null && EligibilityEvaluator.DenialReason(x, existing.Append(candidate)) != null)
                .Select(x => $"{x.EmployeeId} - {x.FirstName} {x.LastName}").ToList();
            return View("Edit", form);
        }
        var rule = form.Id == 0 ? new EligibilityRule() : await db.EligibilityRules.FindAsync(form.Id);
        if (rule == null) return NotFound();
        rule.RuleType = form.RuleType; rule.RuleValue = form.RuleValue.Trim(); rule.IsActive = form.IsActive;
        if (form.Id == 0) db.EligibilityRules.Add(rule);
        await db.SaveChangesAsync();
        TempData["Success"] = "Eligibility rule saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await db.EligibilityRules.FindAsync(id);
        if (rule == null) return NotFound();
        db.EligibilityRules.Remove(rule); await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}

public sealed class EligibilityRuleForm
{
    public int Id { get; set; }
    [Required] public string RuleType { get; set; } = "EmployeeIdPrefix";
    [Required, StringLength(256)] public string RuleValue { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
