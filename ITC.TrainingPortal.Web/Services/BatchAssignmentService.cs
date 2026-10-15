using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace ITC.TrainingPortal.Web.Services;

public sealed record AssignmentCandidate(int? Id, string EmployeeId, string Name, string Department, string? SkipReason);
public sealed record AssignmentPreview(string Program, DateTime AssignedAt, DateTime? DueDate,
    IReadOnlyList<AssignmentCandidate> Employees, string Token);
public sealed record AssignmentBatch(int ProgramId, DateTime AssignedAt, DateTime? DueDate, int[] EmployeeIds, DateTime ExpiresAt);

public sealed class BatchAssignmentService(ApplicationDbContext db, AdminWorkflowService workflow)
{
    public const int MaximumEmployees = 1000;
    public const int MaximumCsvBytes = 128 * 1024;

    public static List<string> ReadCsv(Stream stream)
    {
        using var parser = new TextFieldParser(stream, System.Text.Encoding.UTF8, true, leaveOpen: true)
        { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = true };
        parser.SetDelimiters(",");
        try
        {
            var headers = parser.ReadFields();
            if (headers == null || headers.Count(x => x.Equals("EmployeeId", StringComparison.OrdinalIgnoreCase)) != 1)
                throw new InvalidOperationException("CSV must have one EmployeeId column in its header.");
            var column = Array.FindIndex(headers, x => x.Equals("EmployeeId", StringComparison.OrdinalIgnoreCase));
            var ids = new List<string>();
            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields()!;
                if (fields.Length != headers.Length) throw new InvalidOperationException("Each CSV row must have the same number of columns as the header.");
                ids.Add(fields[column].Trim());
                if (ids.Count > MaximumEmployees) throw new InvalidOperationException("Upload no more than 1,000 employee rows at a time.");
            }
            if (ids.Count == 0) throw new InvalidOperationException("The CSV contains no employees.");
            return ids;
        }
        catch (MalformedLineException) { throw new InvalidOperationException("The CSV has invalid quoting. Save it as CSV (comma delimited) and try again."); }
    }

    public async Task<(string Program, List<AssignmentCandidate> Rows)> ResolveAsync(int programId, int[]? selected = null, List<string>? codes = null)
    {
        var program = (await workflow.ProgramsAsync()).SingleOrDefault(x => x.Id == programId);
        if (program == null || !program.IsReady) throw new InvalidOperationException("Select a ready training program before reviewing employees.");
        var employees = await db.Employees.AsNoTracking().Include(x => x.Department).ToListAsync();
        var rules = await db.EligibilityRules.AsNoTracking().ToListAsync();
        var assigned = (await db.TrainingAssignments.AsNoTracking().Where(x => x.TrainingProgramId == programId).Select(x => x.EmployeeId).ToListAsync()).ToHashSet();
        var byId = employees.ToDictionary(x => x.Id);
        var byCode = employees.ToDictionary(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase);
        var requests = codes ?? (selected ?? []).Select(id => byId.TryGetValue(id, out var e) ? e.EmployeeId : $"Unknown record {id}").ToList();
        if (requests.Count == 0 || requests.Count > MaximumEmployees) throw new InvalidOperationException("Select between 1 and 1,000 employees, or upload a CSV.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<AssignmentCandidate>();
        foreach (var code in requests)
        {
            byCode.TryGetValue(code, out var employee);
            var reason = !seen.Add(code) ? "Duplicate in selection" : employee == null ? "Employee ID not found" :
                EligibilityEvaluator.DenialReason(employee, rules) ?? (assigned.Contains(employee.Id) ? "Already assigned" : null);
            rows.Add(new(employee?.Id, code, employee == null ? "" : $"{employee.FirstName} {employee.LastName}", employee?.Department.Name ?? "", reason));
        }
        return (program.Title, rows);
    }
}
