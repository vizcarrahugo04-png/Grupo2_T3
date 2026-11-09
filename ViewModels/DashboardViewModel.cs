using LabTrack.Models;

namespace LabTrack.ViewModels;

public sealed class DashboardViewModel
{
    public string FullName { get; init; } = string.Empty;
    public bool IsPenalized { get; init; }
    public int AvailableEquipment { get; init; }
    public int PendingCount { get; init; }
    public int ApprovedCount { get; init; }
    public List<Reservation> RecentReservations { get; init; } = [];
}
