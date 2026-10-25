using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OxBox.Domain.Alerts;
using OxBox.Domain.CostCentres;
using OxBox.Infrastructure.Alerts;
using OxBox.Infrastructure.Persistence;
using OxBox.Integration.Tests.Persistence;

namespace OxBox.Integration.Tests.Alerts;

[Collection(DatabaseTestGroup.Name)]
[Trait("Category", "Integration")]
public sealed class OutboxDispatcherTests(SqliteFixture database) : IAsyncLifetime
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 15, 8, 0, 0, TimeSpan.Zero));
    private readonly FlakySender _sender = new();
    private ServiceProvider _services = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await database.CreateMigratedDatabaseAsync();
        _services = new ServiceCollection()
            .AddDbContext<OxBoxDbContext>(options => options.UseSqlite(connectionString))
            .BuildServiceProvider();

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OxBoxDbContext>();
        var costCentre = new CostCentre("1101", "Interní klinika");
        db.CostCentres.Add(costCentre);
        await db.SaveChangesAsync();
        var alert = new Alert(AlertKind.NoFullCylinder, "NoFullCylinder:BOX-01:1101", "BOX-01", costCentre.Id, "No full cylinder", _time.GetUtcNow());
        db.OutboxMessages.Add(new OutboxMessage(alert, MessageChannel.Email, "udrzba@nemocnice.test", "Subject", "Body", _time.GetUtcNow()));
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync() => await _services.DisposeAsync();

    [Fact]
    public async Task Dispatch_SenderDown_KeepsTheMessageAndRetriesLaterUntilDelivered()
    {
        var dispatcher = new OutboxDispatcher(
            _services.GetRequiredService<IServiceScopeFactory>(), [_sender], _time, NullLogger<OutboxDispatcher>.Instance);

        _sender.IsDown = true;
        (await dispatcher.DispatchDueAsync(CancellationToken.None)).Should().Be(1);
        (await MessageAsync()).Should().Match<OutboxMessage>(m => !m.IsSent && m.Attempts == 1 && m.LastError == "Connection refused");

        // Not due again until the retry delay has passed.
        _sender.IsDown = false;
        (await dispatcher.DispatchDueAsync(CancellationToken.None)).Should().Be(0);

        _time.Advance(TimeSpan.FromSeconds(31));
        (await dispatcher.DispatchDueAsync(CancellationToken.None)).Should().Be(1);
        (await MessageAsync()).IsSent.Should().BeTrue();
        _sender.Delivered.Should().Be(1);

        (await dispatcher.DispatchDueAsync(CancellationToken.None)).Should().Be(0);
    }

    private async Task<OutboxMessage> MessageAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OxBoxDbContext>().OutboxMessages.AsNoTracking().SingleAsync();
    }

    private sealed class FlakySender : IMessageSender
    {
        public bool IsDown { get; set; }

        public int Delivered { get; private set; }

        public MessageChannel Channel => MessageChannel.Email;

        public Task<string?> SendAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            if (IsDown)
            {
                throw new InvalidOperationException("Connection refused");
            }

            Delivered++;
            return Task.FromResult<string?>(null);
        }
    }
}
