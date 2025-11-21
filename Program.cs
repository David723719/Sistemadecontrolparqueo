using Microsoft.EntityFrameworkCore;
using Sistemadecontrolparqueo.Data;
using Sistemadecontrolparqueo.Models;

var builder = WebApplication.CreateBuilder(args);

// 🔑 Obtener y parsear MYSQL_URL (Railway) o usar appsettings (local)
string connectionString = GetMySqlConnection(builder.Configuration);

// Registrar DbContext
builder.Services.AddDbContext<ParqueoContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Parse("8.0.32-mysql"))); // versión fija para evitar advertencias

// Servicios MVC
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

// 🧠 Aplicar migraciones automáticamente al iniciar (solo en producción)
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ParqueoContext>();
    context.Database.Migrate();
}

app.Run();

// 🔐 Método auxiliar: obtiene conexión para MySQL (Railway o local)
static string GetMySqlConnection(IConfiguration configuration)
{
    var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");
    
    if (!string.IsNullOrEmpty(mysqlUrl))
    {
        // Parsear URL de Railway/MySQL
        var uri = new Uri(mysqlUrl);
        var user = uri.UserInfo.Split(':')[0];
        var password = Uri.UnescapeDataString(uri.UserInfo.Split(':')[1]);
        var host = uri.Host;
        var port = uri.Port;
        var database = uri.LocalPath.Trim('/');

        // SSL obligatorio en Railway
        return $"Server={host};Port={port};Database={database};User={user};Password={password};SslMode=Required;Connection Timeout=30;";
    }

    // Desarrollo local
    var localConn = configuration.GetConnectionString("ParqueoDB");
    if (string.IsNullOrEmpty(localConn))
        throw new InvalidOperationException("No se encontró cadena de conexión (ni MYSQL_URL ni ParqueoDB).");

    return localConn;
}