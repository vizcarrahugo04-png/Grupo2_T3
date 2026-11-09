using System.Security.Claims;
using LabTrack.Data;
using LabTrack.Models;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Controllers;

[Authorize]
public sealed class HomeController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole(UserRoles.Admin))
            return RedirectToAction("Index", "Admin");

        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Challenge();

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Challenge();

        var now = DateTime.UtcNow;
        var model = new DashboardViewModel
        {
            FullName = user.FullName,
            IsPenalized = user.IsPenalized,
            AvailableEquipment = await db.Equipments.CountAsync(e => !e.Reservations.Any(r =>
                r.Status == ReservationStatus.Approved && r.ReturnedAtUtc == null && r.StartsAtUtc <= now)),
            PendingCount = await db.Reservations.CountAsync(r =>
                r.StudentId == userId && r.Status == ReservationStatus.Pending),
            ApprovedCount = await db.Reservations.CountAsync(r =>
                r.StudentId == userId && r.Status == ReservationStatus.Approved && r.ReturnedAtUtc == null),
            RecentReservations = await db.Reservations.AsNoTracking()
                .Include(r => r.Equipment)
                .Where(r => r.StudentId == userId)
                .OrderByDescending(r => r.RequestedAtUtc).Take(5).ToListAsync()
        };
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
