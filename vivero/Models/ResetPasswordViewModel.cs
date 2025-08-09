using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace vivero.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
