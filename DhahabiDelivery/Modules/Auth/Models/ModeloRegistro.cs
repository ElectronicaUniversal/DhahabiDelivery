using System.ComponentModel.DataAnnotations;
using DhahabiDelivery.Modules.Auth.Resources;
using Mensajeria;

namespace DhahabiDelivery.Modules.Auth.Models;

/// <summary>
///     Modelo usado por el módulo de autentificación para las operaciones
///     de registro de usuarios en la base de datos.
/// </summary>
public class ModeloRegistro
{
    /// <summary>
    ///     El apodo del usuario.
    /// </summary>
    [Display(ResourceType = typeof(ResourceModelNames), Name = nameof(ResourceModelNames.NickName))]
    [Required(ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "RequiredError")]
    public string NickName { get; set; } = string.Empty;

    /// <summary>
    ///     El nombre del usuario.
    /// </summary>
    [Display(ResourceType = typeof(ResourceModelNames), Name = nameof(ResourceModelNames.Name))]
    [Required(ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "RequiredError")]
    [DataType(DataType.Text)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     El apellido del usuario.
    /// </summary>
    [Display(ResourceType = typeof(ResourceModelNames), Name = nameof(ResourceModelNames.LastName))]
    [Required(ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "RequiredError")]
    [DataType(DataType.Text)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    ///     Indica si el usuario se identifica como masculino. Por defecto, se inicializa en true.
    /// </summary>
    [Display(ResourceType = typeof(ResourceModelNames), Name = nameof(ResourceModelNames.Sex))]
    [Required(ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "RequiredError")]
    public bool EsMasculino { get; set; } = true;

    /// <summary>
    ///     La fecha de nacimiento del usuario. Debe ser mayor de 18 años.
    /// </summary>
    [Display(ResourceType = typeof(ResourceModelNames), Name = nameof(ResourceModelNames.BornDate))]
    [DataType(DataType.Date)]
    [Required(ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "RequiredError")]
    [MinAge(18, ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "MinAgeError")]
    public DateTime? FechaNacimiento { get; set; }

    /// <summary>
    ///     La dirección de correo electrónico del usuario. Debe ser un formato válido de email.
    /// </summary>
    [Display(ResourceType = typeof(ResourceModelNames), Name = nameof(ResourceModelNames.Email))]
    [EmailAddress(ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "InvalidEmail")]
    [Required(ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "RequiredError")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    ///     La contraseña del usuario. Debe tener al menos 6 caracteres.
    /// </summary>
    [Display(ResourceType = typeof(ResourceModelNames), Name = nameof(ResourceModelNames.Password))]
    [DataType(DataType.Password)]
    [Required(ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "RequiredError")]
    [MinLength(6, ErrorMessageResourceType = typeof(ResourceErrors), ErrorMessageResourceName = "MinLengthError")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     Este método transforma el modelo de registro en un objeto de solicitud
    ///     <see cref="RegistrarClienteRequest" />, que puede ser utilizado para realizar
    ///     la operación de registro.
    /// </summary>
    /// <returns></returns>
    public RegistrarClienteRequest ToRequest()
    {
        var now = DateTime.UtcNow;
        return new RegistrarClienteRequest(
            NickName,
            Email,
            Password,
            Name,
            LastName,
            EsMasculino,
            FechaNacimiento ?? now
        );
    }
}

/// <summary>
///     Un atributo personalizado que se utiliza para validar que la fecha de nacimiento
///     ingresada cumple con la restricción de edad mínima.
/// </summary>
/// <param name="minAge"></param>
public class MinAgeAttribute(int minAge) : ValidationAttribute
{
    /// <summary>
    ///     Este método verifica si la fecha de nacimiento es válida en función de la edad mínima especificada.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public override bool IsValid(object? value)
    {
        if (value is DateTime date) return date <= DateTime.Today.AddYears(-minAge);
        return false;
    }
}