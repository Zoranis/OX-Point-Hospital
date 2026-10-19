namespace OxBox.Domain.Technicians;

/// <summary>A maintenance or supplier technician who restocks boxes by scanning a personal service code.</summary>
public sealed class Technician
{
    private Technician()
    {
        Name = null!;
        ServiceBarcode = null!;
    }

    public Technician(string name, string serviceBarcode)
    {
        Name = Guard.NotBlank(name, nameof(name));
        ServiceBarcode = Guard.NotBlank(serviceBarcode, nameof(serviceBarcode));
        IsActive = true;
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    /// <summary>Scanning this code at a box enters or leaves service mode, and identifies the technician in the log.</summary>
    public string ServiceBarcode { get; private set; }

    public bool IsActive { get; private set; }

    public void Rename(string name) => Name = Guard.NotBlank(name, nameof(name));

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
