using LabTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db, bool seedDemo)
    {
        // Demo académico: crea el esquema en la primera ejecución.
        // Al evolucionar el modelo se deben usar migraciones de EF Core.
        await db.Database.EnsureCreatedAsync();
        if (!seedDemo) return;

        if (!await db.Users.AnyAsync())
        {
            var users = new[]
            {
                new AppUser { StudentCode = "N00239363", FullName = "Hugo Cruzado", Role = UserRoles.Student },
                new AppUser { StudentCode = "N00348614", FullName = "Marco Castillo", Role = UserRoles.Student },
                new AppUser { StudentCode = "Admin", FullName = "Encargado de laboratorio", Role = UserRoles.Admin }
            };
            var hasher = new PasswordHasher<AppUser>();
            foreach (var user in users)
            {
                user.PasswordHash = hasher.HashPassword(user, "1234");
            }
            db.Users.AddRange(users);
        }

        if (!await db.Equipments.AnyAsync())
        {
            db.Equipments.AddRange(
                new Equipment { InventoryCode = "COM-001", Name = "Laptop Lenovo ThinkPad", Category = EquipmentCategories.Computing },
                new Equipment { InventoryCode = "COM-002", Name = "Monitor Dell 24 pulgadas", Category = EquipmentCategories.Computing },
                new Equipment { InventoryCode = "ELE-001", Name = "Osciloscopio digital", Category = EquipmentCategories.Electronics },
                new Equipment { InventoryCode = "ELE-002", Name = "Kit Arduino Uno", Category = EquipmentCategories.Electronics },
                new Equipment { InventoryCode = "RED-001", Name = "Switch Cisco 24 puertos", Category = EquipmentCategories.Networks },
                new Equipment { InventoryCode = "RED-002", Name = "Router de laboratorio", Category = EquipmentCategories.Networks });
        }

        await db.SaveChangesAsync();
    }
}
