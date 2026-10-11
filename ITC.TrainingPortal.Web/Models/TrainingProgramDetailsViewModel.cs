using ITC.TrainingPortal.Application.DTOs;
namespace ITC.TrainingPortal.Web.Models;
public sealed class TrainingProgramDetailsViewModel
{
    public required TrainingProgramDto Program { get; init; }
    public QuizDto? Exam { get; init; }
    public IReadOnlyList<TrainingVideoDto> Videos { get; init; }=[];
    public IReadOnlyList<TrainingAssignmentDto> Assignments { get; init; }=[];
}
