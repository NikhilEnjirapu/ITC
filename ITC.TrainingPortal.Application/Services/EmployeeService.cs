using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Domain.Interfaces;
using ITC.TrainingPortal.Domain.Exceptions;

namespace ITC.TrainingPortal.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IRepository<Employee> _repository;
    private readonly IRepository<Department> _deptRepository;

    public EmployeeService(IRepository<Employee> repository, IRepository<Department> deptRepository)
    {
        _repository = repository;
        _deptRepository = deptRepository;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        var departments = (await _deptRepository.GetAllAsync()).ToDictionary(d => d.Id, d => d.Name);
        return entities.Select(e => Map(e, departments.GetValueOrDefault(e.DepartmentId)));
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var e = await _repository.GetByIdAsync(id);
        if (e == null) return null;
        var department = await _deptRepository.GetByIdAsync(e.DepartmentId);
        return Map(e, department?.Name);
    }

    public async Task CreateAsync(EmployeeDto dto)
    {
        Normalize(dto);
        bool deptExists = await _deptRepository.AnyAsync(d => d.Id == dto.DepartmentId);
        if (!deptExists) throw new InvalidOperationException("Selected department does not exist.");

        bool empIdExists = await _repository.AnyAsync(e => e.EmployeeId == dto.EmployeeId);
        if (empIdExists) throw new InvalidOperationException("Employee ID already exists.");

        bool emailExists = await _repository.AnyAsync(e => e.Email == dto.Email);
        if (emailExists) throw new InvalidOperationException("Email already exists.");

        var entity = new Employee { EmployeeId = dto.EmployeeId, FirstName = dto.FirstName, LastName = dto.LastName, Email = dto.Email, DepartmentId = dto.DepartmentId, WorkerType = dto.WorkerType, UserId = dto.UserId };
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
    }

    public async Task UpdateAsync(EmployeeDto dto)
    {
        Normalize(dto);
        bool deptExists = await _deptRepository.AnyAsync(d => d.Id == dto.DepartmentId);
        if (!deptExists) throw new InvalidOperationException("Selected department does not exist.");

        bool empIdExists = await _repository.AnyAsync(e => e.EmployeeId == dto.EmployeeId && e.Id != dto.Id);
        if (empIdExists) throw new InvalidOperationException("Employee ID already exists.");

        bool emailExists = await _repository.AnyAsync(e => e.Email == dto.Email && e.Id != dto.Id);
        if (emailExists) throw new InvalidOperationException("Email already exists.");

        var entity = await _repository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            throw new ConcurrencyConflictException("The employee was deleted by another user.");
        }

        if (!entity.RowVersion.SequenceEqual(dto.RowVersion))
        {
            throw new ConcurrencyConflictException("The employee was changed by another user. Reload it and try again.");
        }

        entity.WorkerType = dto.WorkerType;
        entity.UserId = dto.UserId;
        entity.EmployeeId = dto.EmployeeId;
        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.Email = dto.Email;
        entity.DepartmentId = dto.DepartmentId;
        entity.UpdatedAt = DateTime.UtcNow;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity != null)
        {
            _repository.Remove(entity);
            await _repository.SaveChangesAsync();
        }
    }

    private static EmployeeDto Map(Employee employee, string? departmentName) => new()
    {
        Id = employee.Id,
        WorkerType = employee.WorkerType,
        UserId = employee.UserId,
        EmployeeId = employee.EmployeeId,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        Email = employee.Email,
        DepartmentId = employee.DepartmentId,
        DepartmentName = departmentName,
        RowVersion = employee.RowVersion
    };

    private static void Normalize(EmployeeDto dto)
    {
        dto.UserId = dto.UserId?.Trim() ?? string.Empty;
        if (!EligibilityEvaluator.WorkerTypes.Contains(dto.WorkerType)) throw new InvalidOperationException("Invalid worker category.");
        dto.EmployeeId = dto.EmployeeId?.Trim() ?? string.Empty;
        dto.FirstName = dto.FirstName?.Trim() ?? string.Empty;
        dto.LastName = dto.LastName?.Trim() ?? string.Empty;
        dto.Email = dto.Email?.Trim() ?? string.Empty;
    }
}
