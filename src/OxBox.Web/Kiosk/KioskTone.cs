namespace OxBox.Web.Kiosk;

/// <summary>The color scheme of a kiosk screen.</summary>
public enum KioskTone
{
    Neutral,
    Success,
    Error,
    Offline,

    /// <summary>A technician is servicing the box; users can see at a glance that it is not for them now.</summary>
    Service,
}
