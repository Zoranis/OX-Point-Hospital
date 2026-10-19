using System.ComponentModel.DataAnnotations;

namespace OxBox.Application.Boxes;

/// <summary>How the server drives box hardware, whichever workflow is running.</summary>
public sealed class BoxOptions
{
    public const string SectionName = "Box";

    /// <summary>How often the box's agent checks whether a service visit has timed out.</summary>
    [Range(typeof(TimeSpan), "00:00:00.200", "00:01:00")]
    public TimeSpan TickInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Attempts to open a lock before giving up on it.</summary>
    [Range(1, 5)]
    public int OpenAttempts { get; set; } = 3;
}
