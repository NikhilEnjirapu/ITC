using ITC.TrainingPortal.Application.DTOs;
namespace ITC.TrainingPortal.Application.Interfaces;
public interface ITrainingProgramService
{
    Task<IEnumerable<TrainingProgramDto>> GetAllAsync();
    Task<TrainingProgramDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(TrainingProgramDto dto);
    Task UpdateAsync(TrainingProgramDto dto);
    Task DeleteAsync(int id);
}
