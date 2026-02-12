namespace AgroSolutions.Usuarios.WebApi.DTOs
{
    using System.ComponentModel.DataAnnotations;

    public class UsuarioRegistroDto
    {
        [Required(ErrorMessage = "O e-mail � obrigat�rio.")]
        [EmailAddress(ErrorMessage = "O e-mail deve ser v�lido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha � obrigat�ria.")]
        [MinLength(6, ErrorMessage = "A senha deve ter no m�nimo 6 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        public int TipoId { get; set; }
    }

    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string TipoDescricao { get; set; } = string.Empty;
    }
}
