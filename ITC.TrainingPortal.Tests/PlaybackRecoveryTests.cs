using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ITC.TrainingPortal.Domain.Entities;
using ITC.TrainingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Fact]
    public async Task Rejected_Jump_Returns_Saved_Position_And_Allows_Recovery()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 0);
        using var client = Browser(factory);
        using var login = await SignIn(client, "employee");
        var html = await client.GetStringAsync("/employee/video/101");
        using var jump = new HttpRequestMessage(HttpMethod.Post, "/employee/video/heartbeat")
        {
            Content = JsonContent.Create(new { videoId = 101, positionSeconds = 90, playbackState = "playing" })
        };
        jump.Headers.Add("RequestVerificationToken", Token(html));
        using var rejected = await client.SendAsync(jump);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        var body = await rejected.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("playback_resync", body.GetProperty("code").GetString());
        Assert.Equal(0, body.GetProperty("acceptedPositionSeconds").GetInt32());
        using var resume = new HttpRequestMessage(HttpMethod.Post, "/employee/video/heartbeat")
        {
            Content = JsonContent.Create(new { videoId = 101, positionSeconds = 0, playbackState = "paused" })
        };
        resume.Headers.Add("RequestVerificationToken", Token(html));
        using var recovered = await client.SendAsync(resume);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        using var scope = factory.Services.CreateScope();
        var progress = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().VideoProgresses.SingleAsync();
        Assert.Equal(0, progress.CurrentPositionSeconds);
        Assert.False(progress.IsCompleted);
        Assert.Equal("paused", progress.PlaybackState);
    }
}
