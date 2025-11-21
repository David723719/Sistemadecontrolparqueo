using Microsoft.AspNetCore.Mvc;
using Sistemadecontrolparqueo.Data;
using Sistemadecontrolparqueo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Sistemadecontrolparqueo.Controllers
{
    public class VehiculoController : Controller
    {
        private readonly ParqueoContext _context;
        private readonly ILogger<VehiculoController> _logger;

        public VehiculoController(ParqueoContext context, ILogger<VehiculoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Verificar si la base de datos está disponible
                if (!await _context.Database.CanConnectAsync())
                {
                    // Si no puede conectar, intentar crear la base de datos
                    await _context.Database.EnsureCreatedAsync();
                }
                
                var vehiculos = await _context.Vehiculos.ToListAsync();
                return View(vehiculos);
            }
            catch (Exception ex)
            {
                // Log del error
                _logger.LogError(ex, "Error al acceder a la base de datos en Index");
                
                // Retornar una lista vacía en lugar de lanzar excepción
                // Esto permite que la aplicación funcione aunque la BD tenga problemas
                return View(new List<Vehiculo>());
            }
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
            try
            {
                var historial = await _context.Vehiculos
                    .OrderByDescending(v => v.FechaEntrada)
                    .ToListAsync();
                return View(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al acceder al historial");
                // Retornar lista vacía en lugar de error
                return View(new List<Vehiculo>());
            }
        }
    }
}