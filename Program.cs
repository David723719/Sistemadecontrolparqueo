using Microsoft.EntityFrameworkCore;
using Sistemadecontrolparqueo.Data;
using Sistemadecontrolparqueo.Models;

var builder = WebApplication.CreateBuilder(args);

// 🔑 Conexión para Railway (MYSQL_URL) o local
string connectionString = GetConnectionString(builder.Configuration);

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

// ✅ Puerto dinámico (solo una declaración)
var port = Environment.GetEnvironmentVariable("PORT") ?? "80";
app.Urls.Add($"http://0.0.0.0:{port}");

// ✅ Migraciones en segundo plano (no bloquean healthcheck)
if (app.Environment.IsProduction())
{
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(2000);
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

// 🔐 Método auxiliar limpio
static string GetConnectionString(IConfiguration config)
{
    var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");
    if (!string.IsNullOrEmpty(mysqlUrl))
    {
        var uri = new Uri(mysqlUrl);
        var user = uri.UserInfo.Split(':')[0];
        var pass = Uri.UnescapeDataString(uri.UserInfo.Split(':')[1]);
        var host = uri.Host;
        var port = uri.Port;
        var db = uri.LocalPath.Trim('/');
        return $"Server={host};Port={port};Database={db};User={user};Password={pass};SslMode=Required;Connection Timeout=30;";
    }
    return config.GetConnectionString("ParqueoDB") 
           ?? throw new InvalidOperationException("No hay cadena de conexión.");
}