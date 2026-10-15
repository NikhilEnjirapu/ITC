using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Identity;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Web.Authentication;
using ITC.TrainingPortal.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ITC.TrainingPortal.Web.Controllers;

public sealed class QuizController : Controller
{
    private readonly IQuizService _service;
    private readonly ITrainingProgramService _programs;
    private readonly ICurrentEmployeeAccessor _employeeAccessor;
    public QuizController(IQuizService service, ITrainingProgramService programs, ICurrentEmployeeAccessor employeeAccessor)
    { _service = service; _programs = programs; _employeeAccessor = employeeAccessor; }

    [Authorize(Policy = PortalPolicies.Administrator)]
    public async Task<IActionResult> Index() => View(await _service.GetAllAsync());

    [Authorize(Policy = PortalPolicies.Administrator), HttpGet("/admin/exams/{id:int}/questions")]
    public async Task<IActionResult> Details(int id) => await _service.GetAsync(id) is { } quiz
        ? View(new QuizAdminDetailsViewModel(quiz, await _service.GetQuestionsAsync(id))) : NotFound();

    [Authorize(Policy = PortalPolicies.Administrator)]
    public async Task<IActionResult> Create(int? programId)
    {
        if (!(await _programs.GetAllAsync()).Any())
        {
            ViewData["SetupMessage"] = "Create a program and add videos before setting up its exam.";
            ViewData["SetupController"] = "TrainingProgram"; ViewData["SetupAction"] = "Create"; ViewData["SetupLabel"] = "Create program";
            return View("_SetupRequired");
        }
        if(programId.HasValue && await _programs.GetByIdAsync(programId.Value) == null) return NotFound();
        var setup = await HttpContext.RequestServices.GetRequiredService<ITC.TrainingPortal.Web.Services.AdminWorkflowService>().ProgramsAsync();
        if (!setup.Any(x => x.Missing != "Add videos") || (programId.HasValue && setup.Any(x => x.Id == programId && x.Missing == "Add videos")))
        {
            ViewData["SetupMessage"] = "Add videos to the program before creating its exam.";
            ViewData["SetupController"] = "TrainingProgram"; ViewData["SetupAction"] = "Index"; ViewData["SetupLabel"] = "Continue program setup";
            return View("_SetupRequired");
        }
        ViewData["ReturnProgramId"] = programId;
        await ProgramsAsync(programId ?? 0); return View(new QuizDto { TrainingProgramId = programId ?? 0 });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = PortalPolicies.Administrator)]
    public async Task<IActionResult> Create(QuizDto dto, int? returnProgramId)
    {
        ViewData["ReturnProgramId"] = returnProgramId;
        var db = HttpContext.RequestServices.GetRequiredService<ITC.TrainingPortal.Infrastructure.Data.ApplicationDbContext>();
        if (!db.TrainingVideos.Any(x => x.TrainingProgramId == dto.TrainingProgramId)) ModelState.AddModelError("", "Add videos to the selected program before creating its exam.");
        if (ModelState.IsValid) try { var id = await _service.CreateAsync(dto); TempData["Success"]="Exam draft created. Add questions to continue."; return RedirectToAction(nameof(Details), new { id }); }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        await ProgramsAsync(dto.TrainingProgramId); return View(dto);
    }

    [Authorize(Policy = PortalPolicies.Administrator)]
    public async Task<IActionResult> Edit(int id)
    { var dto = await _service.GetAsync(id); if (dto == null) return NotFound(); await ProgramsAsync(dto.TrainingProgramId); return View(dto); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = PortalPolicies.Administrator)]
    public async Task<IActionResult> Edit(int id, QuizDto dto)
    {
        if (id != dto.Id) return BadRequest();
        if (ModelState.IsValid) try { await _service.UpdateAsync(dto); TempData["Success"]="Exam saved."; return RedirectToAction(nameof(Details), new { id }); }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        await ProgramsAsync(dto.TrainingProgramId); return View(dto);
    }

    [Authorize(Policy = PortalPolicies.Administrator)]
    public async Task<IActionResult> Delete(int id) => await _service.GetAsync(id) is { } dto ? View(dto) : NotFound();

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Policy = PortalPolicies.Administrator)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    { try { await _service.DeleteAsync(id); TempData["Success"]="Exam deleted."; return RedirectToAction(nameof(Index)); } catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View("Delete", await _service.GetAsync(id)); } }

    [Authorize(Policy = PortalPolicies.Administrator), HttpGet("/admin/exams/{quizId:int}/questions/add")]
    public async Task<IActionResult> AddQuestion(int quizId) { if(await _service.GetAsync(quizId) == null) return NotFound(); var questions = await _service.GetQuestionsAsync(quizId); return View("QuestionForm", new QuizQuestionDto { QuizId = quizId, SequenceOrder = questions.Select(x => x.SequenceOrder).DefaultIfEmpty(0).Max() + 1 }); }

    [HttpPost("/admin/exams/{quizId:int}/questions/add"), ValidateAntiForgeryToken, Authorize(Policy = PortalPolicies.Administrator)]
    public async Task<IActionResult> AddQuestion(QuizQuestionDto dto, bool addAnother = false)
    { if (ModelState.IsValid) try { await _service.AddQuestionAsync(dto); TempData["Success"] = "Question saved."; return addAnother ? RedirectToAction(nameof(AddQuestion), new { quizId = dto.QuizId }) : RedirectToAction(nameof(Details), new { id = dto.QuizId }); } catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); } return View("QuestionForm", dto); }

    [Authorize(Policy = PortalPolicies.Administrator), HttpGet("/admin/exams/questions/{id:int}/edit")]
    public async Task<IActionResult> EditQuestion(int id) => await _service.GetQuestionAsync(id) is { } dto ? View("QuestionForm", dto) : NotFound();

    [HttpPost("/admin/exams/questions/{id:int}/edit"), ValidateAntiForgeryToken, Authorize(Policy = PortalPolicies.Administrator)]
    public async Task<IActionResult> EditQuestion(int id, QuizQuestionDto dto)
    { if (id != dto.Id) return BadRequest(); if (ModelState.IsValid) try { await _service.UpdateQuestionAsync(dto); return RedirectToAction(nameof(Details), new { id = dto.QuizId }); } catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); } return View("QuestionForm", dto); }

    [HttpPost("/admin/exams/questions/{id:int}/delete"), ValidateAntiForgeryToken, Authorize(Policy = PortalPolicies.Administrator)]
    public async Task<IActionResult> DeleteQuestion(int id, int quizId)
    { try { await _service.DeleteQuestionAsync(id); } catch (InvalidOperationException ex) { TempData["QuizMessage"] = ex.Message; } return RedirectToAction(nameof(Details), new { id = quizId }); }

    [Authorize(Policy = PortalPolicies.Employee), HttpGet("/employee/exams")]
    public async Task<IActionResult> Mine()
    { var employee = await EmployeeAsync(); return employee == null ? Forbid() : View(await _service.GetEmployeeExamsAsync(employee.Id)); }

    [Authorize(Policy = PortalPolicies.Employee), HttpGet("/employee/results")]
    public async Task<IActionResult> Results()
    { var employee = await EmployeeAsync(); return employee == null ? Forbid() : View((await _service.GetEmployeeExamsAsync(employee.Id)).SelectMany(x=>x.Results).OrderByDescending(x=>x.CompletedAt ?? x.AttemptDate).ToList()); }

    [Authorize(Policy = PortalPolicies.Employee), HttpGet("/employee/exams/{id:int}")]
    public async Task<IActionResult> Introduction(int id)
    { var employee = await EmployeeAsync(); if (employee == null) return Forbid(); var access=await _service.GetAccessAsync(employee.Id,id); return access==null ? NotFound() : View(access); }

    [HttpPost("/employee/exams/{id:int}/start"), ValidateAntiForgeryToken, Authorize(Policy = PortalPolicies.Employee)]
    public async Task<IActionResult> Start(int id)
    { var employee = await EmployeeAsync(); if (employee == null) return Forbid(); try { return View("Attempt", await _service.StartOrResumeAsync(employee.Id, id)); } catch (UnauthorizedAccessException) { return Forbid(); } catch (InvalidOperationException ex) { TempData["QuizMessage"] = ex.Message; return RedirectToAction(nameof(Introduction), new { id }); } }

    [Authorize(Policy = PortalPolicies.Employee), HttpGet("/employee/exams/{id:int}/availability")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Availability(int id)
    {
        var employee = await EmployeeAsync(); if(employee == null) return Forbid();
        var access = await _service.GetAccessAsync(employee.Id, id);
        return access == null ? NotFound() : PartialView("_ExamAvailability", access);
    }

    [HttpPost("/employee/exams/submit"), ValidateAntiForgeryToken, Authorize(Policy = PortalPolicies.Employee)]
    public async Task<IActionResult> Submit(int attemptId, [Bind(Prefix = "answers")] Dictionary<int, string> answers)
    { var employee = await EmployeeAsync(); if (employee == null) return Forbid(); try { var result = await _service.SubmitAsync(employee.Id, new ExamSubmissionDto { AttemptId = attemptId, Answers = answers }); return RedirectToAction(nameof(Result), new { id = result.Id }); } catch (UnauthorizedAccessException) { return Forbid(); } catch (InvalidOperationException ex) { TempData["QuizMessage"] = ex.Message; return RedirectToAction(nameof(Mine)); } }

    [Authorize(Policy = PortalPolicies.Employee), HttpGet("/employee/results/{id:int}")]
    public async Task<IActionResult> Result(int id)
    { var employee = await EmployeeAsync(); if (employee == null) return Forbid(); return await _service.GetResultAsync(employee.Id, id) is { } result ? View(result) : NotFound(); }

    private async Task ProgramsAsync(int selected = 0) => ViewBag.TrainingPrograms = new SelectList(await _programs.GetAllAsync(), "Id", "Title", selected);
    private async Task<EmployeeDto?> EmployeeAsync() { var r = await _employeeAccessor.ResolveAsync(User); return r.Status == EmployeeIdentityResolutionStatus.Resolved ? r.Employee : null; }
}

public sealed record QuizAdminDetailsViewModel(QuizDto Quiz, IReadOnlyList<QuizQuestionDto> Questions);
