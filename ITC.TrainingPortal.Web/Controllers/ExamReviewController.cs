using System.Text.Json;
using ITC.TrainingPortal.Infrastructure.Data;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITC.TrainingPortal.Web.Controllers;

[Authorize(Policy = PortalPolicies.Administrator)]
[Route("admin/exam-reviews")]
public sealed class ExamReviewController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await db.QuizResults.AsNoTracking()
        .Include(x => x.Employee).Include(x => x.Quiz)
        .Where(x => x.IsCompleted && x.RequiresReview).OrderBy(x => x.CompletedAt).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Review(int id)
    {
        var result = await db.QuizResults.AsNoTracking().Include(x => x.Employee).Include(x => x.Quiz)
            .ThenInclude(x => x.Questions).SingleOrDefaultAsync(x => x.Id == id && x.IsCompleted);
        return result == null ? NotFound() : View(result);
    }

    [HttpPost("{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, [Bind(Prefix = "marks")] Dictionary<int, bool> marks, string? rowVersion)
    {
        var result = await db.QuizResults.Include(x => x.Employee).Include(x => x.Quiz)
            .ThenInclude(x => x.Questions).SingleOrDefaultAsync(x => x.Id == id && x.IsCompleted);
        if (result == null) return NotFound();
        if (!result.RequiresReview || Convert.ToBase64String(result.RowVersion) != (rowVersion ?? ""))
            return Conflict("This result has already changed. Reload it before reviewing.");
        var typed = result.Quiz.Questions.Where(x => x.IsTypedAnswer && string.IsNullOrWhiteSpace(x.AnswerKeywords)).ToList();
        if (!ModelState.IsValid || marks.Count != typed.Count || typed.Any(x => !marks.ContainsKey(x.Id)))
        {
            ModelState.AddModelError("", "Mark every typed answer correct or incorrect.");
            return View(result);
        }
        var answers = JsonSerializer.Deserialize<Dictionary<int, string>>(result.SubmittedAnswersJson ?? "{}")!;
        foreach (var question in result.Quiz.Questions.Where(x => x.IsTypedAnswer && !string.IsNullOrWhiteSpace(x.AnswerKeywords)))
            marks[question.Id] = ITC.TrainingPortal.Application.Services.KeywordAnswerMatcher.IsMatch(answers.GetValueOrDefault(question.Id), question.AnswerKeywords);
        var correct = result.Quiz.Questions.Count(x => x.IsTypedAnswer ? marks[x.Id] :
            answers.TryGetValue(x.Id, out var answer) && string.Equals(answer, x.CorrectOption, StringComparison.OrdinalIgnoreCase));
        result.ScorePercentage = (int)Math.Round(correct * 100d / result.Quiz.Questions.Count, MidpointRounding.AwayFromZero);
        result.IsPassed = result.ScorePercentage >= result.Quiz.PassingScorePercentage;
        result.RequiresReview = false;
        result.ManualMarksJson = JsonSerializer.Serialize(marks);
        result.ReviewedBy = User.FindFirst("oid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
        result.ReviewedAt = result.UpdatedAt = DateTime.UtcNow;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("Another administrator changed this result. Reload it."); }
        return RedirectToAction(nameof(Review), new { id });
    }
}
