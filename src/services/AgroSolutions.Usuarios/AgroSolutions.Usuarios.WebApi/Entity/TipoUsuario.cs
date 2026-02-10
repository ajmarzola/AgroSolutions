using System.ComponentModel.DataAnnotations;

namespace AgroSolutions.Usuarios.WebApi.Entity
{
    public class TipoUsuario
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
    }
}
