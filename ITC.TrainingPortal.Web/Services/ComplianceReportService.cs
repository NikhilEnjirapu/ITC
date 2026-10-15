using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ITC.TrainingPortal.Web.Services;

public sealed record ComplianceRow(string EmployeeId, string Employee, string Department, string Program,
    int CompletedVideos, int TotalVideos, string Exam, int? Score, string Status, DateTime? DueDate, int AssignmentId = 0, int EmployeeRecordId = 0, int ProgramId = 0);

public sealed class ReportFilter
{
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public int? TrainingProgramId { get; set; }
    public string? Status { get; set; }
}

public sealed class ComplianceReportService(ApplicationDbContext db)
{
    public async Task<IReadOnlyList<ComplianceRow>> GetAsync(ReportFilter filter)
    {
        var query = db.TrainingAssignments.AsNoTracking().AsQueryable();
        if (filter.EmployeeId.HasValue) query = query.Where(x => x.EmployeeId == filter.EmployeeId);
        if (filter.DepartmentId.HasValue) query = query.Where(x => x.Employee.DepartmentId == filter.DepartmentId);
        if (filter.TrainingProgramId.HasValue) query = query.Where(x => x.TrainingProgramId == filter.TrainingProgramId);
        var assignments = await query.Include(x => x.Employee).ThenInclude(x => x.Department).Include(x => x.TrainingProgram).ToListAsync();
        var employeeIds = assignments.Select(x => x.EmployeeId).Distinct().ToList();
        var programIds = assignments.Select(x => x.TrainingProgramId).Distinct().ToList();
        var videos = await db.TrainingVideos.AsNoTracking().Where(x => programIds.Contains(x.TrainingProgramId)).ToListAsync();
        var progress = await db.VideoProgresses.AsNoTracking().Where(x => employeeIds.Contains(x.EmployeeId)).ToListAsync();
        var quizzes = await db.Quizzes.AsNoTracking().Where(x => programIds.Contains(x.TrainingProgramId)).ToListAsync();
        var results = await db.QuizResults.AsNoTracking().Where(x => employeeIds.Contains(x.EmployeeId)).ToListAsync();
        var rows = assignments.Select(a =>
        {
            var programVideos = videos.Where(x => x.TrainingProgramId == a.TrainingProgramId).Select(x => x.Id).ToHashSet();
            var completed = progress.Count(x => x.EmployeeId == a.EmployeeId && x.IsCompleted && programVideos.Contains(x.TrainingVideoId));
            var quiz = quizzes.SingleOrDefault(x => x.TrainingProgramId == a.TrainingProgramId);
            var attempts = results.Where(x => x.EmployeeId == a.EmployeeId && x.QuizId == quiz?.Id).OrderByDescending(x => x.AttemptDate).ToList();
            var active = attempts.Any(x => !x.IsCompleted);
            var passed = attempts.Any(x => x.IsCompleted && !x.RequiresReview && x.IsPassed);
            var completedAttempts = attempts.Where(x => x.IsCompleted).ToList();
            var best = completedAttempts.Where(x => !x.RequiresReview).OrderByDescending(x => x.ScorePercentage).FirstOrDefault();
            var awaitingReview = completedAttempts.Any(x => x.RequiresReview);
            var exam = active ? "In progress" : passed ? "Passed" : awaitingReview ? "Awaiting review" : best != null ? "Failed" : quiz == null ? "Not configured" : "Not attempted";
            var complete = programVideos.Count > 0 && completed == programVideos.Count && passed && !active;
            var started = progress.Any(x => x.EmployeeId == a.EmployeeId && programVideos.Contains(x.TrainingVideoId) && x.CurrentPositionSeconds > 0) || attempts.Count > 0;
            var status = complete ? "Completed" : a.DueDate < DateTime.UtcNow ? "Overdue" : started ? "In progress" : "Not started";
            return new ComplianceRow(a.Employee.EmployeeId, $"{a.Employee.FirstName} {a.Employee.LastName}", a.Employee.Department.Name,
                a.TrainingProgram.Title, completed, programVideos.Count, exam, best?.ScorePercentage, status, a.DueDate, a.Id, a.EmployeeId, a.TrainingProgramId);
        });
        if (!string.IsNullOrWhiteSpace(filter.Status)) rows = rows.Where(x => x.Status == filter.Status);
        return rows.OrderBy(x => x.EmployeeId).ThenBy(x => x.Program).ToList();
    }
}
