using OxBox.Application.Feedback;

namespace OxBox.Web.Kiosk;

/// <summary>A slot number shown large, with what to do there ("Empty in", "Full out").</summary>
public sealed record KioskSlot(string Label, int Number);

/// <summary>
/// What the kiosk page renders for a screen: the wording, a tone (color), and the slots shown
/// large when the person at the box has to go to particular slots. <c>Note</c> is a small line
/// above the title (which technician is servicing the box).
/// </summary>
public sealed record KioskView(KioskTone Tone, string Title, string Body, IReadOnlyList<KioskSlot> Slots, string? Note = null)
{
    public static KioskView For(KioskScreen screen, KioskLanguage language)
    {
        string Text(string key) => KioskTexts.Get(key, language);
        string Format(string key, params object[] arguments) => KioskTexts.Format(key, language, arguments);

        return screen switch
        {
            ReadyScreen => new(KioskTone.Neutral, Text("Ready.Title"), Text("Ready.Body"), []),
            OutOfServiceScreen => new(KioskTone.Offline, Text("OutOfService.Title"), Text("OutOfService.Body"), []),
            ExchangeOpenScreen open => new(
                KioskTone.Success,
                Text("Exchange.Title"),
                Text("Exchange.Body"),
                [new(Text("Exchange.ReturnSlot"), open.ReturnPositionNumber), new(Text("Exchange.FullSlot"), open.DispensePositionNumber)]),
            RejectedScreen rejected => new(
                KioskTone.Error,
                Text($"Error.{rejected.Error}.Title"),
                Text($"Error.{rejected.Error}.Body"),
                rejected.ReturnPositionNumber is { } slot ? [new(Text("Exchange.ReturnSlot"), slot)] : []),
            ServiceScreen service => new(
                KioskTone.Service,
                Text("Service.Title"),
                $"{(service.EmptySlots.Count > 0 ? Format("Service.Empties", string.Join(", ", service.EmptySlots)) : Text("Service.NoEmpties"))} {Format("Service.Body", service.RestockedCount)}",
                service.LastInsertedPositionNumber is { } inserted ? [new(Text("Service.InsertSlot"), inserted)] : [],
                Note: Format("Service.Technician", service.TechnicianName)),
            ServiceRejectedScreen rejected => new(
                KioskTone.Error, Text($"ServiceError.{rejected.Error}.Title"), Text($"ServiceError.{rejected.Error}.Body"), []),
            ServiceFinishedScreen finished => new(
                KioskTone.Success, Text("Service.Finished.Title"), Format("Service.Finished.Body", finished.RemovedCount, finished.RestockedCount), []),
            _ => throw new ArgumentOutOfRangeException(nameof(screen), screen, "Unknown kiosk screen."),
        };
    }

    // Records compare lists by reference; the page redraws only when what it shows has changed.
    public bool Equals(KioskView? other) =>
        other is not null
        && Tone == other.Tone
        && Title == other.Title
        && Body == other.Body
        && Note == other.Note
        && Slots.SequenceEqual(other.Slots);

    public override int GetHashCode() => HashCode.Combine(Tone, Title, Body, Note, Slots.Count);
}
