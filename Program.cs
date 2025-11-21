using Microsoft.EntityFrameworkCore;
using Sistemadecontrolparqueo.Data;
using Sistemadecontrolparqueo.Models;

var builder = WebApplication.CreateBuilder(args);

// 🔑 Obtener cadena de conexión (Railway inyecta MYSQL_URL)
string connectionString;
var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");

if (!string.IsNullOrEmpty(mysqlUrl))
{
    var uri = new Uri(mysqlUrl);
    var user = uri.UserInfo.Split(':')[0];
    var password = Uri.UnescapeDataString(uri.UserInfo.Split(':')[1]);
    var host = uri.Host;
    var port = uri.Port;
    var database = uri.LocalPath.Trim('/');

    connectionString = $"Server={host};Port={port};Database={database};User={user};Password={password};SslMode=Required;Connection Timeout=30;";
}
else
{
    // Desarrollo local
    connectionString = builder.Configuration.GetConnectionString("ParqueoDB")
                       ?? throw new InvalidOperationException("No se encontró cadena de conexión.");
}

// Registrar DbContext
builder.Services.AddDbContext<ParqueoContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllersWithViews();

var app = builder.Build();

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

// ✅ Puerto dinámico para Railway/Heroku
var port = Environment.GetEnvironmentVariable("PORT") ?? "80";
app.Urls.Add($"http://0.0.0.0:{port}");

// ✅ Migraciones en segundo plano (no bloquean el healthcheck)
if (app.Environment.IsProduction())
{
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(2000); // Espera 2s para que la app esté online
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ParqueoContext>();
            await context.Database.MigrateAsync();
            Console.WriteLine("✅ Migraciones aplicadas.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en migraciones: {ex.Message}");
        }
    });
}

app.Run();