// New file: Backend/FormBuilder.API/Services/ReportExportService.cs
// NuGet required: ClosedXML (for Excel)

using ClosedXML.Excel;
using FormBuilder.API.DTOs;

namespace FormBuilder.API.Services;

public interface IReportExportService
{
    byte[] ExportToExcel(RunReportResult result, string reportName);
    byte[] ExportToCsv(RunReportResult result);
}

public class ReportExportService : IReportExportService
{
    public byte[] ExportToExcel(RunReportResult result, string reportName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Report");

        // ---- Title row ----
        ws.Cell(1, 1).Value = reportName;
        ws.Cell(1, 1).Style.Font.Bold      = true;
        ws.Cell(1, 1).Style.Font.FontSize  = 14;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A5F");
        ws.Range(1, 1, 1, result.Columns.Count).Merge();

        // ---- Generated date ----
        ws.Cell(2, 1).Value = $"Generated: {DateTime.Now:dd MMM yyyy HH:mm}";
        ws.Cell(2, 1).Style.Font.FontSize  = 10;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Range(2, 1, 2, result.Columns.Count).Merge();

        // ---- Row count ----
        ws.Cell(3, 1).Value = $"Total Rows: {result.TotalCount:N0}";
        ws.Cell(3, 1).Style.Font.FontSize  = 10;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Range(3, 1, 3, result.Columns.Count).Merge();

        // ---- Header row ----
        int headerRow = 5;
        for (int i = 0; i < result.Columns.Count; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = result.Columns[i];
            cell.Style.Font.Bold            = true;
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#1D4ED8");
        }

        // ---- Data rows ----
        for (int r = 0; r < result.Rows.Count; r++)
        {
            var row     = result.Rows[r];
            bool isEven = r % 2 == 1;

            for (int c = 0; c < result.Columns.Count; c++)
            {
                var cell  = ws.Cell(headerRow + 1 + r, c + 1);
                var value = row.GetValueOrDefault(result.Columns[c]);

                if (value == null)
                    cell.Value = "";
                else if (value is DateTime dt)
                    cell.Value = dt.ToString("dd/MM/yyyy HH:mm");
                else if (value is decimal or double or float or int or long)
                    cell.Value = Convert.ToDouble(value);
                else
                    cell.Value = value.ToString();

                if (isEven)
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F7FF");

                cell.Style.Border.BottomBorder      = XLBorderStyleValues.Thin;
                cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#E2E8F0");
            }
        }

        // ---- Auto-fit columns ----
        ws.Columns().AdjustToContents();

        // ---- Freeze header ----
        ws.SheetView.FreezeRows(headerRow);

        // ---- Add SQL sheet ----
        if (!string.IsNullOrWhiteSpace(result.GeneratedSql))
        {
            var sqlWs = wb.Worksheets.Add("Generated SQL");
            sqlWs.Cell(1, 1).Value = result.GeneratedSql;
            sqlWs.Cell(1, 1).Style.Font.FontName = "Courier New";
            sqlWs.Cell(1, 1).Style.Font.FontSize = 10;
            sqlWs.Column(1).Width = 120;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportToCsv(RunReportResult result)
    {
        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine(string.Join(",", result.Columns.Select(CsvEscape)));

        // Rows
        foreach (var row in result.Rows)
        {
            var values = result.Columns.Select(col =>
                CsvEscape(row.GetValueOrDefault(col)?.ToString() ?? ""));
            sb.AppendLine(string.Join(",", values));
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
