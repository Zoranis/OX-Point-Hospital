using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OxBox.Application.Abstractions;

namespace OxBox.Infrastructure.OxPoint;

internal sealed class BuConnectionFactory(
    IOptions<BuConnectionOptions> options,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory) : IBoxGatewayFactory
{
    public IBoxGateway Create(string host, int port) =>
        new BuConnection(
            new UriBuilder("ws", host, port).Uri,
            options.Value,
            timeProvider,
            loggerFactory.CreateLogger<BuConnection>());
}
