namespace ITC.TrainingPortal.Application.Identity;

public interface IEmployeeIdentityResolver
{
    Task<EmployeeIdentityResolution> ResolveAsync(EmployeeIdentityMatchMode matchMode, string identifier);
}
