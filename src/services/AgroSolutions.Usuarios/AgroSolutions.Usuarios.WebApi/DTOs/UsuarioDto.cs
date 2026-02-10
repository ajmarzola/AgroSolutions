namespace AgroSolutions.Usuarios.WebApi.DTOs
{
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string TipoDescricao { get; set; } = string.Empty;
    }
}
