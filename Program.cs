using Microsoft.EntityFrameworkCore;
using Sistemadecontrolparqueo.Data;
using Sistemadecontrolparqueo.Models;

var builder = WebApplication.CreateBuilder(args);

// 🔑 Configurar cadena de conexión (Railway/Heroku: MYSQL_URL; Local: appsettings)
string connectionString = GetMySqlConnection(builder.Configuration);

builder.Services.AddDbContext<ParqueoContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Parse("8.0.32-mysql")));

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

// 🧠 Aplicar migraciones en producción
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ParqueoContext>();
    context.Database.Migrate();
}

// ✅ Soporte para Heroku: puerto dinámico (y Railway)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();

// 🔐 Método auxiliar: obtiene conexión para MySQL
static string GetMySqlConnection(IConfiguration configuration)
{
    // Heroku: usa CLEARDB_DATABASE_URL (si usas ClearDB)
    var dbUrl = Environment.GetEnvironmentVariable("CLEARDB_DATABASE_URL")
                ?? Environment.GetEnvironmentVariable("MYSQL_URL");

    if (!string.IsNullOrEmpty(dbUrl))
    {
        var uri = new Uri(dbUrl);
        var user = uri.UserInfo.Split(':')[0];
        var password = Uri.UnescapeDataString(uri.UserInfo.Split(':')[1]);
        var host = uri.Host;
        var port = uri.Port;
        var database = uri.LocalPath.Trim('/');

        return $"Server={host};Port={port};Database={database};User={user};Password={password};SslMode=Required;Connection Timeout=30;";
    }

    return configuration.GetConnectionString("ParqueoDB") 
           ?? throw new InvalidOperationException("No se encontró cadena de conexión.");
}