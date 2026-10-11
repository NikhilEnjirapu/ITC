using ITC.TrainingPortal.Application.DTOs;
namespace ITC.TrainingPortal.Web.Models;
public sealed class DashboardViewModel
{
    public int CompletedAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public bool IsDataAvailable { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalPrograms { get; set; }
    public int TotalVideos { get; set; }
    public int TotalAssignments { get; set; }
    public IReadOnlyList<TrainingAssignmentDto> RecentAssignments { get; set; }=[];
}
