using System.ComponentModel.DataAnnotations;

namespace AgroSolutions.Usuarios.WebApi.DTOs
{
    public class RegistroUsuarioDto
    {
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email deve ser válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo de usuário é obrigatório.")]
        public int TipoId { get; set; }
    }
}
