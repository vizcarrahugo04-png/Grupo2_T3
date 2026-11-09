using System.Data;
using LabTrack.Data;
using LabTrack.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LabTrack.Services;

/// <summary>
/// Mantiene todas las reglas críticas en el servidor. Las operaciones de reserva
/// usan SERIALIZABLE para evitar dobles asignaciones bajo solicitudes simultáneas.
/// </summary>
public sealed class ReservationService(AppDbContext db)
{
    public async Task<OperationResult> RequestAsync(Guid studentId, Guid equipmentId,
        DateTime startsLocal, DateTime endsLocal)
    {
        DateTime startsAtUtc;
        DateTime endsAtUtc;
        try
        {
            startsAtUtc = PeruClock.ToUtc(startsLocal);
            endsAtUtc = PeruClock.ToUtc(endsLocal);
        }
        catch (ArgumentException)
        {
            return OperationResult.Fail("La fecha u hora indicada no es válida.");
        }

        if (startsAtUtc < DateTime.UtcNow)
            return OperationResult.Fail("El horario debe comenzar en el futuro.");
        if (endsAtUtc <= startsAtUtc)
            return OperationResult.Fail("La hora de fin debe ser posterior a la hora de inicio.");

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var student = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == studentId);
            if (student is null || student.Role != UserRoles.Student)
                return OperationResult.Fail("El estudiante no existe.");
            if (student.IsPenalized)
                return OperationResult.Fail("Tu cuenta está penalizada y no puede solicitar reservas.");

            var equipment = await db.Equipments.AsNoTracking().SingleOrDefaultAsync(e => e.Id == equipmentId);
            if (equipment is null)
                return OperationResult.Fail("Selecciona un equipo válido.");

            // Prestado significa que aún no ha sido devuelto físicamente.
            var borrowedNow = await db.Reservations.AnyAsync(r =>
                r.EquipmentId == equipmentId && r.Status == ReservationStatus.Approved &&
                r.ReturnedAtUtc == null && r.StartsAtUtc <= DateTime.UtcNow);
            if (borrowedNow)
                return OperationResult.Fail("El equipo está prestado actualmente.");

            var equipmentConflict = await db.Reservations.AnyAsync(r =>
                r.EquipmentId == equipmentId && r.Status == ReservationStatus.Approved &&
                r.ReturnedAtUtc == null && r.StartsAtUtc < endsAtUtc && startsAtUtc < r.EndsAtUtc);
            if (equipmentConflict)
                return OperationResult.Fail("Este equipo ya tiene una reserva aprobada en ese horario.");

            // Un estudiante tampoco puede mantener solicitudes pendientes solapadas.
            var studentConflict = await db.Reservations.AnyAsync(r =>
                r.StudentId == studentId && r.Status != ReservationStatus.Rejected &&
                r.ReturnedAtUtc == null && r.StartsAtUtc < endsAtUtc && startsAtUtc < r.EndsAtUtc);
            if (studentConflict)
                return OperationResult.Fail("Ya tienes una reserva o solicitud que coincide con ese horario.");

            db.Reservations.Add(new Reservation
            {
                StudentId = studentId,
                EquipmentId = equipmentId,
                StartsAtUtc = startsAtUtc,
                EndsAtUtc = endsAtUtc
            });

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return OperationResult.Ok("Solicitud registrada. Queda pendiente de aprobación.");
        }
        catch (Exception exception) when (IsConcurrencyFailure(exception))
        {
            return OperationResult.Fail("Otra solicitud se procesó al mismo tiempo. Actualiza e inténtalo nuevamente.");
        }
    }

    public async Task<OperationResult> ApproveAsync(Guid reservationId)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var reservation = await db.Reservations.SingleOrDefaultAsync(r => r.Id == reservationId);
            if (reservation is null || reservation.Status != ReservationStatus.Pending)
                return OperationResult.Fail("La solicitud ya fue gestionada o no existe.");

            var equipmentConflict = await db.Reservations.AnyAsync(r =>
                r.Id != reservation.Id && r.EquipmentId == reservation.EquipmentId &&
                r.Status == ReservationStatus.Approved && r.ReturnedAtUtc == null &&
                r.StartsAtUtc < reservation.EndsAtUtc && reservation.StartsAtUtc < r.EndsAtUtc);
            if (equipmentConflict)
                return OperationResult.Fail("No se puede aprobar: existe una reserva aprobada que se cruza con ese horario.");

            var studentConflict = await db.Reservations.AnyAsync(r =>
                r.Id != reservation.Id && r.StudentId == reservation.StudentId &&
                r.Status == ReservationStatus.Approved && r.ReturnedAtUtc == null &&
                r.StartsAtUtc < reservation.EndsAtUtc && reservation.StartsAtUtc < r.EndsAtUtc);
            if (studentConflict)
                return OperationResult.Fail("El estudiante ya tiene otro equipo aprobado durante ese horario.");

            reservation.Status = ReservationStatus.Approved;
            reservation.ApprovedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return OperationResult.Ok("Solicitud aprobada.");
        }
        catch (Exception exception) when (IsConcurrencyFailure(exception))
        {
            return OperationResult.Fail("La solicitud cambió mientras la procesabas. Actualiza la página.");
        }
    }

    public async Task<OperationResult> RejectAsync(Guid reservationId, string? reason)
    {
        reason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            return OperationResult.Fail("Debes escribir un motivo para rechazar la solicitud.");
        if (reason.Length > 300)
            return OperationResult.Fail("El motivo no debe superar 300 caracteres.");

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var reservation = await db.Reservations.SingleOrDefaultAsync(r => r.Id == reservationId);
            if (reservation is null || reservation.Status != ReservationStatus.Pending)
                return OperationResult.Fail("La solicitud ya fue gestionada o no existe.");

            reservation.Status = ReservationStatus.Rejected;
            reservation.RejectionReason = reason;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return OperationResult.Ok("Solicitud rechazada con motivo registrado.");
        }
        catch (Exception exception) when (IsConcurrencyFailure(exception))
        {
            return OperationResult.Fail("La solicitud cambió mientras la procesabas. Actualiza la página.");
        }
    }

    public async Task<OperationResult> ReturnAsync(Guid reservationId)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var reservation = await db.Reservations.Include(r => r.Student)
                .SingleOrDefaultAsync(r => r.Id == reservationId);
            if (reservation is null || reservation.Status != ReservationStatus.Approved ||
                reservation.ReturnedAtUtc is not null)
                return OperationResult.Fail("No hay un préstamo pendiente de devolución con ese código.");

            var now = DateTime.UtcNow;
            if (reservation.StartsAtUtc > now)
                return OperationResult.Fail("Este préstamo todavía no ha comenzado.");

            reservation.ReturnedAtUtc = now;
            if (now > reservation.EndsAtUtc)
            {
                reservation.Student.IsPenalized = true;
                reservation.Student.PenalizedAtUtc = now;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return now > reservation.EndsAtUtc
                ? OperationResult.Ok("Devolución registrada. El estudiante fue penalizado por atraso.")
                : OperationResult.Ok("Devolución registrada. El equipo vuelve a estar disponible.");
        }
        catch (Exception exception) when (IsConcurrencyFailure(exception))
        {
            return OperationResult.Fail("El préstamo cambió mientras lo procesabas. Actualiza la página.");
        }
    }

    private static bool IsConcurrencyFailure(Exception exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is PostgresException pg && pg.SqlState is ("40001" or "40P01" or "23P01"))
                return true;
            current = current.InnerException;
        }
        return false;
    }
}
