using System.Threading.Channels;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.BackgroundTasks;

/// <summary>
/// Channel-backed <see cref="IBackgroundTaskQueue"/> -- an unbounded in-memory queue. Work is lost
/// on process restart/app-pool recycle (no persistence), which is acceptable for its current use
/// (re-running a recalculation that a later certificate save would re-trigger anyway), but makes
/// this unsuitable for work that must survive a crash.
/// </summary>
public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _channel =
        Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public void QueueWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        if (!_channel.Writer.TryWrite(workItem))
        {
            throw new InvalidOperationException("Background task queue is no longer accepting work items.");
        }
    }

    public async Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
