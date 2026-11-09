using System.ComponentModel.DataAnnotations;

namespace LabTrack.ViewModels;

public sealed class EquipmentOption
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
}

public sealed class ReservationCreateViewModel
{
    [Required(ErrorMessage = "Selecciona un equipo.")]
    public Guid EquipmentId { get; set; }

    [Display(Name = "Inicio")]
    public DateTime StartsLocal { get; set; }

    [Display(Name = "Fin")]
    public DateTime EndsLocal { get; set; }

    public List<EquipmentOption> Equipments { get; set; } = [];
}
