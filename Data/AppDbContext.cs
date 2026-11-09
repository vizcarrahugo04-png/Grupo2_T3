using LabTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.StudentCode).IsUnique();
            entity.Property(x => x.StudentCode).HasMaxLength(24).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.InventoryCode).IsUnique();
            entity.Property(x => x.InventoryCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(24).IsRequired();
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.RejectionReason).HasMaxLength(300);
            entity.HasIndex(x => new { x.EquipmentId, x.StartsAtUtc, x.EndsAtUtc });
            entity.HasIndex(x => new { x.StudentId, x.StartsAtUtc, x.EndsAtUtc });
            entity.HasOne(x => x.Student).WithMany(x => x.Reservations)
                .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Equipment).WithMany(x => x.Reservations)
                .HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
