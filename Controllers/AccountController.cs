using System.Security.Claims;
using LabTrack.Data;
using LabTrack.Models;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Controllers;

public sealed class AccountController(AppDbContext db) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var code = model.Code.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.StudentCode == code);

        // Mensaje uniforme si el código no existe o la contraseña es incorrecta.
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Credenciales inválidas");
            return View(model);
        }

        var now = DateTime.UtcNow;
        if (user.LockedUntilUtc > now)
        {
            ModelState.AddModelError(string.Empty,
                "Alerta de seguridad: cuenta bloqueada temporalmente por 3 intentos fallidos. Inténtalo más tarde.");
            return View(model);
        }

        var hasher = new PasswordHasher<AppUser>();
        var verified = hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verified == PasswordVerificationResult.Failed)
        {
            // Incremento atómico para contar intentos de sesiones concurrentes.
            await db.Users.Where(u => u.Id == user.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.FailedLoginAttempts,
                    u => u.FailedLoginAttempts + 1));
            var attempts = await db.Users.AsNoTracking().Where(u => u.Id == user.Id)
                .Select(u => u.FailedLoginAttempts).SingleAsync();

            ModelState.AddModelError(string.Empty, "Credenciales inválidas");
            if (attempts >= 3)
            {
                await db.Users.Where(u => u.Id == user.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(u => u.LockedUntilUtc, now.AddMinutes(15))
                        .SetProperty(u => u.FailedLoginAttempts, 0));
                ModelState.AddModelError(string.Empty,
                    "Alerta de seguridad: cuenta bloqueada durante 15 minutos.");
            }
            return View(model);
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntilUtc = null;
        if (verified == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = hasher.HashPassword(user, model.Password);
        await db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new("StudentCode", user.StudentCode)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = false });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Denied() => View();
}
