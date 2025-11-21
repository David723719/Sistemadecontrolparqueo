using Microsoft.EntityFrameworkCore;
using Sistemadecontrolparqueo.Data;
using Sistemadecontrolparqueo.Models;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// 🔐 Obtener cadena de conexión (Railway o local)
string connectionString = GetConnectionString(builder.Configuration);
LogConnectionString(connectionString);

// Registrar DbContext
builder.Services.AddDbContext<ParqueoContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions =>
        {
            mySqlOptions.CommandTimeout(120);
            mySqlOptions.EnableRetryOnFailure(3);
        }
    ));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Pipeline HTTP
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

// 🌐 Puerto dinámico (Railway lo requiere)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");
Console.WriteLine($"🌐 Escuchando en http://0.0.0.0:{port}");

// 🔒 APLICAR MIGRACIONES ANTES DE INICIAR (solo en producción)
// Esto evita que el health check falle por tablas inexistentes
if (app.Environment.IsProduction())
{
    Console.WriteLine("⏳ [Producción] Aplicando migraciones antes de iniciar...");
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ParqueoContext>();

        Console.WriteLine("📡 Probando conexión a la base de datos...");
        await context.Database.OpenConnectionAsync();
        await context.Database.CloseConnectionAsync();
        Console.WriteLine("✅ Conexión exitosa.");

        Console.WriteLine("🚀 Aplicando migraciones...");
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await context.Database.MigrateAsync(cts.Token);
        Console.WriteLine("✅ Migraciones aplicadas correctamente.");
    }
    catch (Exception ex)
    {
        var errorMsg = ex switch
        {
            MySqlException mySqlEx => $"MySQL [{mySqlEx.Number}]: {mySqlEx.Message}",
            OperationCanceledException => "Timeout: migraciones tardaron más de 2 minutos",
            _ => ex.Message
        };
        Console.WriteLine($"❌ ERROR CRÍTICO: No se pudieron aplicar migraciones.\n{errorMsg}");
        Console.WriteLine("💀 La aplicación no puede iniciar. Despliegue fallido.");
        throw; // Esto hace que el contenedor se caiga → Railway lo marca como fallido (mejor que 500 silencioso)
    }
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

            return $"Server={host};Port={port};Database={db};User={user};Password={pass};SslMode=Required;Connection Timeout=30;";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"❌ No se pudo procesar MYSQL_URL: {ex.Message}", ex);
        }
    }

    // Fallback: desarrollo local
    var localConn = config.GetConnectionString("ParqueoDB");
    if (string.IsNullOrWhiteSpace(localConn))
        throw new InvalidOperationException("❌ No hay cadena de conexión (ni MYSQL_URL ni ParqueoDB)");

    Console.WriteLine("🔧 Modo desarrollo: usando appsettings.json");
    return localConn;
}

static void LogConnectionString(string conn)
{
    try
    {
        var server = ExtractValue(conn, "Server");
        var db = ExtractValue(conn, "Database");
        var user = ExtractValue(conn, "User");
        Console.WriteLine($"🔍 Conexión: Server={server}, Database={db}, User={user}");
    }
    catch { }
}

static string ExtractValue(string conn, string key)
{
    var match = System.Text.RegularExpressions.Regex.Match(conn, $@"{key}=([^;]+)");
    return match.Success ? match.Groups[1].Value : "??";
}