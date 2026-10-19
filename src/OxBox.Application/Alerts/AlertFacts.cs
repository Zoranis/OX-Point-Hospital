using OxBox.Domain.Alerts;
using OxBox.Domain.CostCentres;

namespace OxBox.Application.Alerts;

/// <summary>What a stock alert is about. The same facts always give the same <see cref="Key"/>.</summary>
public sealed record AlertFacts(AlertKind Kind, string BoxCode, CostCentre CostCentre, int Count)
{
    /// <summary>Identifies the condition, so it is raised only once while active: per box and cost centre.</summary>
    public string Key => $"{Kind}:{BoxCode}:{CostCentre.Code}";
}
