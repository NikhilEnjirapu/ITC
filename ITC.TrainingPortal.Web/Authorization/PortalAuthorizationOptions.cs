namespace ITC.TrainingPortal.Web.Authorization;

public sealed class PortalAuthorizationOptions
{
    public const string SectionName = "PortalAuthorization";

    public string RoleClaimType { get; set; } = string.Empty;
    public string AdministratorRole { get; set; } = string.Empty;
    public string EmployeeRole { get; set; } = string.Empty;
    public bool AllowDevelopmentBypass { get; set; }
}
