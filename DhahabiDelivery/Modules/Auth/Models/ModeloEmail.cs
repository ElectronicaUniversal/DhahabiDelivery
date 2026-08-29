using System.ComponentModel.DataAnnotations;

namespace DhahabiDelivery.Modules.Auth.Models;

/// <summary>
///     Modelo usado por <see cref="PaginaOlvidarContrasenia" />
///     Este modelo debe llegar desde los parámetros de la url o
///     el navegador puede suplirlo automáticamente.
/// </summary>
public class ModeloEmail
{
    /// <summary>
    ///     Correo electrónico del usuario.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}