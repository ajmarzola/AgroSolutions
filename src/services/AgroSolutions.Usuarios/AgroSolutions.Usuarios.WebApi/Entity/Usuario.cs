using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroSolutions.Usuarios.WebApi.Entity
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Senha { get; set; } = string.Empty;

        [Column("TipoId")]
        public int TipoId { get; set; }

        // O '?' é fundamental para o Swagger não exigir o objeto completo no cadastro
        public virtual TipoUsuario? Tipo { get; set; }
    }
}
