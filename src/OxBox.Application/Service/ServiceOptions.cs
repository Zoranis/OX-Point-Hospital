using System.ComponentModel.DataAnnotations;

namespace OxBox.Application.Service;

public sealed class ServiceOptions
{
    public const string SectionName = "Service";

    /// <summary>A service visit ends by itself after this long without a scan (RFQ: automatic timeout).</summary>
    [Range(typeof(TimeSpan), "00:01:00", "02:00:00")]
    public TimeSpan InactivityTimeout { get; set; } = TimeSpan.FromMinutes(10);
}
