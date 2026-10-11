using System.Reflection;
using ITC.TrainingPortal.Web.Authorization;
using ITC.TrainingPortal.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ITC.TrainingPortal.Tests;

public sealed class RouteSeparationTests
{
    [Theory]
    [InlineData(typeof(AccountController), "Login", "/login")]
    [InlineData(typeof(AccountController), "AccessDenied", "/access-denied")]
    [InlineData(typeof(HomeController), "Index", "/admin")]
    [InlineData(typeof(TrainingController), "Dashboard", "/employee")]
    [InlineData(typeof(TrainingController), "Dashboard", "/employee/dashboard")]
    [InlineData(typeof(TrainingController), "MyTraining", "/employee/training")]
    [InlineData(typeof(TrainingController), "Content", "/employee/training/{id:int}")]
    [InlineData(typeof(TrainingController), "Player", "/employee/video/{id:int}")]
    [InlineData(typeof(QuizController), "Mine", "/employee/exams")]
    [InlineData(typeof(QuizController), "Introduction", "/employee/exams/{id:int}")]
    [InlineData(typeof(QuizController), "Result", "/employee/results/{id:int}")]
    public void Public_Route_Is_Explicit(Type controller, string action, string template)
    {
        var method = controller.GetMethods().Single(x => x.DeclaringType == controller && x.Name == action);
        var templates = method.GetCustomAttributes<HttpMethodAttribute>().Select(x => x.Template);
        Assert.Contains(template, templates);
    }

    [Fact]
    public void Admin_Management_Controllers_Require_Admin_Policy()
    {
        Type[] controllers = [typeof(HomeController), typeof(EmployeeController), typeof(DepartmentController),
            typeof(TrainingProgramController), typeof(TrainingVideoController), typeof(TrainingAssignmentController),
            typeof(ReportController), typeof(AuditController)];
        Assert.All(controllers, type => Assert.Contains(type.GetCustomAttributes<AuthorizeAttribute>(),
            x => x.Policy == PortalPolicies.Administrator));
    }

    [Fact]
    public void Employee_Personal_Controller_Requires_Employee_Policy() =>
        Assert.Contains(typeof(TrainingController).GetCustomAttributes<AuthorizeAttribute>(),
            x => x.Policy == PortalPolicies.Employee);

    [Fact]
    public void Quiz_Admin_And_Employee_Actions_Use_Separate_Policies()
    {
        Assert.Equal(PortalPolicies.Administrator, Policy(nameof(QuizController.Index)));
        Assert.Equal(PortalPolicies.Employee, Policy(nameof(QuizController.Mine)));
    }

    [Fact]
    public void Root_Landing_Is_A_Role_Aware_Redirect_Endpoint() =>
        Assert.Contains("/", typeof(PortalController).GetMethod(nameof(PortalController.Index))!
            .GetCustomAttributes<HttpMethodAttribute>().Select(x => x.Template));

    private static string? Policy(string action) => typeof(QuizController).GetMethod(action)!
        .GetCustomAttribute<AuthorizeAttribute>()?.Policy;
}
