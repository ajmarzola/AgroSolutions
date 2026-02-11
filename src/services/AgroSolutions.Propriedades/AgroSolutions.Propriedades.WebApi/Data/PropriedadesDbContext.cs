using Microsoft.EntityFrameworkCore;
using AgroSolutions.Propriedades.WebApi.Entities;

namespace AgroSolutions.Propriedades.WebApi.Data;

public class PropriedadesDbContext : DbContext
{
    public PropriedadesDbContext(DbContextOptions<PropriedadesDbContext> options) : base(options)
    {
    }

    public DbSet<Propriedade> Propriedades { get; set; }
    public DbSet<Talhao> Talhoes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Propriedade>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired();
            entity.Property(e => e.Localizacao).IsRequired();
        });

        modelBuilder.Entity<Talhao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired();
            entity.Property(e => e.Cultura).IsRequired();
            entity.Property(e => e.Area).HasPrecision(18, 2);
            entity.HasOne(d => d.Propriedade)
                  .WithMany(p => p.Talhoes)
                  .HasForeignKey(d => d.PropriedadeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
