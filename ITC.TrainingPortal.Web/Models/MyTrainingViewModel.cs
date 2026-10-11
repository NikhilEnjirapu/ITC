using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Identity;

namespace ITC.TrainingPortal.Web.Models;

public sealed class MyTrainingViewModel
{
    public EmployeeIdentityResolutionStatus IdentityStatus { get; init; }
    public string? EmployeeName { get; init; }
    public IReadOnlyDictionary<int, QuizResultDto> ExamResults { get; init; } = new Dictionary<int, QuizResultDto>();
    public IReadOnlyList<EmployeeTrainingDto> Assignments { get; init; } = [];
}
