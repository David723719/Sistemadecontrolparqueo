using System.ComponentModel.DataAnnotations;

namespace Sistemadecontrolparqueo.Models
{
    public class Vehiculo
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Placa { get; set; } = string.Empty;

        public DateTime FechaEntrada { get; set; }

        public DateTime? FechaSalida { get; set; }

        public string Estado { get; set; } = "Dentro del Parqueo";

        public int? TiempoParqueado { get; set; }

        public bool RecompensaEntregada { get; set; } = false;
    }
}