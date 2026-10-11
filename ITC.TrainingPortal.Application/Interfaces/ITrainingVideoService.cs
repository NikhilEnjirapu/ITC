using ITC.TrainingPortal.Application.DTOs;
namespace ITC.TrainingPortal.Application.Interfaces;
public interface ITrainingVideoService
{
    Task<IEnumerable<TrainingVideoDto>> GetAllAsync();
    Task<TrainingVideoDto?> GetByIdAsync(int id);
    Task CreateAsync(TrainingVideoDto dto);
    Task UpdateAsync(TrainingVideoDto dto);
    Task DeleteAsync(int id);
}
