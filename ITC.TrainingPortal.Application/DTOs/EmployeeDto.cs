using System.ComponentModel.DataAnnotations;

namespace ITC.TrainingPortal.Application.DTOs;

public class EmployeeDto
{
    [Required, RegularExpression("Unknown|Employee|Contractor|ThirdParty|Intern|Temporary")]
    [Display(Name = "Worker category")]
    public string WorkerType { get; set; } = "Unknown";

    [StringLength(450), Display(Name = "SSO user identifier")]
    public string UserId { get; set; } = string.Empty;
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Employee ID")]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please select a department.")]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
