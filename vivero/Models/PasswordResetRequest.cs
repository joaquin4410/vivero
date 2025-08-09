using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace vivero.Models
{
    public class PasswordResetRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        public bool IsUsed { get; set; } = false;
    }

}
