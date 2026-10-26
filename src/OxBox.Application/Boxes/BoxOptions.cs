using System.ComponentModel.DataAnnotations;

namespace OxBox.Application.Boxes;

/// <summary>How the server drives box hardware, whichever workflow is running.</summary>
public sealed class BoxOptions
{
    public const string SectionName = "Box";

    /// <summary>How often the box's agent checks whether a service visit has timed out.</summary>
    [Range(typeof(TimeSpan), "00:00:00.200", "00:01:00")]
    public TimeSpan TickInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long the BU link must stay down before the display says the box is out of service.
    /// The real BU drops its link every 20–30 s for a few seconds; those gaps should not flash
    /// on the display.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00", "00:05:00")]
    public TimeSpan OfflineScreenDelay { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// A scan of the same code within this time of the previous one is ignored. Readers fire
    /// again and again while a code stays in view, and each repeat would replace the result on
    /// the display with "already returned".
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00", "00:01:00")]
    public TimeSpan RepeatScanWindow { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Attempts to open a lock before giving up on it.</summary>
    [Range(1, 5)]
    public int OpenAttempts { get; set; } = 3;
}
