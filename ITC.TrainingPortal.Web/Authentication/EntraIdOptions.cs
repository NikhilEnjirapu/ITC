namespace ITC.TrainingPortal.Web.Authentication;

public sealed class EntraIdOptions
{
    public const string SectionName = "EntraId";

    public bool Enabled { get; set; }
    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/signin-oidc";
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";
}
