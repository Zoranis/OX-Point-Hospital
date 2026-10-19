using System.ComponentModel.DataAnnotations;

namespace OxBox.Infrastructure.OxPoint;

public sealed class BuConnectionOptions
{
    public const string SectionName = "BuConnection";

    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>How long to wait for the reply to a command, such as opening a lock.</summary>
    [Range(typeof(TimeSpan), "00:00:00.100", "00:00:30")]
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(3);

    [Range(typeof(TimeSpan), "00:00:00.100", "00:01:00")]
    public TimeSpan InitialReconnectDelay { get; set; } = TimeSpan.FromSeconds(1);

    [Range(typeof(TimeSpan), "00:00:01", "00:10:00")]
    public TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Interval between WebSocket pings. A silently dropped link (for example a pulled cable)
    /// is otherwise only noticed by the TCP stack after minutes.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00", "00:05:00")]
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long to wait for the pong before treating the link as dead. Set to zero if the BU
    /// firmware turns out not to answer pings; only one-way keep-alives are then sent.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00", "00:05:00")]
    public TimeSpan KeepAliveTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
