using ITC.TrainingPortal.Application.DTOs;
namespace ITC.TrainingPortal.Application.Interfaces;
public interface ITrainingAssignmentService
{
    Task<IEnumerable<TrainingAssignmentDto>> GetAllAsync();
    Task<IReadOnlyList<EmployeeTrainingDto>> GetEmployeeTrainingAsync(int employeeId);
    Task<TrainingAssignmentDto?> GetByIdAsync(int id);
    Task CreateAsync(TrainingAssignmentDto dto);
    Task UpdateAsync(TrainingAssignmentDto dto);
    Task DeleteAsync(int id);
}
