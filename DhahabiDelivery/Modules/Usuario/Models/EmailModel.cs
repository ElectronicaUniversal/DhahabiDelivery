using System.ComponentModel.DataAnnotations;

namespace DhahabiDelivery.Modules.Usuario.Models;

public class EmailModel
{
    [Required]
    [MinLength(6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}