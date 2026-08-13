using ClosedXML.Excel;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;

namespace NtisPlatform.Application.Services.Asset_Management
{
    public partial class AssetMasterService
    {
        #region Export Excel Methods

        public async Task<byte[]> ExportToExcelAsync(AssetMasterQueryParameters queryParameters, CancellationToken cancellationToken = default)
        {
            // Force PageSize to -1 to get all records, and page number to 1
            queryParameters.PageSize = -1;
            queryParameters.PageNumber = 1;

            var result = await GetAllAsync(queryParameters, cancellationToken);
            var items = result.Items.ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Assets");

            var headers = new[]
            {
                "Asset ID",
                "Asset Name",
                "Asset Category",
                "Asset Type",
                "Owning Department",
                "Capital Value",
                "Ownership Type",
                "Condition",
                "Life (Yrs)",
                "Address"
            };

            for (var c = 0; c < headers.Length; c++)
            {
                var cell = worksheet.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
            }

            for (var r = 0; r < items.Count; r++)
            {
                var item = items[r];
                worksheet.Cell(r + 2, 1).Value = item.AssetNo ?? "";
                worksheet.Cell(r + 2, 2).Value = item.AssetName ?? "";
                worksheet.Cell(r + 2, 3).Value = item.AssetCategoryName ?? "";
                worksheet.Cell(r + 2, 4).Value = item.AssetTypeName ?? "";
                worksheet.Cell(r + 2, 5).Value = item.DepartmentName ?? "";
                worksheet.Cell(r + 2, 6).Value = item.CapitalValue ?? 0m;
                worksheet.Cell(r + 2, 7).Value = item.OwnershipType ?? "";
                worksheet.Cell(r + 2, 8).Value = item.AssetCondition ?? "";
                worksheet.Cell(r + 2, 9).Value = item.AssetLife.HasValue ? item.AssetLife.Value.ToString() : "-";
                worksheet.Cell(r + 2, 10).Value = item.Details?.Address ?? "";
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        #endregion
    }
}
