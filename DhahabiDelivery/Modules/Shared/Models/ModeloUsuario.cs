namespace DhahabiDelivery.Modules.Shared.Models;

public record ModeloUsuario(
    string? NickName,
    string? Name,
    string? LastName,
    bool? EsMasculino,
    string? FechaNacimiento,
    string? Email,
    string? Password
);