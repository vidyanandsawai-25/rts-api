using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Helpers;

namespace NtisPlatform.Application.Services;

public partial class TaxZoningRangeService
{
    // ─────────────────────────────────────────────────────────────────────────
    // Ward Abstract exports
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<byte[]> ExportWardAbstractToExcelAsync(
        WardAbstractQueryParameters queryParams,
        string ulbName = "",
        CancellationToken cancellationToken = default)
    {
        var rows = await BuildWardAbstractRowsAsync(queryParams, cancellationToken);
        var zoneLabels = rows.SelectMany(r => r.ZoneCounts.Select(z => z.TaxZoneNo)).Distinct().OrderBy(x => x).ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Ward Abstract");

        var fixedHeaders = new[] { "Ward No.", "Total Properties", "Covered", "Pending", "Coverage %" };
        var allHeaders = fixedHeaders.Concat(zoneLabels.Select(z => $"Zone {z}")).ToArray();
        var totalCols = allHeaders.Length;

        // ── Council name row ─────────────────────────────────────────────────
        var titleCell = ws.Cell(1, 1);
        titleCell.Value = ulbName;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 14;
        titleCell.Style.Font.FontColor = XLColor.FromHtml("#17508E");
        titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(1, 1, 1, totalCols).Merge();

        // ── Report title row ─────────────────────────────────────────────────
        var subtitleCell = ws.Cell(2, 1);
        subtitleCell.Value = "Ward-wise and Zone-wise Zoning Abstract";
        subtitleCell.Style.Font.Bold = true;
        subtitleCell.Style.Font.FontSize = 11;
        subtitleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(2, 1, 2, totalCols).Merge();

        // ── Column header row (row 3) ─────────────────────────────────────────
        for (var c = 0; c < allHeaders.Length; c++)
        {
            var cell = ws.Cell(3, c + 1);
            cell.Value = allHeaders[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#17508E");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // ── Data rows (start at row 4) ───────────────────────────────────────
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var zoneMap = row.ZoneCounts.ToDictionary(z => z.TaxZoneNo, z => z.Count);
            var dataRow = r + 4;

            ws.Cell(dataRow, 1).Value = row.WardNo;
            ws.Cell(dataRow, 1).Style.Font.Bold = true;
            ws.Cell(dataRow, 1).Style.Font.FontColor = XLColor.FromHtml("#123D70");
            ws.Cell(dataRow, 2).Value = row.TotalProperties;
            ws.Cell(dataRow, 3).Value = row.CoveredProperties;
            ws.Cell(dataRow, 4).Value = row.PendingProperties;
            ws.Cell(dataRow, 5).Value = $"{row.CoveragePercent:0.00}%";

            for (var z = 0; z < zoneLabels.Count; z++)
                ws.Cell(dataRow, 6 + z).Value = zoneMap.TryGetValue(zoneLabels[z], out var cnt) ? cnt : 0;
        }

        // ── TOTAL row ────────────────────────────────────────────────────────
        var totalRow = rows.Count + 4;
        ws.Cell(totalRow, 1).Value = "TOTAL";
        ws.Cell(totalRow, 1).Style.Font.Bold = true;
        ws.Cell(totalRow, 2).Value = rows.Sum(r => r.TotalProperties);
        ws.Cell(totalRow, 3).Value = rows.Sum(r => r.CoveredProperties);
        ws.Cell(totalRow, 4).Value = rows.Sum(r => r.PendingProperties);
        var totTotal = rows.Sum(r => r.TotalProperties);
        var totCovered = rows.Sum(r => r.CoveredProperties);
        ws.Cell(totalRow, 5).Value = totTotal == 0 ? "0.00%" : $"{(totCovered * 100.0 / totTotal):0.00}%";
        for (var z = 0; z < zoneLabels.Count; z++)
            ws.Cell(totalRow, 6 + z).Value = rows.Sum(r =>
                r.ZoneCounts.FirstOrDefault(x => x.TaxZoneNo == zoneLabels[z])?.Count ?? 0);

        var totalRange = ws.Range(totalRow, 1, totalRow, totalCols);
        totalRange.Style.Font.Bold = true;
        totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tax Zoning Ranges export — ward-grouped format
    //   Col A : अ क्रे  (Sr. No.)
    //   Col B : मालमत्ता क्र. > पासून  (From Property No.)
    //   Col C : मालमत्ता क्र. > पर्यंत  (To Property No.)
    //   Col D : एकूण मालमत्ता  (Total Properties)
    //   Col E : वस्तीचा प्रकार  (Tax Zone No.)
    //   Col F : पत्ता  (Zone Description)
    // ─────────────────────────────────────────────────────────────────────────

    private const int ExportTotalCols = 6;

    public async Task<byte[]> ExportRangesToExcelAsync(
        TaxZoningRangeQueryParameters queryParams,
        string ulbName = "",
        CancellationToken cancellationToken = default)
    {
        var allParams = new TaxZoningRangeQueryParameters
        {
            PageNumber = 1,
            PageSize = int.MaxValue,
            WardId = queryParams.WardId,
            TaxZoneId = queryParams.TaxZoneId,
            PropertyNo = queryParams.PropertyNo,
            Description = queryParams.Description,
            SearchTerm = queryParams.SearchTerm,
            SortBy = "WardNo",
            SortOrder = "asc",
        };
        var result = await GetAllAsync(allParams, cancellationToken);
        var groups = result.Items
            .GroupBy(i => i.WardNo ?? "")
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Column headers resolve via the DB-backed "TaxZoningRangeExport" resource so they follow the
        // requesting user's language (see LanguageMiddleware / HttpContextKeys.CurrentLanguage);
        // title/subtitle rows below remain fixed Marathi statutory-form text.
        var language = GetLanguage();
        var headerLabels = _localizationService.GetTranslations("TaxZoningRangeExport", language, new[]
        {
            "TaxZoningReport_Col_SrNo",
            "TaxZoningReport_Col_PropertyNo",
            "TaxZoningReport_Col_TotalProperties",
            "TaxZoningReport_Col_TaxZone",
            "TaxZoningReport_Col_Address",
            "TaxZoningReport_Col_Total",
            "TaxZoningReport_Col_GrandTotal",
        });

        // Safe fallback logic if keys are missing from database
        string GetHeaderLabel(string key, string fallback)
        {
            return headerLabels.TryGetValue(key, out var val) && val != key ? val : fallback;
        }

        // Compute current Indian financial year: Apr–Mar
        var now = DateTime.Now;
        var fyStart = now.Month >= 4 ? now.Year : now.Year - 1;
        var fyEnd = (fyStart + 1) % 100;
        var financeYearText = $"सन {fyStart}-{fyEnd:D2} करिता करावयाच्या करमुल्यांकनाच्या कामा करिता";

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Tax Zoning Ranges");

        // ── Row 1: ULB / council name ────────────────────────────────────────
        ExportMergedTitle(ws, 1, ulbName, 13, bold: true);

        // ── Row 2: finance year ───────────────────────────────────────────────
        ExportMergedTitle(ws, 2, financeYearText, 11, bold: false);

        // ── Row 3: document sub-title ─────────────────────────────────────────
        ExportMergedTitle(ws, 3, "वार्ड व मालमत्ता क्र. निहाय वस्तीचा प्रकार यादी", 11, bold: true);

        int row = 4;
        var grandTotalProperties = 0;

        foreach (var wardGroup in groups)
        {
            var items = wardGroup.ToList();

            // ── Ward heading row ─────────────────────────────────────────────
            var wardRange = ws.Range(row, 1, row, ExportTotalCols);
            wardRange.Merge();
            wardRange.Style.Font.Bold = true;
            wardRange.Style.Font.FontSize = 11;
            wardRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wardRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            wardRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            ExportBorder(wardRange);
            ws.Cell(row, 1).Value = $"वार्ड क्र. {wardGroup.Key}";
            row++;

            // ── Column header row 1 of 2 ─────────────────────────────────────
            // A (merged 2 rows): Sr. No.
            // B+C (merged col, row 1 only): Property No.
            // D (merged 2 rows): Total Properties
            // E (merged 2 rows): Type of Use
            // F (merged 2 rows): Address
            ExportColHeaderMergedRows(ws, row, 1, GetHeaderLabel("TaxZoningReport_Col_SrNo", "Sr. No."));              // A spans 2 rows
            var propNoRange = ws.Range(row, 2, row, 3);
            propNoRange.Merge();
            ExportColHeaderStyle(propNoRange);
            ws.Cell(row, 2).Value = GetHeaderLabel("TaxZoningReport_Col_PropertyNo", "Property No.");
            ExportColHeaderMergedRows(ws, row, 4, GetHeaderLabel("TaxZoningReport_Col_TotalProperties", "Total Properties"));   // D spans 2 rows
            ExportColHeaderMergedRows(ws, row, 5, GetHeaderLabel("TaxZoningReport_Col_TaxZone", "Tax Zone"));         // E spans 2 rows
            ExportColHeaderMergedRows(ws, row, 6, GetHeaderLabel("TaxZoningReport_Col_Address", "Address"));           // F spans 2 rows
            row++;

            // ── Column header row 2 of 2 ─────────────────────────────────────
            // A, D, E, F are already merged from row above — apply border only
            ExportBorder(ws.Range(row, 1, row, 1));
            ExportBorder(ws.Range(row, 4, row, 4));
            ExportBorder(ws.Range(row, 5, row, 5));
            ExportBorder(ws.Range(row, 6, row, 6));
            // B: पासून (From), C: पर्यंत (To) — not yet covered by a translation key, left hardcoded Marathi
            ExportColHeaderCell(ws.Cell(row, 2), "पासून");
            ExportColHeaderCell(ws.Cell(row, 3), "पर्यंत");
            row++;

            // ── Data rows ────────────────────────────────────────────────────
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var dataRange = ws.Range(row, 1, row, ExportTotalCols);
                ExportBorder(dataRange);
                if (i % 2 == 1)
                    dataRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F9FAFB");

                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 2).Value = item.FromPropertyNo ?? "";
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 3).Value = item.ToPropertyNo ?? "";
                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 4).Value = item.TotalProperties;
                ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 5).Value = item.TaxZoneNo ?? "";
                ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 6).Value = item.ZoneDescription ?? "";
                ws.Cell(row, 6).Style.Alignment.WrapText = true;
                ws.Row(row).Height = 30;
                row++;
            }

            // ── Ward "total" row — properties covered in zoning for this ward ──
            var wardTotal = items.Sum(x => x.TotalProperties);
            grandTotalProperties += wardTotal;

            ExportBorder(ws.Range(row, 1, row, ExportTotalCols));
            ws.Range(row, 1, row, 3).Merge();
            ws.Cell(row, 1).Value = GetHeaderLabel("TaxZoningReport_Col_Total", "total");
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(row, 4).Value = wardTotal;
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;

            row++; // blank spacer between wards (and before the grand total row)
        }

        // ── Grand total row — total properties covered in zoning across all wards ──
        var grandTotalRange = ws.Range(row, 1, row, ExportTotalCols);
        ExportBorder(grandTotalRange);
        grandTotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
        ws.Range(row, 1, row, 3).Merge();
        ws.Cell(row, 1).Value = GetHeaderLabel("TaxZoningReport_Col_GrandTotal", "Grand Total");
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Cell(row, 4).Value = grandTotalProperties;
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(1).Width = 8;
        ws.Column(2).Width = 14;
        ws.Column(3).Width = 14;
        ws.Column(4).Width = 14;
        ws.Column(5).Width = 18;
        ws.Column(6).Width = 65;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Export helpers ────────────────────────────────────────────────────────

    private static void ExportMergedTitle(IXLWorksheet ws, int row, string text, int fontSize, bool bold)
    {
        var cell = ws.Cell(row, 1);
        cell.Value = text;
        cell.Style.Font.Bold = bold;
        cell.Style.Font.FontSize = fontSize;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(row, 1, row, ExportTotalCols).Merge();
    }

    private static void ExportColHeaderMergedRows(IXLWorksheet ws, int firstRow, int col, string text)
    {
        var r = ws.Range(firstRow, col, firstRow + 1, col);
        r.Merge();
        ExportColHeaderStyle(r);
        ws.Cell(firstRow, col).Value = text;
    }

    private static void ExportColHeaderStyle(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#17508E");
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ExportBorder(range);
    }

    private static void ExportColHeaderCell(IXLCell cell, string text)
    {
        cell.Value = text;
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#17508E");
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ExportBorder(cell.AsRange());
    }

    private static void ExportBorder(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = XLColor.FromHtml("#9CA3AF");
        range.Style.Border.InsideBorderColor = XLColor.FromHtml("#9CA3AF");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<List<WardZoningAbstractDto>> BuildWardAbstractRowsAsync(
        WardAbstractQueryParameters queryParams,
        CancellationToken cancellationToken)
    {
        var wardQuery = _wardRepository.GetQueryable().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
            wardQuery = wardQuery.Where(w => w.WardNo.Contains(queryParams.SearchTerm));
        wardQuery = wardQuery.OrderBy(w => w.WardNo);

        var wards = await wardQuery.ToListAsync(cancellationToken);
        var wardIds = wards.Select(w => w.Id).ToList();
        var rangesByWard = await GetActiveRangesByWardAsync(wardIds, cancellationToken);
        var taxZoneNos = await _taxZoneRepository.GetQueryable().AsNoTracking()
            .Select(z => new { z.Id, z.TaxZoneNo })
            .ToDictionaryAsync(z => z.Id, z => z.TaxZoneNo, cancellationToken);

        // Batch fetch all properties for the targeted wards to avoid N+1 queries
        var allProperties = await _propertyRepository.GetQueryable().AsNoTracking()
            .Where(p => !p.MarkedForDeletion && wardIds.Contains(p.WardId))
            .Select(p => new { p.WardId, p.PropertyNo })
            .ToListAsync(cancellationToken);

        var propertiesByWard = allProperties
            .GroupBy(p => p.WardId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.PropertyNo).ToList());

        var result = new List<WardZoningAbstractDto>();

        foreach (var ward in wards)
        {
            var wardPropertyNos = propertiesByWard.GetValueOrDefault(ward.Id) ?? new List<string?>();

            var wardRanges = rangesByWard.GetValueOrDefault(ward.Id) ?? new List<ActiveRangeBounds>();

            // Coverage is derived from currently-active TaxZoningRange bounds, not the denormalized
            // PropertyMast.TaxZoneId column, so it stays in sync the instant a range is deleted/edited.
            var total = wardPropertyNos.Count;
            var countsByZone = new Dictionary<int, int>();
            var covered = 0;

            foreach (var propertyNo in wardPropertyNos)
            {
                var zoneId = MatchZone(propertyNo, wardRanges);
                if (zoneId == null)
                    continue;

                covered++;
                countsByZone[zoneId.Value] = countsByZone.GetValueOrDefault(zoneId.Value) + 1;
            }

            var zoneCounts = countsByZone.Select(kv => new WardZoningAbstractZoneCountDto
            {
                TaxZoneId = kv.Key,
                TaxZoneNo = taxZoneNos.GetValueOrDefault(kv.Key, string.Empty),
                Count = kv.Value
            }).ToList();

            result.Add(new WardZoningAbstractDto
            {
                WardId = ward.Id,
                WardNo = ward.WardNo,
                TotalProperties = total,
                CoveredProperties = covered,
                PendingProperties = total - covered,
                CoveragePercent = total == 0 ? 0 : Math.Round(covered * 100.0 / total, 2),
                ZoneCounts = zoneCounts
            });
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pending properties export
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<byte[]> ExportPendingPropertiesToExcelAsync(int? wardId = null, string ulbName = "", CancellationToken cancellationToken = default)
    {
        var propQuery = _propertyRepository.GetQueryable().AsNoTracking().Where(p => !p.MarkedForDeletion);
        if (wardId.HasValue)
        {
            var w = wardId.Value;
            propQuery = propQuery.Where(p => p.WardId == w);
        }

        var properties = await propQuery
            .Select(p => new { p.WardId, p.PropertyNo, p.PartitionNo })
            .ToListAsync(cancellationToken);

        var relevantWardIds = properties.Select(p => p.WardId).Distinct().ToList();
        var rangesByWard = await GetActiveRangesByWardAsync(relevantWardIds, cancellationToken);

        var wardNos = await _wardRepository.GetQueryable().AsNoTracking()
            .Select(w => new { w.Id, w.WardNo })
            .ToDictionaryAsync(w => w.Id, w => w.WardNo, cancellationToken);

        var pending = properties
            .Where(p => !rangesByWard.TryGetValue(p.WardId, out var wardRanges) || MatchZone(p.PropertyNo, wardRanges) == null)
            .Select(p => new
            {
                WardNo = wardNos.GetValueOrDefault(p.WardId, string.Empty),
                p.PropertyNo,
                p.PartitionNo
            })
            .OrderBy(p => p.WardNo, PropertyRangeMatcher.Comparer)
            .ThenBy(p => p.PropertyNo ?? string.Empty, PropertyRangeMatcher.Comparer)
            .ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Pending Properties");

        const int totalCols = 3;

        // ── Row 1: ULB / council name ────────────────────────────────────────
        var titleCell = ws.Cell(1, 1);
        titleCell.Value = ulbName;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 14;
        titleCell.Style.Font.FontColor = XLColor.FromHtml("#17508E");
        titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(1, 1, 1, totalCols).Merge();

        // ── Row 2: column headers ────────────────────────────────────────────
        var headers = new[] { "Ward No.", "Property No.", "Partition No." };
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(2, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#17508E");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // ── Data rows (start at row 3) ────────────────────────────────────────
        for (var i = 0; i < pending.Count; i++)
        {
            var row = i + 3;
            ws.Cell(row, 1).Value = pending[i].WardNo;
            ws.Cell(row, 2).Value = pending[i].PropertyNo ?? "";
            ws.Cell(row, 3).Value = pending[i].PartitionNo ?? "";
        }

        ws.Columns().AdjustToContents();

        using var ms = new System.IO.MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Bulk-update template
    // ─────────────────────────────────────────────────────────────────────────

    public byte[] GenerateBulkTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Tax Zoning Template");

        var headers = new[] { "Ward No.", "Property From", "Property To", "Tax Zone", "Zone Description" };
        var colWidths = new[] { 14, 16, 16, 14, 60 };

        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#17508E");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Column(c + 1).Width = colWidths[c];
        }

        using var ms = new System.IO.MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

}
