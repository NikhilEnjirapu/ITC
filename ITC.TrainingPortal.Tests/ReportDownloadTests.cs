using System.Net;
using System.Text.RegularExpressions;
using System.IO.Compression;
using ITC.TrainingPortal.Domain.Entities;
using PdfSharp.Pdf.IO;

namespace ITC.TrainingPortal.Tests;

public sealed partial class DevelopmentAuthenticationTests
{
    [Theory]
    [InlineData("Excel")]
    [InlineData("Pdf")]
    public async Task Report_Export_Button_Downloads_Filtered_File(string format)
    {
        using var factory=new LocalFactory();await SeedUnlockProgram(factory,ExamUnlockCondition.AllVideos,0);
        using var client=Browser(factory);using var login=await SignIn(client,"admin");
        var html=await client.GetStringAsync("/admin/reports?EmployeeId=2&DepartmentId=1&TrainingProgramId=10&Status=Not%20started");
        var button=Regex.Match(html,$"<a[^>]*>Export {format.ToUpperInvariant() switch { "PDF" => "PDF", _ => "Excel" }}</a>").Value;
        var action=Regex.Match(button,"href=\"([^\"]+)\"");
        Assert.True(action.Success,button);
        Assert.Contains("download",button);
        var url=WebUtility.HtmlDecode(action.Groups[1].Value);
        Assert.Contains("EmployeeId=2",url); Assert.Contains("DepartmentId=1",url); Assert.Contains("TrainingProgramId=10",url); Assert.Contains("Status=Not",url);
        using var response=await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Equal("attachment",response.Content.Headers.ContentDisposition?.DispositionType);
        using var stream=new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        if(format=="Excel") { using var zip=new ZipArchive(stream); Assert.NotNull(zip.GetEntry("xl/worksheets/sheet1.xml")); }
        else { using var pdf=PdfReader.Open(stream,PdfDocumentOpenMode.Import); Assert.True(pdf.PageCount>0); }
    }
}
