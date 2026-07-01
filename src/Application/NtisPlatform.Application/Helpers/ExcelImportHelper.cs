using ClosedXML.Excel;

namespace NtisPlatform.Application.Helpers;

/// <summary>
/// One parsed data row from an imported worksheet. <see cref="RowNumber"/> is the 1-based Excel row
/// (header is row 1) so it can be quoted in user-facing error messages.
/// </summary>
public sealed record ExcelRow(int RowNumber, IReadOnlyDictionary<string, string?> Cells);

/// <summary>
/// Minimal ClosedXML reader for bulk-update uploads: reads the first worksheet, treats the first row of
/// the used range as the header, and returns the headers plus one <see cref="ExcelRow"/> per data row
/// (header → cell string, case-insensitive keys). Value coercion to CLR types happens downstream.
/// </summary>
public static class ExcelImportHelper
{
    public static (List<string> Headers, List<ExcelRow> Rows) Read(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new ArgumentException("The uploaded workbook contains no worksheets.");

        var range = worksheet.RangeUsed()
            ?? throw new ArgumentException("The uploaded worksheet is empty.");

        var columnCount = range.ColumnCount();
        var rowCount = range.RowCount();

        // Row 1 of the used range is the header. Read by explicit column index so blank header cells
        // keep their position (never shift later columns).
        var headerRow = range.Row(1);
        var headers = new List<string>(columnCount);
        for (var c = 1; c <= columnCount; c++)
            headers.Add(headerRow.Cell(c).GetString().Trim());

        if (headers.All(string.IsNullOrWhiteSpace))
            throw new ArgumentException("The uploaded worksheet has no header row.");

        var duplicateHeaders = headers
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .GroupBy(h => h, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateHeaders.Count > 0)
            throw new ArgumentException($"The uploaded worksheet has duplicate column header(s): {string.Join(", ", duplicateHeaders)}.");

        var rows = new List<ExcelRow>();
        for (var r = 2; r <= rowCount; r++)
        {
            var row = range.Row(r);
            var cells = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var isEmpty = true;

            for (var c = 1; c <= columnCount; c++)
            {
                var header = headers[c - 1];
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                var value = row.Cell(c).GetString().Trim();
                cells[header] = string.IsNullOrEmpty(value) ? null : value;
                if (!string.IsNullOrEmpty(value))
                    isEmpty = false;
            }

            // Skip fully blank rows so trailing empty lines don't become spurious update targets.
            if (isEmpty)
                continue;

            rows.Add(new ExcelRow(row.RowNumber(), cells));
        }

        return (headers, rows);
    }
}
