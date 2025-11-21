using Microsoft.EntityFrameworkCore;
using Sistemadecontrolparqueo.Data;
using Sistemadecontrolparqueo.Models;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// 🔐 Obtener cadena de conexión (Railway o local)
string connectionString = GetConnectionString(builder.Configuration);
LogConnectionString(connectionString);

// Registrar DbContext con configuración robusta para MySQL
builder.Services.AddDbContext<ParqueoContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions =>
        {
            mySqlOptions.CommandTimeout(120); // 2 minutos para migraciones grandes
            mySqlOptions.EnableRetryOnFailure(3); // Reintentos automáticos en errores transitorios
        }
    ));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Pipeline de solicitudes
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Vehiculo}/{action=Index}/{id?}");

// 🌐 Configurar puerto dinámico (obligatorio en Railway)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");
Console.WriteLine($"🌐 Escuchando en http://0.0.0.0:{port}");

// 🔄 Aplicar migraciones con reintentos (solo en producción)
if (app.Environment.IsProduction())
{
    _ = Task.Run(async () =>
    {
        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Espera progresiva: 3s → 6s → 9s
                var delay = TimeSpan.FromSeconds(3 * attempt);
                Console.WriteLine($"⏳ Intento {attempt}/{maxRetries}: Esperando {delay.TotalSeconds}s para estabilidad de DB...");
                await Task.Delay(delay);

                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ParqueoContext>();

                // ✅ Prueba real de conexión (no solo ping)
                Console.WriteLine("📡 Verificando conexión a la base de datos...");
                await context.Database.OpenConnectionAsync();
                await context.Database.CloseConnectionAsync();
                Console.WriteLine("✅ Conexión exitosa.");

                // 🚀 Aplicar migraciones
                Console.WriteLine("🔄 Aplicando migraciones...");
                await context.Database.MigrateAsync();
                Console.WriteLine("✅ Migraciones aplicadas correctamente.");

                // 💡 Opcional: Sembrar datos iniciales si es la primera vez
                // await SeedInitialData(context);

                return;
            }
            catch (Exception ex)
            {
                string errorMsg = ex switch
                {
                    MySqlException mySqlEx => $"MySQL [{mySqlEx.Number}]: {mySqlEx.Message}",
                    InvalidOperationException => "Configuración inválida o DB no disponible",
                    _ => ex.Message
                };

                Console.WriteLine($"❌ Falló intento {attempt}: {errorMsg}");

                if (attempt == maxRetries)
                {
                    Console.WriteLine("💀 Error crítico: No se pudieron aplicar migraciones. La aplicación no puede continuar.");
                    Environment.Exit(1); // Falla el contenedor (Railway lo reiniciará o marcará como fallido)
                }
            }
        }
    });
}

app.Run();

// ───────────────────────────────────────────────────────
// 🔐 Métodos auxiliares
// ───────────────────────────────────────────────────────

static string GetConnectionString(IConfiguration config)
{
    var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");
    if (!string.IsNullOrEmpty(mysqlUrl))
    {
        try
        {
            var uri = new Uri(mysqlUrl);
            var userInfo = uri.UserInfo.Split(':');
            var user = userInfo[0];
            var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var host = uri.Host;
            var port = uri.Port;
            var db = uri.LocalPath.Trim('/');

            // ✅ Usamos SslMode=Required (Railway lo exige)
            return $"Server={host};Port={port};Database={db};User={user};Password={pass};SslMode=Required;Connection Timeout=30;Command Timeout=120;";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"❌ No se pudo procesar MYSQL_URL: {ex.Message}", ex);
        }
    }

    // Caer a appsettings.json (solo desarrollo local)
    var fallback = config.GetConnectionString("ParqueoDB");
    if (string.IsNullOrWhiteSpace(fallback))
        throw new InvalidOperationException("❌ No se encontró MYSQL_URL ni ConnectionStrings:ParqueoDB");

    Console.WriteLine("🔧 Modo desarrollo: usando conexión local.");
    return fallback;
}

static void LogConnectionString(string conn)
{
    try
    {
        var parts = System.Text.RegularExpressions.Regex.Matches(conn, @"(\w+)=([^;]+)")
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);

        var server = parts.GetValueOrDefault("Server") ?? "desconocido";
        var db = parts.GetValueOrDefault("Database") ?? "desconocido";
        var user = parts.GetValueOrDefault("User") ?? "desconocido";

        // Ocultar contraseña en logs
        var safeConn = conn;
        if (parts.TryGetValue("Password", out var pass) && !string.IsNullOrEmpty(pass))
        {
            safeConn = safeConn.Replace(pass, "***");
        }

        Console.WriteLine($"🔍 Conexión: Server={server}, Database={db}, User={user}");
        Console.WriteLine($"🔒 Cadena (segura): {safeConn}");
    }
    catch
    {
        Console.WriteLine("⚠️ No se pudo analizar la cadena de conexión.");
    }
}