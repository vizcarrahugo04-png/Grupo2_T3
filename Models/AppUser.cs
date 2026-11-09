namespace LabTrack.Models;

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.Student;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public bool IsPenalized { get; set; }
    public DateTime? PenalizedAtUtc { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}

public static class UserRoles
{
    public const string Student = "Estudiante";
    public const string Admin = "Administrador";
}
