using LabTrack.Models;

namespace LabTrack.ViewModels;

public sealed class AdminDashboardViewModel
{
    public int PendingCount { get; init; }
    public int ActiveLoanCount { get; init; }
    public int PenalizedCount { get; init; }
    public List<Reservation> PendingReservations { get; init; } = [];
    public List<Reservation> ActiveLoans { get; init; } = [];
}
