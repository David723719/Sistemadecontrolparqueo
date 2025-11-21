using Microsoft.EntityFrameworkCore;
using Sistemadecontrolparqueo.Models;

namespace Sistemadecontrolparqueo.Data
{
    public class ParqueoContext : DbContext
    {
        public ParqueoContext(DbContextOptions<ParqueoContext> options) : base(options) { }

        public DbSet<Vehiculo> Vehiculos { get; set; }
    }
}