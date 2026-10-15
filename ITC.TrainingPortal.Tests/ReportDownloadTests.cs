using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ITC.TrainingPortal.Domain.Entities;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Fact]
    public async Task Report_Csv_Button_Downloads_Filtered_File()
    {
        using var factory = new LocalFactory();
        await SeedUnlockProgram(factory, ExamUnlockCondition.AllVideos, 0);
        using var client = Browser(factory);
        using var login = await SignIn(client, "admin");
        var html = await client.GetStringAsync("/admin/reports?EmployeeId=2&DepartmentId=1&TrainingProgramId=10&Status=Not%20started");
        var button = Regex.Match(html, "<a[^>]*>Export CSV</a>").Value;
        var action = Regex.Match(button, "href=\"([^\"]+)\"");
        Assert.True(action.Success, button);
        Assert.Contains("download", button);
        var url = WebUtility.HtmlDecode(action.Groups[1].Value);
        Assert.Contains("EmployeeId=2", url);
        Assert.Contains("DepartmentId=1", url);
        Assert.Contains("TrainingProgramId=10", url);
        Assert.Contains("Status=Not", url);

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("training-compliance.csv", response.Content.Headers.ContentDisposition?.FileName);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Take(3).SequenceEqual(Encoding.UTF8.GetPreamble()));
        Assert.Contains("Employee ID", Encoding.UTF8.GetString(bytes));
    }
}