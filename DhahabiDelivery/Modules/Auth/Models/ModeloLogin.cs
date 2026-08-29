using System.ComponentModel.DataAnnotations;
using DhahabiDelivery.Modules.Auth.Resources;

namespace DhahabiDelivery.Modules.Auth.Models;

/// <summary>
///     Modelo usado en el módulo de autentificación para las operaciones
///     de inicio de sesión.
/// </summary>
public class ModeloLogin
{
    /// <summary>
    ///     Correo electrónico del usuario.
    /// </summary>
    [Display(ResourceType = typeof(ResourceModelNames), Name = nameof(ResourceModelNames.Email))]
    [Required(ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "RequiredError")]
    [EmailAddress(ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "InvalidEmail")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    ///     Contraseña del usuario, debe tener al menos 6 caracteres.
    /// </summary>
    [Display(ResourceType = typeof(ResourceModelNames), Name = nameof(ResourceModelNames.Password))]
    [Required(ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "RequiredError")]
    [MinLength(6, ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "MinLengthError")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}