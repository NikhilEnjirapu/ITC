using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Domain.Exceptions;
using ITC.TrainingPortal.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITC.TrainingPortal.Web.Controllers;

[Authorize(Policy = PortalPolicies.Administrator)]
public sealed class TrainingProgramController : Controller
{
    private readonly ITrainingProgramService _service;
    private readonly ITrainingVideoService _videos;private readonly ITrainingAssignmentService _assignments;
    private readonly IQuizService _quizzes;
    public TrainingProgramController(ITrainingProgramService service,ITrainingVideoService videos,ITrainingAssignmentService assignments,IQuizService quizzes){_service=service;_videos=videos;_assignments=assignments;_quizzes=quizzes;}
    public async Task<IActionResult> Index() => View(await _service.GetAllAsync());
    public IActionResult Create() => View(new TrainingProgramDto());
    public async Task<IActionResult> Details(int id)
    {
        var program=await _service.GetByIdAsync(id); if(program==null)return NotFound();
        var videos=(await _videos.GetAllAsync()).Where(x=>x.TrainingProgramId==id).OrderBy(x=>x.SequenceOrder).ToList();
        var assignments=(await _assignments.GetAllAsync()).Where(x=>x.TrainingProgramId==id).ToList();
        var exam=(await _quizzes.GetAllAsync()).SingleOrDefault(x=>x.TrainingProgramId==id);
        return View(new ITC.TrainingPortal.Web.Models.TrainingProgramDetailsViewModel{Program=program,Videos=videos,Assignments=assignments,Exam=exam});
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Create(TrainingProgramDto dto)
    {
        if(ModelState.IsValid) try { var id=await _service.CreateAsync(dto); TempData["Success"]="Training program created. Add videos to continue setup."; return RedirectToAction(nameof(Details), new { id }); }
        catch(InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        return View(dto);
    }
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _service.GetByIdAsync(id); if(dto == null) return NotFound();
        await UnlockVideosAsync(id); return View(dto);
    }
    public async Task<IActionResult> Availability(int id)
    {
        var dto = await _service.GetByIdAsync(id); if(dto == null) return NotFound();
        await UnlockVideosAsync(id);
        if(ViewBag.HasSetupContent != true) { TempData["Success"] = "Add videos and exam questions before configuring exam availability."; return RedirectToAction(nameof(Details), new { id }); }
        return View(dto);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Availability(int id, TrainingProgramDto dto)
    {
        if(id != dto.Id) return BadRequest();
        var current = await _service.GetByIdAsync(id); if(current == null) return NotFound();
        current.ExamUnlockCondition = dto.ExamUnlockCondition; current.ExamUnlockVideoId = dto.ExamUnlockVideoId; current.RowVersion = dto.RowVersion;
        ModelState.Remove(nameof(dto.Title));
        await UnlockVideosAsync(id);
        if(ViewBag.HasSetupContent != true) return RedirectToAction(nameof(Details),new { id });
        if(ModelState.IsValid) try { await _service.UpdateAsync(current); TempData["Success"] = "Exam availability saved. Review the program and activate the exam before assigning."; return RedirectToAction(nameof(Details),new { id }); }
        catch(InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); }
        catch(ConcurrencyConflictException ex) { ModelState.AddModelError("",ex.Message); var latest=await _service.GetByIdAsync(id); if(latest==null)return NotFound(); current.RowVersion=latest.RowVersion;ModelState.Remove(nameof(dto.RowVersion)); }
        return View(current);
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Edit(int id, TrainingProgramDto dto)
    {
        if(id!=dto.Id)return BadRequest();
        if(ModelState.IsValid) try { await _service.UpdateAsync(dto); TempData["Success"]="Training program saved."; return RedirectToAction(nameof(Details), new { id }); }
        catch(ConcurrencyConflictException ex) { ModelState.AddModelError("",ex.Message); var current=await _service.GetByIdAsync(id); if(current==null)return NotFound(); dto.RowVersion=current.RowVersion; ModelState.Remove(nameof(dto.RowVersion)); }
        catch(InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); }
        await UnlockVideosAsync(id); return View(dto);
    }
    private async Task UnlockVideosAsync(int id) { ViewBag.HasSetupContent = (await _quizzes.GetAllAsync()).Any(x => x.TrainingProgramId == id && x.QuestionCount > 0) && (await _videos.GetAllAsync()).Any(x => x.TrainingProgramId == id); ViewBag.UnlockVideos =
        (await _videos.GetAllAsync()).Where(x => x.TrainingProgramId == id).OrderBy(x => x.SequenceOrder)
        .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem($"{x.SequenceOrder}. {x.Title}", x.Id.ToString())).ToList(); }
    public async Task<IActionResult> Delete(int id) => await _service.GetByIdAsync(id) is { } dto ? View(dto) : NotFound();
    [HttpPost,ActionName("Delete"),ValidateAntiForgeryToken] public async Task<IActionResult> DeleteConfirmed(int id)
    { try{await _service.DeleteAsync(id);TempData["Success"]="Training program archived. Reports and learning history have been retained.";return RedirectToAction(nameof(Index));}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);var dto=await _service.GetByIdAsync(id);return dto==null?NotFound():View("Delete",dto);} }
}
