using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Domain.Interfaces;
using ITC.TrainingPortal.Domain.Exceptions;

namespace ITC.TrainingPortal.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IRepository<Department> _repository;
    private readonly IRepository<Employee> _employeeRepository;

    public DepartmentService(IRepository<Department> repository, IRepository<Employee> employeeRepository)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(Map);
    }

    public async Task<DepartmentDto?> GetByIdAsync(int id)
    {
        var e = await _repository.GetByIdAsync(id);
        if (e == null) return null;
        return Map(e);
    }

    public async Task CreateAsync(DepartmentDto dto)
    {
        Normalize(dto);
        var entity = new Department { Name = dto.Name, Description = dto.Description };
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
    }

    public async Task UpdateAsync(DepartmentDto dto)
    {
        Normalize(dto);
        var entity = await _repository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            throw new ConcurrencyConflictException("The department was deleted by another user.");
        }

        if (!entity.RowVersion.SequenceEqual(dto.RowVersion))
        {
            throw new ConcurrencyConflictException("The department was changed by another user. Reload it and try again.");
        }

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.UpdatedAt = DateTime.UtcNow;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity != null)
        {
            bool hasEmployees = await _employeeRepository.AnyAsync(e => e.DepartmentId == id);
            if (hasEmployees)
            {
                throw new InvalidOperationException("Cannot delete department because it has associated employees.");
            }

            _repository.Remove(entity);
            await _repository.SaveChangesAsync();
        }
    }

    private static DepartmentDto Map(Department department) => new()
    {
        Id = department.Id,
        Name = department.Name,
        Description = department.Description,
        RowVersion = department.RowVersion
    };

    private static void Normalize(DepartmentDto dto)
    {
        dto.Name = dto.Name?.Trim() ?? string.Empty;
        dto.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
    }
}
