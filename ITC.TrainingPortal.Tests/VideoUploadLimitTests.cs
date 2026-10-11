using ITC.TrainingPortal.Web.Services;
using Microsoft.AspNetCore.Http;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Theory]
    [InlineData("/admin/training-videos/Create")]
    [InlineData("/admin/training-videos/Edit/1")]
    public async Task Oversized_Admin_Upload_Returns_Useful_Error_Instead_Of_Blank_Antiforgery_Error(string path)
    {
        using var factory = new LocalFactory();
        await factory.AddIsolatedTestEmployees();
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        using var content = new ByteArrayContent([]);
        content.Headers.ContentLength = 783919750;
        content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("multipart/form-data; boundary=oversized-test");
        using var response = await client.PostAsync(path, content);
        Assert.Equal(System.Net.HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Contains("Back to training videos", await response.Content.ReadAsStringAsync());
    }
}

public sealed class VideoUploadLimitTests
{
    [Theory]
    [InlineData("Create")]
    [InlineData("Edit")]
    public async Task Oversized_Upload_Is_Explained_Before_Form_Is_Read(string action)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.RouteValues["controller"] = "TrainingVideo";
        context.Request.RouteValues["action"] = action;
        context.Request.ContentLength = 783919750;
        context.Response.Body = new MemoryStream();
        var nextCalled = false;
        await new VideoUploadLimitMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }).InvokeAsync(context);
        Assert.False(nextCalled);
        Assert.Equal(413, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var html = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("500 MB", html);
        Assert.Contains("Your video was not saved", html);
    }

    [Theory]
    [InlineData(100L)]
    [InlineData(VideoUploadService.MaxRequestBytes)]
    [InlineData(null)]
    public async Task Other_Uploads_Continue_To_Normal_Validation(long? length)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.RouteValues["controller"] = "TrainingVideo";
        context.Request.RouteValues["action"] = "Create";
        context.Request.ContentLength = length;
        var nextCalled = false;
        await new VideoUploadLimitMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }).InvokeAsync(context);
        Assert.True(nextCalled);
    }
}
