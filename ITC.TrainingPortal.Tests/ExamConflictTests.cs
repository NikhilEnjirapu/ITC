using System.Reflection;
using System.Security.Claims;
using ITC.TrainingPortal.Application.DTOs;
using ITC.TrainingPortal.Application.Identity;
using ITC.TrainingPortal.Application.Interfaces;
using ITC.TrainingPortal.Domain.Exceptions;
using ITC.TrainingPortal.Web.Authentication;
using ITC.TrainingPortal.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ITC.TrainingPortal.Tests;

public class ExamConflictTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Concurrent_Exam_Request_Returns_Recovery_Redirect(bool start)
    {
        var service = DispatchProxy.Create<IQuizService, ConflictService>();
        var context = new DefaultHttpContext();
        var controller = new QuizController(service, null!, new Accessor())
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, new MemoryTempData())
        };
        var response = start ? await controller.Start(10) : await controller.Submit(1, new());
        var redirect = Assert.IsType<RedirectToActionResult>(response);
        Assert.Equal(start ? "Introduction" : "Mine", redirect.ActionName);
        Assert.NotNull(controller.TempData["QuizMessage"]);
    }

    public class ConflictService : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new ConcurrencyConflictException("Concurrent save");
    }
    private sealed class Accessor : ICurrentEmployeeAccessor
    {
        public Task<EmployeeIdentityResolution> ResolveAsync(ClaimsPrincipal principal) =>
            Task.FromResult(EmployeeIdentityResolution.Resolved(new EmployeeDto { Id = 1 }));
    }
    private sealed class MemoryTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
