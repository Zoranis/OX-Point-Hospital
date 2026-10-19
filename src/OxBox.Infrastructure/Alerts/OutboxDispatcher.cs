using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OxBox.Domain.Alerts;
using OxBox.Infrastructure.Persistence;

namespace OxBox.Infrastructure.Alerts;

/// <summary>
/// Sends queued alert messages. Each due message is claimed with <c>FOR UPDATE SKIP LOCKED</c>, so
/// two servers never send the same one; a failure is retried with growing delays (see
/// <see cref="OutboxMessage.AttemptFailed"/>), and the error is kept for the alerts page.
/// </summary>
internal sealed partial class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IEnumerable<IMessageSender> senders,
    TimeProvider time,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;

    private readonly Dictionary<MessageChannel, IMessageSender> _senders = senders.ToDictionary(sender => sender.Channel);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval, time);
        do
        {
            try
            {
                await DispatchDueAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The database may be briefly unavailable; the messages stay queued.
                LogDispatchFailed(exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <returns>How many messages were attempted.</returns>
    internal async Task<int> DispatchDueAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OxBoxDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var now = time.GetUtcNow();
        var due = await db.OutboxMessages
            .FromSql($"""
                SELECT * FROM "OutboxMessages"
                WHERE "SentAtUtc" IS NULL AND "NextAttemptAtUtc" <= {now}
                ORDER BY "Id"
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in due)
        {
            try
            {
                var note = await _senders[message.Channel].SendAsync(message, cancellationToken);
                message.Delivered(time.GetUtcNow(), note);
                LogSent(message.Channel, message.Recipient, message.Subject);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.AttemptFailed(exception.Message, time.GetUtcNow());
                LogSendFailed(message.Channel, message.Recipient, message.Attempts, exception.Message);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return due.Count;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent {Channel} to {Recipient}: {Subject}")]
    private partial void LogSent(MessageChannel channel, string recipient, string subject);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Channel} to {Recipient} failed (attempt {Attempt}); will retry: {Error}")]
    private partial void LogSendFailed(MessageChannel channel, string recipient, int attempt, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Dispatching alert messages failed; retrying")]
    private partial void LogDispatchFailed(Exception exception);
}
