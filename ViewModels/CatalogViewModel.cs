namespace LabTrack.ViewModels;

public sealed class EquipmentCard
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string InventoryCode { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public bool IsBorrowed { get; init; }
}

public sealed class CatalogViewModel
{
    public string? Category { get; init; }
    public List<EquipmentCard> Equipments { get; init; } = [];
}
