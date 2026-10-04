using AdministracionSoluciones.Data;
using AdministracionSoluciones.Filters;
using AdministracionSoluciones.Models;
using AdministracionSoluciones.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Base de datos (cadena de conexión en appsettings.json → "ConnectionStrings:SIGES")
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SIGES")));

// Servicios compartidos
builder.Services.AddSingleton<CifradoService>();
builder.Services.AddScoped<BitacoraService>();

// Sesión de usuario (USR1)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// El filtro SesionRequeridaFilter protege TODAS las pantallas: sin sesión → login
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SesionRequeridaFilter>();
});

var app = builder.Build();

// Crea el usuario administrador inicial si la tabla de usuarios está vacía
await CrearAdministradorInicialAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


static async Task CrearAdministradorInicialAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var cifrado = scope.ServiceProvider.GetRequiredService<CifradoService>();
    var config = app.Configuration;

    if (await db.Usuarios.AnyAsync())
    {
        return;
    }

    db.Usuarios.Add(new Usuario
    {
        NombreUsuario = config["AdministradorInicial:NombreUsuario"] ?? "admin",
        NombreCompleto = config["AdministradorInicial:NombreCompleto"] ?? "Administrador del sistema",
        Correo = config["AdministradorInicial:Correo"] ?? "admin@siges.com",
        Contrasena = cifrado.Cifrar(config["AdministradorInicial:Contrasena"] ?? "Admin123*"),
        Estado = EstadosUsuario.Activo
    });
    await db.SaveChangesAsync();
}
