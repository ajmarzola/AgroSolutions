using System.ComponentModel.DataAnnotations;

namespace AgroSolutions.Usuarios.WebApi.Entity
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public int TipoUsuarioId { get; set; }
        public TipoUsuario? Tipo { get; set; }
    }
}
