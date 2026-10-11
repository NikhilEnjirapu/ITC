namespace ITC.TrainingPortal.Domain.Entities;

public class EligibilityRule : BaseEntity
{
    public string RuleType { get; set; } = string.Empty; 
    public string RuleValue { get; set; } = string.Empty; 
    public bool IsActive { get; set; } = true;
}
