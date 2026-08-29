namespace DhahabiDelivery.Modules.Auth.Models;

/// <summary>
///     Clase estática que contiene las constantes usadas para
///     saber la razón de verificación de un email.
/// </summary>
public static class ConfirmarEmailReason
{
    /// <summary>
    ///     Se usa cuando se verifica un email a la hora de registrar.
    ///     Un nuevo usuario
    /// </summary>
    public const string Register = "REGISTRO";

    /// <summary>
    ///     Se usa cuando un usuario solicita un cambio de contraseña.
    /// </summary>
    public const string ChangePassword = "CHANGE_PASSWORD";

    /// <summary>
    ///     Se usa cuando un usuario va a iniciar sesión.
    /// </summary>
    public const string Login = "LOGIN";
}