using LabTrack.Data;
using LabTrack.Models;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Controllers;

[Authorize(Roles = UserRoles.Student)]
public sealed class CatalogController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? categoria)
    {
        if (!string.IsNullOrEmpty(categoria) && !EquipmentCategories.All.Contains(categoria))
            return BadRequest("La categoría seleccionada no existe.");

        var now = DateTime.UtcNow;
        var query = db.Equipments.AsNoTracking();
        if (!string.IsNullOrEmpty(categoria))
            query = query.Where(e => e.Category == categoria);

        var model = new CatalogViewModel
        {
            Category = categoria,
            Equipments = await query.OrderBy(e => e.Name)
                .Select(e => new EquipmentCard
                {
                    Id = e.Id,
                    Name = e.Name,
                    InventoryCode = e.InventoryCode,
                    Category = e.Category,
                    IsBorrowed = e.Reservations.Any(r => r.Status == ReservationStatus.Approved &&
                        r.ReturnedAtUtc == null && r.StartsAtUtc <= now)
                }).ToListAsync()
        };
        return View(model);
    }
}
