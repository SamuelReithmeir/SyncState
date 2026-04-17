using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncState.Interfaces;
using SyncState.Models.Configuration;

namespace SyncState.ReloadInterval;

public class ReloadBackgroundWorker : BackgroundService
{
    private readonly SyncStateConfiguration _syncStateConfiguration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ReloadBackgroundWorker> _logger;

    public ReloadBackgroundWorker(SyncStateConfiguration syncStateConfiguration,
        IServiceScopeFactory serviceScopeFactory, ILogger<ReloadBackgroundWorker> logger)
    {
        _syncStateConfiguration = syncStateConfiguration;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_syncStateConfiguration.GetExtension<TimerExtension>() is not { } timerExtension)
        {
            return;
        }

        await foreach (var interval in GetMixedIntervalsAsync(timerExtension.Intervals.ToArray(), stoppingToken))
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var commandService = scope.ServiceProvider.GetRequiredService<ISyncCommandService>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await commandService.HandleAsync(new TimedReloadCommand
            {
                Interval = interval
            }, stoppingToken);
            stopwatch.Stop();
            _logger.LogDebug("Executed TimedReloadCommand for interval {Interval} in {ElapsedMs} ms", interval, stopwatch.ElapsedMilliseconds);
        }
    }

    private static async IAsyncEnumerable<TimeSpan> GetMixedIntervalsAsync(
        TimeSpan[] intervals,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (intervals.Length == 0)
        {
            yield break;
        }

        // Create one delay task per interval
        var tasks = new Task<TimeSpan>[intervals.Length];

        for (var i = 0; i < intervals.Length; i++)
        {
            var interval = intervals[i];
            tasks[i] = Delay(interval, cancellationToken);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            // Wait for any interval to elapse
            var completed = await Task.WhenAny(tasks).ConfigureAwait(false);

            // Find which interval fired
            var index = Array.IndexOf(tasks, completed);

            // Propagate cancellation or faults immediately
            var elapsedInterval = await completed.ConfigureAwait(false);

            yield return elapsedInterval;

            // Restart the completed interval
            tasks[index] = Delay(elapsedInterval, cancellationToken);
        }
    }

    private static async Task<TimeSpan> Delay(
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        return interval;
    }
}