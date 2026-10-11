using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Domain.Exceptions;
using ITC.TrainingPortal.Domain.Interfaces;

namespace ITC.TrainingPortal.Tests;

public class FoundationTests
{
    [Fact]
    public void EmployeeDto_RejectsInvalidValues()
    {
        var dto = new EmployeeDto { EmployeeId = "", FirstName = "", LastName = "", Email = "invalid", DepartmentId = 0 };

        var results = Validate(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EmployeeDto.EmployeeId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EmployeeDto.Email)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EmployeeDto.DepartmentId)));
    }

    [Fact]
    public void DepartmentDto_RejectsMissingName()
    {
        Assert.Contains(Validate(new DepartmentDto()), r => r.MemberNames.Contains(nameof(DepartmentDto.Name)));
    }

    [Fact]
    public async Task EmployeeService_GetAll_PopulatesDepartmentName()
    {
        var employees = new FakeRepository<Employee>(new Employee { Id = 1, DepartmentId = 7, EmployeeId = "E1" });
        var departments = new FakeRepository<Department>(new Department { Id = 7, Name = "Finance" });
        var service = new EmployeeService(employees, departments);

        var result = Assert.Single(await service.GetAllAsync());

        Assert.Equal("Finance", result.DepartmentName);
    }

    [Fact]
    public async Task EmployeeService_Create_RejectsUnknownDepartment()
    {
        var service = new EmployeeService(new FakeRepository<Employee>(), new FakeRepository<Department>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(ValidEmployee()));

        Assert.Equal("Selected department does not exist.", exception.Message);
    }

    [Fact]
    public async Task EmployeeService_Create_RejectsDuplicateEmployeeId()
    {
        var employees = new FakeRepository<Employee>(new Employee { Id = 2, EmployeeId = "E1", Email = "other@example.com" });
        var departments = new FakeRepository<Department>(new Department { Id = 1, Name = "Finance" });
        var service = new EmployeeService(employees, departments);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(ValidEmployee()));

        Assert.Equal("Employee ID already exists.", exception.Message);
    }

    [Fact]
    public async Task EmployeeService_Update_RejectsStaleRowVersion()
    {
        var employees = new FakeRepository<Employee>(new Employee
        {
            Id = 3, EmployeeId = "E1", Email = "user@example.com", DepartmentId = 1, RowVersion = [2]
        });
        var departments = new FakeRepository<Department>(new Department { Id = 1, Name = "Finance" });
        var service = new EmployeeService(employees, departments);
        var dto = ValidEmployee();
        dto.Id = 3;
        dto.RowVersion = [1];

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.UpdateAsync(dto));
    }

    [Fact]
    public async Task DepartmentService_Delete_RejectsDepartmentWithEmployees()
    {
        var departments = new FakeRepository<Department>(new Department { Id = 1, Name = "Finance" });
        var employees = new FakeRepository<Employee>(new Employee { Id = 2, DepartmentId = 1 });
        var service = new DepartmentService(departments, employees);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(1));
    }

    private static EmployeeDto ValidEmployee() => new()
    {
        EmployeeId = "E1", FirstName = "Test", LastName = "User", Email = "user@example.com", DepartmentId = 1
    };

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);
        return results;
    }
}

internal sealed class FakeRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly List<T> _items;

    public FakeRepository(params T[] items) => _items = items.ToList();

    public Task<T?> GetByIdAsync(int id) => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));
    public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(_items.ToList());
    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        Task.FromResult<IEnumerable<T>>(_items.AsQueryable().Where(predicate).ToList());
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) =>
        Task.FromResult(_items.AsQueryable().Any(predicate));
    public Task AddAsync(T entity) { _items.Add(entity); return Task.CompletedTask; }
    public void Update(T entity) { }
    public void Remove(T entity) => _items.Remove(entity);
    public Task SaveChangesAsync() => Task.CompletedTask;
}
