using ITC.TrainingPortal.Domain.Entities;

namespace ITC.TrainingPortal.Application.Services;

public static class EligibilityEvaluator
{
    public static readonly string[] WorkerTypes = ["Unknown", "Employee", "Contractor", "ThirdParty", "Intern", "Temporary"];
    public static readonly string[] RuleTypes = ["EmployeeIdPrefix", "EmployeeId", "EmailDomain", "WorkerType", "DepartmentId"];

    public static string? DenialReason(Employee employee, IEnumerable<EligibilityRule> rules)
    {
        var id = employee.EmployeeId.Trim();
        if (id.Length == 0) return "Employee ID is missing.";
        if (id.StartsWith("T", StringComparison.OrdinalIgnoreCase) || id.StartsWith("S", StringComparison.OrdinalIgnoreCase))
            return "Employee ID prefix is excluded.";
        if (!string.Equals(employee.WorkerType, "Employee", StringComparison.OrdinalIgnoreCase))
            return "Worker category is excluded or has not been verified.";
        foreach (var rule in rules.Where(x => x.IsActive))
        {
            var value = rule.RuleValue.Trim();
            var matches = rule.RuleType switch
            {
                "EmployeeIdPrefix" => id.StartsWith(value, StringComparison.OrdinalIgnoreCase),
                "EmployeeId" => string.Equals(id, value, StringComparison.OrdinalIgnoreCase),
                "EmailDomain" => employee.Email.EndsWith("@" + value.TrimStart('@'), StringComparison.OrdinalIgnoreCase),
                "WorkerType" => string.Equals(employee.WorkerType, value, StringComparison.OrdinalIgnoreCase),
                "DepartmentId" => employee.DepartmentId.ToString(System.Globalization.CultureInfo.InvariantCulture) == value,
                _ => true // Invalid active rules fail closed.
            };
            if (matches) return $"Excluded by rule {rule.Id} ({rule.RuleType}).";
        }
        return null;
    }
}
