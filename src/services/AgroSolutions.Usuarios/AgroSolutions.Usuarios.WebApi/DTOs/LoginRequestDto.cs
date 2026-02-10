using System.ComponentModel.DataAnnotations;

namespace AgroSolutions.Usuarios.WebApi.DTOs
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email deve ser válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        public string Password { get; set; } = string.Empty;
    }
}
