using ITC.TrainingPortal.Application.DTOs;

namespace ITC.TrainingPortal.Application.Interfaces;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentDto>> GetAllAsync();
    Task<DepartmentDto?> GetByIdAsync(int id);
    Task CreateAsync(DepartmentDto dto);
    Task UpdateAsync(DepartmentDto dto);
    Task DeleteAsync(int id);
}
