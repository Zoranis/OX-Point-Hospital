using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OxBox.Application.Abstractions;
using OxBox.Application.Boxes;

namespace OxBox.Infrastructure.Boxes;

/// <summary>
/// Runs one <see cref="BoxAgent"/> per configured box for the lifetime of the server. Boxes
/// added in the web app, or whose BU address changed, are started or restarted on request.
/// </summary>
internal sealed partial class BoxSupervisor(
    IServiceProvider services,
    IServiceScopeFactory scopeFactory,
    TimeProvider time,
    ILogger<BoxSupervisor> logger) : BackgroundService, IBoxAgents
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    private readonly Dictionary<int, RunningAgent> _agents = [];
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TaskCompletionSource<CancellationToken> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task StartOrRestartAsync(int boxId, CancellationToken cancellationToken)
    {
        var stoppingToken = await _started.Task.WaitAsync(cancellationToken);
        await using var scope = scopeFactory.CreateAsyncScope();
        var endpoint = await scope.ServiceProvider.GetRequiredService<IBoxStore>().GetBoxEndpointAsync(boxId, cancellationToken)
            ?? throw new InvalidOperationException($"Box {boxId} does not exist.");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_agents.Remove(boxId, out var running))
            {
                await running.StopAsync();
            }

            Start(endpoint, stoppingToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public override void Dispose()
    {
        _gate.Dispose();
        foreach (var agent in _agents.Values)
        {
            agent.Stopping.Dispose();
        }

        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var boxes = await LoadBoxesAsync(stoppingToken);
        LogStarting(boxes.Count);

        await _gate.WaitAsync(stoppingToken);
        try
        {
            foreach (var box in boxes)
            {
                Start(box, stoppingToken);
            }
        }
        finally
        {
            _gate.Release();
        }

        _started.TrySetResult(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutting down: every agent stops with the same token.
        }

        await Task.WhenAll(_agents.Values.Select(agent => agent.Run));
    }

    private void Start(BoxEndpoint box, CancellationToken stoppingToken)
    {
        var stopping = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var agent = ActivatorUtilities.CreateInstance<BoxAgent>(services, box);
        _agents[box.BoxId] = new RunningAgent(stopping, Task.Run(() => agent.RunAsync(stopping.Token), CancellationToken.None));
        LogAgentStarted(box.BoxCode, box.Host, box.Port);
    }

    private async Task<IReadOnlyList<BoxEndpoint>> LoadBoxesAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                return await scope.ServiceProvider.GetRequiredService<IBoxStore>().GetBoxEndpointsAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The database may still be starting; the boxes cannot run without it.
                LogLoadFailed(exception);
                await Task.Delay(RetryDelay, time, stoppingToken);
            }
        }
    }

    private sealed record RunningAgent(CancellationTokenSource Stopping, Task Run)
    {
        public async Task StopAsync()
        {
            await Stopping.CancelAsync();
            await Run;
            Stopping.Dispose();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting agents for {Count} box(es)")]
    private partial void LogStarting(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Box {BoxCode}: agent started for BU {Host}:{Port}")]
    private partial void LogAgentStarted(string boxCode, string host, int port);

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not load the boxes; retrying")]
    private partial void LogLoadFailed(Exception exception);
}
