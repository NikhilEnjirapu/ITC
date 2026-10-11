using ITC.TrainingPortal.Application.DTOs;

namespace ITC.TrainingPortal.Application.Identity;

public enum EmployeeIdentityResolutionStatus
{
    Resolved,
    Unauthenticated,
    NotConfigured,
    MissingIdentifier,
    EmployeeNotFound,
    AmbiguousIdentity
}

public sealed record EmployeeIdentityResolution(
    EmployeeIdentityResolutionStatus Status,
    EmployeeDto? Employee = null)
{
    public static EmployeeIdentityResolution Resolved(EmployeeDto employee) => new(EmployeeIdentityResolutionStatus.Resolved, employee);
    public static EmployeeIdentityResolution Unauthenticated() => new(EmployeeIdentityResolutionStatus.Unauthenticated);
    public static EmployeeIdentityResolution NotConfigured() => new(EmployeeIdentityResolutionStatus.NotConfigured);
    public static EmployeeIdentityResolution MissingIdentifier() => new(EmployeeIdentityResolutionStatus.MissingIdentifier);
    public static EmployeeIdentityResolution EmployeeNotFound() => new(EmployeeIdentityResolutionStatus.EmployeeNotFound);
    public static EmployeeIdentityResolution AmbiguousIdentity() => new(EmployeeIdentityResolutionStatus.AmbiguousIdentity);
}
