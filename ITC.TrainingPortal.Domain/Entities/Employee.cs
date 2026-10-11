namespace ITC.TrainingPortal.Domain.Entities;

public class Employee : BaseEntity
{
    public string WorkerType { get; set; } = "Unknown";
    public string EmployeeId { get; set; } = string.Empty; 
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];

    public ICollection<TrainingAssignment> Assignments { get; set; } = new List<TrainingAssignment>();
    public ICollection<VideoProgress> VideoProgresses { get; set; } = new List<VideoProgress>();
    public ICollection<QuizResult> QuizResults { get; set; } = new List<QuizResult>();
}
