using System.IO.Compression;
using System.Xml.Linq;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace ITC.TrainingPortal.Web.Services;

public static class ReportExporter
{
    static ReportExporter() { GlobalFontSettings.UseWindowsFontsUnderWindows = true; }
    private static readonly string[] Headers = ["Employee ID", "Employee", "Department", "Program", "Completed videos", "Total videos", "Exam", "Latest score", "Status", "Due date (UTC)"];
    private static string[] Values(ComplianceRow row) => [row.EmployeeId, row.Employee, row.Department, row.Program,
        row.CompletedVideos.ToString(), row.TotalVideos.ToString(), row.Exam, row.Score?.ToString() ?? "", row.Status, row.DueDate?.ToString("yyyy-MM-dd") ?? ""];

    public static byte[] Excel(IReadOnlyList<ComplianceRow> rows)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            void Write(string name, string content) { using var writer = new StreamWriter(zip.CreateEntry(name).Open()); writer.Write(content); }
            Write("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Write("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Write("xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Compliance\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Write("xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            // Inline strings preserve Unicode and never execute employee data as spreadsheet formulas.
            var data = new[] { Headers }.Concat(rows.Select(Values));
            var sheet = new XElement(ns + "worksheet", new XElement(ns + "sheetData", data.Select(values =>
                new XElement(ns + "row", values.Select(value => new XElement(ns + "c", new XAttribute("t", "inlineStr"),
                    new XElement(ns + "is", new XElement(ns + "t", value))))))));
            Write("xl/worksheets/sheet1.xml", sheet.ToString());
        }
        return output.ToArray();
    }

    public static byte[] Pdf(IReadOnlyList<ComplianceRow> rows)
    {
        using var document = new PdfDocument();
        document.Info.Title = "Employee training compliance";
        var font = new XFont("Arial", 10);
        XGraphics? graphics = null;
        double y = 0;
        void NewPage()
        {
            graphics?.Dispose();
            var page = document.AddPage(); graphics = XGraphics.FromPdfPage(page); y = 40;
            graphics.DrawString($"Training compliance — {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC — Page {document.PageCount}", font, XBrushes.Black, 35, y); y += 25;
        }
        void Line(string value)
        {
            // Wrap by measured width, including long IDs and unbroken titles.
            while (value.Length > 0)
            {
                if (y > 790) NewPage();
                var length = value.Length;
                while (length > 1 && graphics!.MeasureString(value[..length], font).Width > 520) length--;
                graphics!.DrawString(value[..length], font, XBrushes.Black, 35, y); y += 15;
                value = value[length..];
            }
        }
        NewPage();
        if (rows.Count == 0) Line("No matching assignments.");
        foreach (var row in rows)
        {
            var values = Values(row);
            for (var i = 0; i < Headers.Length; i++) Line($"{Headers[i]}: {values[i]}");
            y += 15;
        }
        graphics?.Dispose();
        using var output = new MemoryStream(); document.Save(output, false); return output.ToArray();
    }
}
