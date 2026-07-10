using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NtisPlatform.Api.Hubs;

/// <summary>
/// Hub that pushes report status changes to the user who submitted the report.
/// Clients call Subscribe() once after connecting to join their personal group.
/// The worker notifies the platform via POST /api/Report/worker/notify, which
/// then broadcasts to the appropriate group.
/// </summary>
[Authorize(Policy = "ReportHub")]
public sealed class ReportStatusHub : Hub
{
    public async Task Subscribe()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
    }
}
