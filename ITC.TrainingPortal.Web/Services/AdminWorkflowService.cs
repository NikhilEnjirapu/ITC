using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ITC.TrainingPortal.Web.Services;

public sealed record ProgramReadiness(int Id, string Title, string? Missing)
{
    public bool IsReady => Missing == null;
}

public sealed class AdminWorkflowService(ApplicationDbContext db)
{
    public async Task<List<ProgramReadiness>> ProgramsAsync()
    {
        var programs = await db.TrainingPrograms.AsNoTracking().Include(x => x.Videos)
            .Include(x => x.Quiz).ThenInclude(x => x!.Questions).OrderBy(x => x.Title).ToListAsync();
        return programs.Select(p => new ProgramReadiness(p.Id, p.Title,
            p.Videos.Count == 0 ? "Add videos" : p.Quiz == null ? "Create exam" :
            p.Quiz.Questions.Count == 0 ? "Add exam questions" : !p.Quiz.IsActive ? "Activate exam" :
            p.ExamUnlockCondition == ExamUnlockCondition.SpecificVideo && !p.Videos.Any(v => v.Id == p.ExamUnlockVideoId)
                ? "Choose an available unlock video" : null)).ToList();
    }

    public async Task<List<Employee>> EligibleEmployeesAsync()
    {
        var rules = await db.EligibilityRules.AsNoTracking().ToListAsync();
        return (await db.Employees.AsNoTracking().OrderBy(x => x.EmployeeId).ToListAsync())
            .Where(x => EligibilityEvaluator.DenialReason(x, rules) == null).ToList();
    }

    public async Task ValidateAssignmentAsync(int employeeId, int programId, int? assignmentId = null)
    {
        if (assignmentId.HasValue)
        {
            var existing = await db.TrainingAssignments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == assignmentId);
            if (existing == null) throw new InvalidOperationException("This assignment no longer exists.");
            if (existing.EmployeeId == employeeId && existing.TrainingProgramId == programId) return;
            var videoIds = db.TrainingVideos.Where(x => x.TrainingProgramId == existing.TrainingProgramId).Select(x => x.Id);
            var quizIds = db.Quizzes.Where(x => x.TrainingProgramId == existing.TrainingProgramId).Select(x => x.Id);
            if (await db.VideoProgresses.AnyAsync(x => x.EmployeeId == existing.EmployeeId && videoIds.Contains(x.TrainingVideoId)) ||
                await db.QuizResults.AnyAsync(x => x.EmployeeId == existing.EmployeeId && quizIds.Contains(x.QuizId)))
                throw new InvalidOperationException("Learning has started. Keep the employee and program unchanged; you can still update the due date.");
        }
        var program = (await ProgramsAsync()).SingleOrDefault(x => x.Id == programId);
        if (program == null || !program.IsReady)
            throw new InvalidOperationException(program == null ? "Select an existing training program." : $"Finish program setup before assigning: {program.Missing}.");
        if (!(await EligibleEmployeesAsync()).Any(x => x.Id == employeeId))
            throw new InvalidOperationException("Select an eligible employee. Review the employee's worker category and eligibility rules.");
    }
}
