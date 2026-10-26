using Microsoft.Extensions.Logging.Abstractions;
using OxBox.Application.Abstractions;
using OxBox.Infrastructure.OxPoint;
using OxBox.OxPointProtocol;
using OxBox.OxPointProtocol.Commands;
using OxBox.OxPointProtocol.Messages;

namespace OxBox.Integration.Tests.OxPoint;

public sealed class BuConnectionTests : IAsyncLifetime
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private readonly CancellationTokenSource _stop = new();
    private FakeBu _bu = null!;
    private BuConnection _connection = null!;
    private Task _run = Task.CompletedTask;

    public async Task InitializeAsync()
    {
        _bu = await FakeBu.StartAsync();
        var options = new BuConnectionOptions
        {
            CommandTimeout = TimeSpan.FromMilliseconds(300),
            ReconnectWait = TimeSpan.FromSeconds(1),
            InitialReconnectDelay = TimeSpan.FromMilliseconds(100),
        };
        _connection = new BuConnection(_bu.Uri, options, TimeProvider.System, NullLogger<BuConnection>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _stop.CancelAsync();
        await _run;
        await _connection.DisposeAsync();
        await _bu.DisposeAsync();
        _stop.Dispose();
    }

    [Fact]
    public async Task SendAsync_NeverConnected_ReturnsNotConnectedAfterReconnectWait()
    {
        var result = await _connection.SendAsync(new RequestLockStatus(0), CancellationToken.None);

        result.Outcome.Should().Be(CommandOutcome.NotConnected);
    }

    [Fact]
    public async Task SendAsync_LinkComesUpWhileWaiting_SendsOnceConnected()
    {
        _bu.Respond = _ => """{ "Response" : "OK", "Board" : 0, "Lock" : 3 }""";
        var send = _connection.SendAsync(new OpenLock(new LockAddress(0, 3)), CancellationToken.None);

        await ConnectAsync();
        var result = await send.WaitAsync(WaitLimit);

        result.Outcome.Should().Be(CommandOutcome.Replied);
    }

    [Fact]
    public async Task SendAsync_LinkDropsBeforeReply_SendsAgainAfterReconnecting()
    {
        _bu.Respond = _ => """{ "Response" : "OK", "Board" : 0, "Lock" : 3 }""";
        await ConnectAsync();
        _bu.DropLinkOnNextCommand();

        var result = await _connection.SendAsync(new OpenLock(new LockAddress(0, 3)), CancellationToken.None).WaitAsync(WaitLimit);

        result.Outcome.Should().Be(CommandOutcome.Replied);
        _bu.ReceivedCommands.Should().HaveCount(2);
    }

    [Fact]
    public async Task RunAsync_Connects_PublishesConnectedEvent()
    {
        await ConnectAsync();

        _bu.ReceivedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_OpenLockAnswered_ReturnsMatchingReply()
    {
        _bu.Respond = command => command == """{"Open":[1,14]}"""
            ? """{ "Response" : "OK", "Board" : 1, "Lock" : 14 }"""
            : null;
        await ConnectAsync();

        var result = await _connection.SendAsync(new OpenLock(new LockAddress(1, 14)), CancellationToken.None);

        result.Outcome.Should().Be(CommandOutcome.Replied);
        result.Reply.Should().Be(new LockOpenReply(new LockAddress(1, 14), Succeeded: true, "OK"));
    }

    [Fact]
    public async Task SendAsync_NoReply_TimesOut()
    {
        await ConnectAsync();

        var result = await _connection.SendAsync(new RequestLockStatus(0), CancellationToken.None);

        result.Outcome.Should().Be(CommandOutcome.TimedOut);
    }

    [Fact]
    public async Task SendAsync_ReplyForDifferentLock_IsNotTakenAsAnswer()
    {
        _bu.Respond = _ => """{ "Response" : "OK", "Board" : 0, "Lock" : 2 }""";
        await ConnectAsync();

        var result = await _connection.SendAsync(new OpenLock(new LockAddress(0, 1)), CancellationToken.None);

        result.Outcome.Should().Be(CommandOutcome.TimedOut);
        (await NextEventAsync()).Should().Be(new BuMessageReceived(new LockOpenReply(new LockAddress(0, 2), true, "OK")));
    }

    [Fact]
    public async Task SendAsync_CommandWithoutReply_ReturnsSentWithoutReply()
    {
        await ConnectAsync();

        var result = await _connection.SendAsync(new SendKeypadStatus(GoodRead: true), CancellationToken.None);

        result.Outcome.Should().Be(CommandOutcome.SentWithoutReply);
    }

    [Fact]
    public async Task Events_UnsolicitedScan_IsPublished()
    {
        await ConnectAsync();

        await _bu.PushAsync("""{ "QR" : "CYL-0042" }""");

        (await NextEventAsync()).Should().Be(new BuMessageReceived(new ScanReceived("CYL-0042", ScanSource.QrOrBarcode)));
    }

    [Fact]
    public async Task RunAsync_BuGoesAway_PublishesDisconnected()
    {
        await ConnectAsync();

        await _bu.DisposeAsync();

        (await NextEventAsync()).Should().Be(new ConnectionChanged(IsConnected: false));
    }

    private async Task ConnectAsync()
    {
        _run = _connection.RunAsync(_stop.Token);
        (await NextEventAsync()).Should().Be(new ConnectionChanged(IsConnected: true));
        await _bu.WaitForClientAsync();
    }

    private async Task<GatewayEvent> NextEventAsync() =>
        await _connection.Events.ReadAsync().AsTask().WaitAsync(WaitLimit);
}
