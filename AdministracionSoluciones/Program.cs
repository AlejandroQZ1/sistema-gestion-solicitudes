
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.BackgroundServices;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services;
using AdministracionSoluciones.Services.Abstract;
using AdministracionSoluciones.Infraestructura;

var builder = WebApplication.CreateBuilder(args);

// Agrega Razor Pages y protege las páginas mediante sesión.
builder.Services.AddRazorPages()
    .AddMvcOptions(options =>
        options.Filters.Add<SesionRequeridaFilter>());

// Acceso a datos: MySQL con Dapper.
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<CatalogoRepository>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<ICatalogoLookupService>(
    services => services.GetRequiredService<CatalogoService>());
builder.Services.AddScoped<BitacoraRepository>();

// Repositorio de tareas.
builder.Services.AddScoped<TareaRepository>();

// Repositorio de solicitudes (SOL1).
builder.Services.AddScoped<ISolicitudRepository, SolicitudRepository>();

// Servicios generales.
builder.Services.AddSingleton<ICifradoService, CifradoService>();
builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// Servicio y filtro de errores de tareas.
builder.Services.AddScoped<ITareaService, TareaService>();
builder.Services.AddScoped<ErroresTareas>();

// Servicio de solicitudes (SOL1, SOL5 y SOL6).
builder.Services.AddScoped<ISolicitudService, SolicitudService>();
builder.Services.AddScoped<IProcesadorVencimientosSolicitudes>(
    services => services.GetRequiredService<ISolicitudService>()
        as IProcesadorVencimientosSolicitudes
        ?? throw new InvalidOperationException(
            "El servicio de solicitudes debe procesar vencimientos."));
builder.Services.AddHostedService<VencimientoSolicitudesHostedService>();

// Desgloses y reporte de facturación.
builder.Services.AddScoped<DesgloseRepository>();
builder.Services.AddScoped<ReporteRepository>();
builder.Services.AddScoped<IDesgloseService, DesgloseService>();
builder.Services.AddScoped<IReporteService, ReporteService>();

// Servicio de correo electrónico (SOL6).
builder.Services.AddScoped<ICorreoService, CorreoService>();

// Sesión de usuario (USR1).
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Crea el administrador inicial si la tabla de usuarios está vacía.
await CrearAdministradorInicialAsync(app);

// Configuración del procesamiento de solicitudes HTTP.
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
    var config = app.Configuration;
    var nombreUsuario = config["AdministradorInicial:NombreUsuario"];
    var nombreCompleto = config["AdministradorInicial:NombreCompleto"];
    var correo = config["AdministradorInicial:Correo"];
    var contrasena = config["AdministradorInicial:Contrasena"];

    if (string.IsNullOrWhiteSpace(config.GetConnectionString("DefaultConnection"))
        || string.IsNullOrWhiteSpace(config["Cifrado:Clave"])
        || string.IsNullOrWhiteSpace(nombreUsuario)
        || string.IsNullOrWhiteSpace(nombreCompleto)
        || string.IsNullOrWhiteSpace(correo)
        || string.IsNullOrWhiteSpace(contrasena))
    {
        app.Logger.LogWarning(
            "Se omitió la creación del administrador inicial porque falta configuración externa requerida.");
        return;
    }

    try
    {
        using var scope = app.Services.CreateScope();
        var usuarioService =
            scope.ServiceProvider.GetRequiredService<IUsuarioService>();

        await usuarioService.CrearAdministradorInicialAsync(
            nombreUsuario,
            nombreCompleto,
            correo,
            contrasena);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "No se pudo preparar el administrador inicial. Revise la conexión a MySQL y la configuración externa requerida.");
    }
}
