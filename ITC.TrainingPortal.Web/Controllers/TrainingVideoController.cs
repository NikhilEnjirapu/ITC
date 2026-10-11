using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Domain.Exceptions;
using ITC.TrainingPortal.Web.Authorization;
using ITC.TrainingPortal.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ITC.TrainingPortal.Infrastructure.Data;

namespace ITC.TrainingPortal.Web.Controllers;

[Authorize(Policy = PortalPolicies.Administrator)]
public sealed class TrainingVideoController : Controller
{
    private readonly ITrainingVideoService _service; private readonly ITrainingProgramService _programs;
    private readonly VideoUploadService _uploads;
    private readonly ILogger<TrainingVideoController> _logger;
    public TrainingVideoController(ITrainingVideoService service, ITrainingProgramService programs, VideoUploadService uploads, ILogger<TrainingVideoController> logger)
    { _service = service; _programs = programs; _uploads = uploads; _logger = logger; }
    public async Task<IActionResult> Index()=>View(await _service.GetAllAsync());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(int id, string direction, [FromServices] ApplicationDbContext db)
    {
        if(direction != "up" && direction != "down") return BadRequest();
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var video = await db.TrainingVideos.FindAsync(id); if(video == null) return NotFound();
        var programId = video.TrainingProgramId;
        try
        {
            var ordered = await db.TrainingVideos.Where(x => x.TrainingProgramId == programId).OrderBy(x => x.SequenceOrder).ToListAsync();
            var ids = ordered.Select(x => x.Id).ToArray();
            if(await db.VideoProgresses.AnyAsync(x => ids.Contains(x.TrainingVideoId)))
                throw new InvalidOperationException("Video order cannot change after viewing has started. Create a new program for revised content.");
            var position = ordered.FindIndex(x => x.Id == id);
            var target = position + (direction == "up" ? -1 : 1);
            if(target >= 0 && target < ordered.Count)
            {
                var other = ordered[target]; var original = video.SequenceOrder; var destination = other.SequenceOrder;
                video.SequenceOrder = checked(ordered.Max(x => x.SequenceOrder) + 1);
                await db.SaveChangesAsync();
                other.SequenceOrder = original; await db.SaveChangesAsync();
                video.SequenceOrder = destination; await db.SaveChangesAsync();
                TempData["Success"] = "Video order updated.";
            }
            if(transaction != null) await transaction.CommitAsync();
        }
        catch(InvalidOperationException ex) { if(transaction != null)await transaction.RollbackAsync();TempData["Error"]=ex.Message; }
        catch(DbUpdateException) { if(transaction != null)await transaction.RollbackAsync();TempData["Error"]="Video records changed while reordering. Reload and try again."; }
        return RedirectToAction("Details","TrainingProgram",new { id=programId });
    }
    public async Task<IActionResult> Create(int? programId)
    {
        if (!(await _programs.GetAllAsync()).Any())
        {
            ViewData["SetupMessage"] = "Create a training program before uploading its videos.";
            ViewData["SetupController"] = "TrainingProgram"; ViewData["SetupAction"] = "Create"; ViewData["SetupLabel"] = "Create program";
            return View("_SetupRequired");
        }
        if(programId.HasValue && await _programs.GetByIdAsync(programId.Value) == null) return NotFound();
        ViewData["ReturnProgramId"]=programId; await SetPrograms(programId);
        var nextOrder = (await _service.GetAllAsync()).Where(x => x.TrainingProgramId == programId).Select(x => x.SequenceOrder).DefaultIfEmpty(0).Max() + 1;
        return View(new TrainingVideoDto { TrainingProgramId = programId ?? 0, SequenceOrder = nextOrder });
    }
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(VideoUploadService.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = VideoUploadService.MaxFileBytes)]
    public Task<IActionResult> Create(TrainingVideoDto dto, IFormFile? videoFile, int? returnProgramId) => SaveVideo(dto, videoFile, false, returnProgramId);
    public async Task<IActionResult> Edit(int id, bool fromProgram = false){var dto=await _service.GetByIdAsync(id);if(dto==null)return NotFound();if(fromProgram)ViewData["ReturnProgramId"]=dto.TrainingProgramId;await SetPrograms(dto.TrainingProgramId);return View(dto);}
    public async Task<IActionResult> Details(int id)=>await _service.GetByIdAsync(id)is{}dto?View(dto):NotFound();
    public async Task<IActionResult> Player(int id)=>await _service.GetByIdAsync(id)is{}dto?View(dto):NotFound();
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(VideoUploadService.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = VideoUploadService.MaxFileBytes)]
    public async Task<IActionResult> Edit(int id, TrainingVideoDto dto, IFormFile? videoFile, int? returnProgramId)
    {
        if (id != dto.Id) return BadRequest();
        return await SaveVideo(dto, videoFile, true, returnProgramId);
    }

    private async Task<IActionResult> SaveVideo(TrainingVideoDto dto, IFormFile? videoFile, bool editing, int? returnProgramId)
    {
        ViewData["ReturnProgramId"] = returnProgramId;
        var previousUrl = dto.VideoUrl;
        if (videoFile != null) ModelState.Remove(nameof(dto.VideoUrl));
        string? uploaded = null;
        var saved = false;
        try
        {
            if (ModelState.IsValid)
            {
                if (videoFile != null)
                {
                    uploaded = await _uploads.SaveAsync(videoFile, HttpContext.RequestAborted);
                    dto.VideoUrl = uploaded;
                }
                if (editing) await _service.UpdateAsync(dto);
                else { dto.Id = 0; await _service.CreateAsync(dto); }
                saved = true;
                TempData["Success"] = "Training video saved.";
                if(returnProgramId.HasValue) return RedirectToAction("Details", "TrainingProgram", new { id = dto.TrainingProgramId });
                return RedirectToAction(nameof(Index));
            }
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        catch (ConcurrencyConflictException ex)
        {
            ModelState.AddModelError("", ex.Message);
            var current = await _service.GetByIdAsync(dto.Id);
            if (current == null) return NotFound();
            dto.RowVersion = current.RowVersion;
            ModelState.Remove(nameof(dto.RowVersion));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Unable to store uploaded training video");
            ModelState.AddModelError("", "The video could not be saved. Ask your administrator to check storage space and folder permissions, then try again.");
        }
        finally
        {
            if (!saved && uploaded != null)
            {
                try { _uploads.RemoveNewUpload(uploaded); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                { _logger.LogError(ex, "Unable to clean up uncommitted video upload {Filename}", uploaded); }
            }
        }
        dto.VideoUrl = previousUrl;
        if (videoFile != null) ModelState.AddModelError("videoFile", "Please choose the video file again before saving.");
        await SetPrograms(dto.TrainingProgramId);
        return View(editing ? "Edit" : "Create", dto);
    }
    public async Task<IActionResult> Delete(int id)=>await _service.GetByIdAsync(id)is{}dto?View(dto):NotFound();
    [HttpPost,ActionName("Delete"),ValidateAntiForgeryToken]public async Task<IActionResult> DeleteConfirmed(int id){try{await _service.DeleteAsync(id);return RedirectToAction(nameof(Index));}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);var dto=await _service.GetByIdAsync(id);return dto==null?NotFound():View("Delete",dto);}}
    private async Task SetPrograms(int? selected=null)=>ViewBag.Programs=new SelectList(await _programs.GetAllAsync(),"Id","Title",selected);
}
