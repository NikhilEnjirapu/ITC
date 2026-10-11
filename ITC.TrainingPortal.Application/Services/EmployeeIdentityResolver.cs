using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Identity;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Domain.Interfaces;

namespace ITC.TrainingPortal.Application.Services;

public sealed class EmployeeIdentityResolver : IEmployeeIdentityResolver
{
    private readonly IRepository<Employee> _employees;
    private readonly IRepository<Department> _departments;

    public EmployeeIdentityResolver(IRepository<Employee> employees, IRepository<Department> departments)
    {
        _employees = employees;
        _departments = departments;
    }

    public async Task<EmployeeIdentityResolution> ResolveAsync(EmployeeIdentityMatchMode matchMode, string identifier)
    {
        if (matchMode == EmployeeIdentityMatchMode.NotConfigured)
        {
            return EmployeeIdentityResolution.NotConfigured();
        }

        var normalizedIdentifier = identifier.Trim();
        var matches = matchMode switch
        {
            EmployeeIdentityMatchMode.UserId => await _employees.FindAsync(e => e.UserId == normalizedIdentifier),
            EmployeeIdentityMatchMode.Email => await _employees.FindAsync(e => e.Email == normalizedIdentifier),
            _ => []
        };

        var matchingEmployees = matches.Take(2).ToList();
        if (matchingEmployees.Count > 1)
        {
            return EmployeeIdentityResolution.AmbiguousIdentity();
        }

        var employee = matchingEmployees.SingleOrDefault();
        if (employee == null)
        {
            return EmployeeIdentityResolution.EmployeeNotFound();
        }

        var department = await _departments.GetByIdAsync(employee.DepartmentId);
        return EmployeeIdentityResolution.Resolved(new EmployeeDto
        {
            Id = employee.Id,
            WorkerType = employee.WorkerType,
            UserId = employee.UserId,
            EmployeeId = employee.EmployeeId,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            DepartmentId = employee.DepartmentId,
            DepartmentName = department?.Name,
            RowVersion = employee.RowVersion
        });
    }
}
