using Microsoft.EntityFrameworkCore;
using Sistemadecontrolparqueo.Data;
using Sistemadecontrolparqueo.Models;

var builder = WebApplication.CreateBuilder(args);

// 🔑 Configurar cadena de conexión (local o Railway)
string connectionString;
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Parsear DATABASE_URL de Railway (mysql://user:pass@host:port/db)
    var uri = new Uri(databaseUrl);
    var user = uri.UserInfo.Split(':')[0];
    var password = Uri.UnescapeDataString(uri.UserInfo.Split(':')[1]);
    var host = uri.Host;
    var port = uri.Port;
    var database = uri.LocalPath.Trim('/');

    connectionString = $"Server={host};Port={port};Database={database};User={user};Password={password};SslMode=Required;";
}
else
{
    // Desarrollo local
    connectionString = builder.Configuration.GetConnectionString("ParqueoDB") 
                       ?? throw new InvalidOperationException("Cadena de conexión no configurada.");
}

// Registrar servicios
builder.Services.AddDbContext<ParqueoContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configurar pipeline HTTP
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

// 🧠 Ejecutar migraciones al iniciar (solo en producción)
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ParqueoContext>();
    context.Database.Migrate(); // ← Crea/actualiza la tabla automáticamente
}

app.Run();