namespace LabTrack.Models;

public sealed class Equipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string InventoryCode { get; set; } = string.Empty;
    public string Category { get; set; } = EquipmentCategories.Computing;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}

public static class EquipmentCategories
{
    public const string Computing = "Cómputo";
    public const string Electronics = "Electrónica";
    public const string Networks = "Redes";

    public static readonly string[] All = [Computing, Electronics, Networks];
}
