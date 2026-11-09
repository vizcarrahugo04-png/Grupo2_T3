using System.ComponentModel.DataAnnotations;

namespace LabTrack.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu código."), Display(Name = "Código de estudiante")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña."), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
