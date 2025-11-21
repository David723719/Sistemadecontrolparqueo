using Microsoft.AspNetCore.Mvc;
using Sistemadecontrolparqueo.Data;
using Sistemadecontrolparqueo.Models;
using Microsoft.EntityFrameworkCore;

namespace Sistemadecontrolparqueo.Controllers
{
    public class VehiculoController : Controller
    {
        private readonly ParqueoContext _context;

        public VehiculoController(ParqueoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vehiculos = await _context.Vehiculos.ToListAsync();
            return View(vehiculos);
        }

        public IActionResult Entrada()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Entrada([Bind("Placa")] Vehiculo vehiculo)
        {
            if (ModelState.IsValid)
            {
                vehiculo.FechaEntrada = DateTime.Now;
                vehiculo.Estado = "Dentro del Parqueo";
                _context.Add(vehiculo);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "✅ Vehículo registrado correctamente";
                return RedirectToAction(nameof(Index));
            }
            return View(vehiculo);
        }

        public async Task<IActionResult> Salida(string placa)
        {
            if (string.IsNullOrEmpty(placa))
                return NotFound();

            var vehiculo = await _context.Vehiculos
                .FirstOrDefaultAsync(m => m.Placa == placa && m.Estado == "Dentro del Parqueo");

            if (vehiculo == null)
                return NotFound();

            return View(vehiculo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salida(int id)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(id);
            if (vehiculo == null || vehiculo.Estado != "Dentro del Parqueo")
                return NotFound();

            vehiculo.FechaSalida = DateTime.Now;

            // Calcular tiempo exacto en horas (con decimales)
            var tiempo = (DateTime)vehiculo.FechaSalida - vehiculo.FechaEntrada;
            var horasTotales = tiempo.TotalHours;
            vehiculo.TiempoParqueado = (int)Math.Floor(horasTotales);

            vehiculo.Estado = "Fuera del Parqueo";

            // Recompensa si supera 10 horas (incluso 10.01)
            vehiculo.RecompensaEntregada = horasTotales > 10;

            _context.Update(vehiculo);
            await _context.SaveChangesAsync();

            if (vehiculo.RecompensaEntregada)
            {
                var horas = (int)horasTotales;
                var minutos = (int)((horasTotales - horas) * 60);
                TempData["Recompensa"] = $"🎉 ¡FELICIDADES! El vehículo {vehiculo.Placa} permaneció {horas} horas y {minutos} minutos en el parqueo (más de 10h). Recibe como regalo: LIMPIAVIDRIOS + VASELINA 🎁";
            }
            else
            {
                TempData["Mensaje"] = "✅ Salida registrada correctamente";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Historial()
        {
            var historial = await _context.Vehiculos
                .OrderByDescending(v => v.FechaEntrada)
                .ToListAsync();
            return View(historial);
        }
    }
}