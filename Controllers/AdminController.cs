using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Services;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public sealed class AdminController(AppDbContext db, ReservationService reservations) : Controller
{
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var pending = await db.Reservations.AsNoTracking()
            .Include(r => r.Student).Include(r => r.Equipment)
            .Where(r => r.Status == ReservationStatus.Pending)
            .OrderBy(r => r.StartsAtUtc).ToListAsync();

        var loans = await db.Reservations.AsNoTracking()
            .Include(r => r.Student).Include(r => r.Equipment)
            .Where(r => r.Status == ReservationStatus.Approved &&
                r.ReturnedAtUtc == null && r.StartsAtUtc <= now)
            .OrderBy(r => r.EndsAtUtc).ToListAsync();

        return View(new AdminDashboardViewModel
        {
            PendingCount = pending.Count,
            ActiveLoanCount = loans.Count,
            PenalizedCount = await db.Users.CountAsync(u => u.IsPenalized),
            PendingReservations = pending,
            ActiveLoans = loans
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        Store(await reservations.ApproveAsync(id));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string? reason)
    {
        Store(await reservations.RejectAsync(id, reason));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(Guid id)
    {
        Store(await reservations.ReturnAsync(id));
        return RedirectToAction(nameof(Index));
    }

    private void Store(OperationResult result) =>
        TempData[result.Success ? "Success" : "Error"] = result.Message;
}
