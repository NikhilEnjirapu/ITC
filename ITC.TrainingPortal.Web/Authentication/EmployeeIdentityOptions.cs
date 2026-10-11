namespace ITC.TrainingPortal.Web.Authentication;

public sealed class EmployeeIdentityOptions
{
    public const string SectionName = "EmployeeIdentity";

    public string MatchMode { get; set; } = nameof(Application.Identity.EmployeeIdentityMatchMode.NotConfigured);
    public string UserIdClaimType { get; set; } = string.Empty;
    public string EmailClaimType { get; set; } = string.Empty;
}
