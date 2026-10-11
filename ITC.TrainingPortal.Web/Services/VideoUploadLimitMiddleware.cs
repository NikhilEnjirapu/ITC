namespace ITC.TrainingPortal.Web.Services;

public sealed class VideoUploadLimitMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var controller = context.Request.RouteValues["controller"]?.ToString();
        var action = context.Request.RouteValues["action"]?.ToString();
        if (HttpMethods.IsPost(context.Request.Method) && controller == "TrainingVideo" &&
            action is "Create" or "Edit" && context.Request.ContentLength > VideoUploadService.MaxRequestBytes)
        {
            // Reject before antiforgery reads the oversized multipart body.
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync("""
                <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Video is too large</title></head>
                <body><main><h1>Video is too large</h1><p>The maximum video size is 500 MB. Compress the video or choose a smaller MP4 or WebM file, then try again.</p><p>Your video was not saved.</p><p><a href="/admin/training-videos">Back to training videos</a></p></main></body></html>
                """);
            return;
        }
        await next(context);
    }
}
