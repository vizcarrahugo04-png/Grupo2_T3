using System.Security.Claims;
using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Services;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Controllers;

[Authorize(Roles = UserRoles.Student)]
public sealed class ReservationsController(AppDbContext db, ReservationService reservations) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        if (userId is null) return Challenge();
        var items = await db.Reservations.AsNoTracking().Include(r => r.Equipment)
            .Where(r => r.StudentId == userId)
            .OrderByDescending(r => r.RequestedAtUtc).ToListAsync();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? equipoId)
    {
        var start = PeruClock.ToLocal(DateTime.UtcNow).AddHours(1);
        start = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
        var model = new ReservationCreateViewModel
        {
            EquipmentId = equipoId ?? Guid.Empty,
            StartsLocal = start,
            EndsLocal = start.AddHours(1)
        };
        await FillEquipmentAsync(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationCreateViewModel model)
    {
        var userId = GetUserId();
        if (userId is null) return Challenge();

        if (ModelState.IsValid)
        {
            var result = await reservations.RequestAsync(userId.Value, model.EquipmentId,
                model.StartsLocal, model.EndsLocal);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Message);
        }

        await FillEquipmentAsync(model);
        return View(model);
    }

    private Guid? GetUserId() => Guid.TryParse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    private async Task FillEquipmentAsync(ReservationCreateViewModel model)
    {
        var now = DateTime.UtcNow;
        model.Equipments = await db.Equipments.AsNoTracking()
            .Where(e => !e.Reservations.Any(r => r.Status == ReservationStatus.Approved &&
                r.ReturnedAtUtc == null && r.StartsAtUtc <= now))
            .OrderBy(e => e.Category).ThenBy(e => e.Name)
            .Select(e => new EquipmentOption
            {
                Id = e.Id,
                Label = e.InventoryCode + " · " + e.Name
            }).ToListAsync();
    }
}
