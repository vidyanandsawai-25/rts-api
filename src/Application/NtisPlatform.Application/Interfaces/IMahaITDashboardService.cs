using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NtisPlatform.Application.DTOs.AapleSarkar;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

public interface IMahaITDashboardService
{
    Task<List<MahaITDashboardRowDto>> GenerateMonthlySummaryAsync(int year, int month, CancellationToken ct = default);
    Task<MahaITDashboardPushResultDto> PushToMahaITDashboardAsync(int year, int month, CancellationToken ct = default);
    Task<List<RTSMahaITDashboardReportEntity>> GetDashboardSummaryAsync(int year, int month, CancellationToken ct = default);
    Task<List<RTSMahaITDashboardPushLogEntity>> GetPushLogsAsync(int limit = 50, CancellationToken ct = default);
}
