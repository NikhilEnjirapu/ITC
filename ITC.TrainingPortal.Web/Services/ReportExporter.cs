using System.Text;

namespace ITC.TrainingPortal.Web.Services;

public static class ReportExporter
{
    private static readonly string[] Headers = ["Employee ID", "Employee", "Department", "Program", "Completed videos", "Total videos", "Exam", "Best score", "Status", "Due date (UTC)"];
    private static string[] Values(ComplianceRow row) => [row.EmployeeId, row.Employee, row.Department, row.Program,
        row.CompletedVideos.ToString(), row.TotalVideos.ToString(), row.Exam, row.Score?.ToString() ?? "", row.Status, row.DueDate?.ToString("yyyy-MM-dd") ?? ""];

    public static byte[] Csv(IReadOnlyList<ComplianceRow> rows)
    {
        static string Cell(string value)
        {
            if (value.Length > 0 && (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')) value = "'" + value;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        var lines = new[] { Headers }.Concat(rows.Select(Values)).Select(values => string.Join(',', values.Select(Cell)));
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(string.Join("\r\n", lines) + "\r\n")).ToArray();
    }
}