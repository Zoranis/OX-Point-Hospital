namespace OxBox.Domain.CostCentres;

/// <summary>A hospital department that owns cylinders and is invoiced for them.</summary>
public sealed class CostCentre
{
    private CostCentre()
    {
        Code = null!;
        Name = null!;
    }

    public CostCentre(string code, string name)
    {
        Code = Guard.NotBlank(code, nameof(code));
        Name = Guard.NotBlank(name, nameof(name));
        IsActive = true;
    }

    public int Id { get; private set; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Alert when this many full cylinders or fewer are left for this cost centre in a box.
    /// <c>null</c> uses the site-wide threshold.
    /// </summary>
    public int? LowStockThreshold { get; private set; }

    public void Rename(string name) => Name = Guard.NotBlank(name, nameof(name));

    public void ChangeLowStockThreshold(int? threshold)
    {
        if (threshold is { } value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
        }

        LowStockThreshold = threshold;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
