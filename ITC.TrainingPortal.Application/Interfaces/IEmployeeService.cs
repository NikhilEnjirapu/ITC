using ITC.TrainingPortal.Application.DTOs;

namespace ITC.TrainingPortal.Application.Interfaces;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task CreateAsync(EmployeeDto dto);
    Task UpdateAsync(EmployeeDto dto);
    Task DeleteAsync(int id);
}
