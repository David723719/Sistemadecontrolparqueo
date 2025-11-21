using Microsoft.EntityFrameworkCore;
using Sistemadecontrolparqueo.Models;

namespace Sistemadecontrolparqueo.Data
{
    public class ParqueoContext : DbContext
    {
        public ParqueoContext(DbContextOptions<ParqueoContext> options) : base(options) { }

        public DbSet<Vehiculo> Vehiculos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar la tabla Vehiculos
            modelBuilder.Entity<Vehiculo>(entity =>
            {
                entity.ToTable("Vehiculos");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Placa).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Estado).HasMaxLength(50);
                entity.Property(e => e.FechaEntrada).IsRequired();
            });
        }
    }
}