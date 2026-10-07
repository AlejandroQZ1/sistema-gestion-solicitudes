using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services;
using AdministracionSoluciones.Services.Abstract;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// El filtro SesionRequeridaFilter protege TODAS las páginas: sin sesión → login (USR1)
builder.Services.AddRazorPages()
    .AddMvcOptions(options => options.Filters.Add<SesionRequeridaFilter>());

// Acceso a datos (MySQL con Dapper)
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<BitacoraRepository>();

// Servicios
builder.Services.AddSingleton<ICifradoService, CifradoService>();
builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// Sesión de usuario (USR1)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var app = builder.Build();

// Crea el usuario administrador inicial si la tabla de usuarios está vacía
await CrearAdministradorInicialAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapRazorPages();

app.Run();


static async Task CrearAdministradorInicialAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
    var config = app.Configuration;

    try
    {
        await usuarioService.CrearAdministradorInicialAsync(
            config["AdministradorInicial:NombreUsuario"] ?? "admin",
            config["AdministradorInicial:NombreCompleto"] ?? "Administrador del sistema",
            config["AdministradorInicial:Correo"] ?? "admin@siges.com",
            config["AdministradorInicial:Contrasena"] ?? "Admin123*");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex,
            "No se pudo conectar a MySQL para crear el administrador inicial. " +
            "Revise que MySQL esté encendido, que se haya ejecutado el script de BaseDatos " +
            "y que la cadena 'DefaultConnection' de appsettings.json tenga el usuario y la contraseña correctos.");
    }
}
