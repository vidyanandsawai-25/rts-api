using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Interfaces;

public interface IAapleSarkarIntegrationService
{
    string DecodeTrackId(string? tdToken);
    Task<bool> NotifyDocumentPendingAsync(string tdToken, CancellationToken ct = default);
    Task<bool> NotifyPaymentPendingAsync(string tdTokenOrAppNo, CancellationToken ct = default);
    Task<bool> MapApplicationAsync(string tdTokenOrTrackId, string applicationNo, string? status = null, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(string applicationNo, string status, string? remark = null, CancellationToken ct = default);
    Task<bool> PushStatusToMahaITSoapAsync(string trackIdOrAppNo, string statusName, string? remark = null, string? transactionId = null, CancellationToken ct = default);
    Task<(bool success, string redirectUrl, string? errorMessage)> ProcessCallbackAsync(string str, string ns, int? ulbId, int? ulbDistrict, CancellationToken ct = default);
}
