using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Domain.Exceptions;
using ITC.TrainingPortal.Domain.Interfaces;

namespace ITC.TrainingPortal.Application.Services;

public sealed class TrainingAssignmentService : ITrainingAssignmentService
{
    private readonly IRepository<TrainingAssignment> _assignments;
    private readonly IRepository<Employee> _employees;
    private readonly IRepository<Department> _departments;
    private readonly IRepository<TrainingProgram> _programs;
    private readonly IRepository<TrainingVideo> _videos;
    private readonly IRepository<VideoProgress> _progress;
    private readonly IRepository<Quiz> _quizzes;

    public TrainingAssignmentService(IRepository<TrainingAssignment> assignments, IRepository<Employee> employees,
        IRepository<Department> departments, IRepository<TrainingProgram> programs, IRepository<TrainingVideo> videos,
        IRepository<VideoProgress> progress, IRepository<Quiz> quizzes)
    {
        _assignments = assignments;
        _employees = employees;
        _departments = departments;
        _programs = programs;
        _videos = videos;
        _progress = progress;
        _quizzes = quizzes;
    }

    public async Task<IEnumerable<TrainingAssignmentDto>> GetAllAsync()
    {
        var employeeRecords = (await _employees.GetAllAsync()).ToList();
        var employees = employeeRecords.ToDictionary(x => x.Id, x => $"{x.EmployeeId} - {x.FirstName} {x.LastName}");
        var departments = (await _departments.GetAllAsync()).ToDictionary(x => x.Id, x => x.Name);
        var employeeDepartments = employeeRecords.ToDictionary(x => x.Id, x => departments.GetValueOrDefault(x.DepartmentId));
        var programs = (await _programs.GetAllAsync()).ToDictionary(x => x.Id, x => x.Title);
        return (await _assignments.GetAllAsync()).OrderByDescending(x => x.AssignedAt)
            .Select(x => Map(x, employees.GetValueOrDefault(x.EmployeeId), employeeDepartments.GetValueOrDefault(x.EmployeeId), programs.GetValueOrDefault(x.TrainingProgramId)));
    }

    public async Task<TrainingAssignmentDto?> GetByIdAsync(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id);
        if (assignment == null) return null;
        var employee = await _employees.GetByIdAsync(assignment.EmployeeId);
        var department = employee == null ? null : await _departments.GetByIdAsync(employee.DepartmentId);
        var program = await _programs.GetByIdAsync(assignment.TrainingProgramId);
        return Map(assignment, employee == null ? null : $"{employee.EmployeeId} - {employee.FirstName} {employee.LastName}", department?.Name, program?.Title);
    }

    public async Task<IReadOnlyList<EmployeeTrainingDto>> GetEmployeeTrainingAsync(int employeeId)
    {
        var assignments = (await _assignments.FindAsync(x => x.EmployeeId == employeeId)).OrderByDescending(x => x.AssignedAt).ToList();
        var programs = (await _programs.GetAllAsync()).ToDictionary(x => x.Id);
        var videos = (await _videos.GetAllAsync()).GroupBy(x => x.TrainingProgramId).ToDictionary(x => x.Key, x => x.ToList());
        var completedVideoIds = (await _progress.FindAsync(x => x.EmployeeId == employeeId && x.IsCompleted)).Select(x => x.TrainingVideoId).ToHashSet();
        var quizzes = (await _quizzes.GetAllAsync()).ToDictionary(x => x.TrainingProgramId, x => x.Title);
        return assignments.Where(x => programs.ContainsKey(x.TrainingProgramId)).Select(assignment =>
        {
            var program = programs[assignment.TrainingProgramId];
            var programVideos = videos.GetValueOrDefault(program.Id) ?? [];
            var orderedVideos = programVideos.OrderBy(x => x.SequenceOrder).ToList();
            var currentVideo = orderedVideos.FirstOrDefault(x => !completedVideoIds.Contains(x.Id));
            return new EmployeeTrainingDto
            {
                AssignmentId = assignment.Id, TrainingProgramId = program.Id, TrainingProgramTitle = program.Title,
                Description = program.Description, AssignedAt = assignment.AssignedAt, DueDate = assignment.DueDate,
                VideoCount = programVideos.Count, CompletedVideoCount = programVideos.Count(x => completedVideoIds.Contains(x.Id)),
                ExamStatus = quizzes.ContainsKey(program.Id) ? "Exam configured" : "No exam configured",
                CurrentVideoId = currentVideo?.Id ?? orderedVideos.LastOrDefault()?.Id,
                CurrentVideoTitle = currentVideo?.Title
            };
        }).ToList();
    }

    public async Task CreateAsync(TrainingAssignmentDto dto)
    {
        await ValidateAsync(dto);
        await _assignments.AddAsync(new TrainingAssignment { EmployeeId = dto.EmployeeId, TrainingProgramId = dto.TrainingProgramId, AssignedAt = dto.AssignedAt, DueDate = dto.DueDate });
        await _assignments.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingAssignmentDto dto)
    {
        await ValidateAsync(dto);
        var entity = await _assignments.GetByIdAsync(dto.Id) ?? throw new ConcurrencyConflictException("The assignment was deleted by another user.");
        if (!entity.RowVersion.SequenceEqual(dto.RowVersion)) throw new ConcurrencyConflictException("The assignment was changed by another user. Reload it and try again.");
        entity.EmployeeId = dto.EmployeeId; entity.TrainingProgramId = dto.TrainingProgramId; entity.AssignedAt = dto.AssignedAt;
        entity.DueDate = dto.DueDate; entity.UpdatedAt = DateTime.UtcNow;
        _assignments.Update(entity);
        await _assignments.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _assignments.GetByIdAsync(id);
        if (entity == null) return;
        _assignments.Remove(entity);
        await _assignments.SaveChangesAsync();
    }

    private async Task ValidateAsync(TrainingAssignmentDto dto)
    {
        if (!await _employees.AnyAsync(x => x.Id == dto.EmployeeId)) throw new InvalidOperationException("Selected employee does not exist.");
        var existing = dto.Id == 0 ? null : await _assignments.GetByIdAsync(dto.Id);
        var retainingAssignment = existing != null && existing.EmployeeId == dto.EmployeeId && existing.TrainingProgramId == dto.TrainingProgramId;
        if (!await _programs.AnyAsync(x => x.Id == dto.TrainingProgramId && (!x.IsArchived || retainingAssignment)))
            throw new InvalidOperationException("Selected training program does not exist or is archived.");
        if (dto.DueDate.HasValue && dto.DueDate.Value < dto.AssignedAt) throw new InvalidOperationException("Due date cannot be earlier than the assignment date.");
        if (await _assignments.AnyAsync(x => x.EmployeeId == dto.EmployeeId && x.TrainingProgramId == dto.TrainingProgramId && x.Id != dto.Id)) throw new InvalidOperationException("This training program is already assigned to the employee.");
    }

    private static TrainingAssignmentDto Map(TrainingAssignment assignment, string? employee, string? department, string? program) => new()
    {
        Id = assignment.Id, EmployeeId = assignment.EmployeeId, EmployeeName = employee, DepartmentName = department,
        TrainingProgramId = assignment.TrainingProgramId, TrainingProgramTitle = program, AssignedAt = assignment.AssignedAt,
        DueDate = assignment.DueDate, RowVersion = assignment.RowVersion
    };
}
