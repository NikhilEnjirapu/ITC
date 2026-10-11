using ITC.TrainingPortal.Web.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ITC.TrainingPortal.Web.Authentication;

// Remove the controller from MVC discovery entirely when local sign-in is unavailable.
public sealed class DevelopmentLoginConvention(bool enabled) : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        if (enabled) return;
        foreach (var controller in application.Controllers.Where(x => x.ControllerType.AsType() == typeof(DevelopmentLoginController)).ToList())
            application.Controllers.Remove(controller);
    }
}
