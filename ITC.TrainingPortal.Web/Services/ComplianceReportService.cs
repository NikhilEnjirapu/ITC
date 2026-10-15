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
    public async Task<IReadOnlyList<ComplianceRow>> GetAsync(ReportFilter filter, int skip = 0, int? take = null)
    {
        var assignments = db.TrainingAssignments.AsNoTracking().AsQueryable();
        if (filter.EmployeeId.HasValue) assignments = assignments.Where(x => x.EmployeeId == filter.EmployeeId);
        if (filter.DepartmentId.HasValue) assignments = assignments.Where(x => x.Employee.DepartmentId == filter.DepartmentId);
        if (filter.TrainingProgramId.HasValue) assignments = assignments.Where(x => x.TrainingProgramId == filter.TrainingProgramId);
        var summaries = assignments.Select(a => new
        {
            a.Id, a.EmployeeId, a.TrainingProgramId, a.DueDate,
            Code = a.Employee.EmployeeId, Name = a.Employee.FirstName + " " + a.Employee.LastName,
            Department = a.Employee.Department.Name, Program = a.TrainingProgram.Title,
            Total = a.TrainingProgram.Videos.Count(),
            Completed = a.TrainingProgram.Videos.Count(v => db.VideoProgresses.Any(p => p.EmployeeId == a.EmployeeId && p.TrainingVideoId == v.Id && p.IsCompleted)),
            HasQuiz = a.TrainingProgram.Quiz != null,
            Active = db.QuizResults.Any(r => r.EmployeeId == a.EmployeeId && r.Quiz.TrainingProgramId == a.TrainingProgramId && !r.IsCompleted),
            Passed = db.QuizResults.Any(r => r.EmployeeId == a.EmployeeId && r.Quiz.TrainingProgramId == a.TrainingProgramId && r.IsCompleted && !r.RequiresReview && r.IsPassed),
            Review = db.QuizResults.Any(r => r.EmployeeId == a.EmployeeId && r.Quiz.TrainingProgramId == a.TrainingProgramId && r.IsCompleted && r.RequiresReview),
            Best = db.QuizResults.Where(r => r.EmployeeId == a.EmployeeId && r.Quiz.TrainingProgramId == a.TrainingProgramId && r.IsCompleted && !r.RequiresReview).Select(r => (int?)r.ScorePercentage).Max(),
            Started = db.VideoProgresses.Any(p => p.EmployeeId == a.EmployeeId && p.TrainingVideo.TrainingProgramId == a.TrainingProgramId && p.CurrentPositionSeconds > 0) ||
                db.QuizResults.Any(r => r.EmployeeId == a.EmployeeId && r.Quiz.TrainingProgramId == a.TrainingProgramId)
        });
        var now = DateTime.UtcNow;
        var statuses = summaries.Select(x => new
        {
            Summary = x,
            Exam = x.Passed ? "Passed" : x.Active ? "In progress" : x.Review ? "Awaiting review" : x.Best != null ? "Failed" : !x.HasQuiz ? "Not configured" : "Not attempted",
            Status = x.Total > 0 && x.Completed == x.Total && x.Passed ? "Completed" : x.DueDate < now ? "Overdue" : x.Started ? "In progress" : "Not started"
        });
        if (!string.IsNullOrWhiteSpace(filter.Status)) statuses = statuses.Where(x => x.Status == filter.Status);
        var page = statuses.OrderBy(x => x.Summary.Code).ThenBy(x => x.Summary.Program).ThenBy(x => x.Summary.Id).Skip(Math.Max(0, skip)).AsQueryable();
        if (take.HasValue) page = page.Take(take.Value);
        return await page.Select(x => new ComplianceRow(x.Summary.Code, x.Summary.Name, x.Summary.Department,
            x.Summary.Program, x.Summary.Completed, x.Summary.Total, x.Exam, x.Summary.Best, x.Status,
            x.Summary.DueDate, x.Summary.Id, x.Summary.EmployeeId, x.Summary.TrainingProgramId)).ToListAsync();
    }
}
