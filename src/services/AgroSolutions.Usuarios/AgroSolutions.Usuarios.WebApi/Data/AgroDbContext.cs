using AgroSolutions.Usuarios.WebApi.Entity;
using Microsoft.EntityFrameworkCore;

namespace AgroSolutions.Usuarios.WebApi.Data
{
    public class AgroDbContext : DbContext
    {
        public AgroDbContext(DbContextOptions<AgroDbContext> options) : base(options) { }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<TipoUsuario> TiposUsuarios { get; set; }
    }
}
