using AdministracionSoluciones.Services.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AdministracionSoluciones.BackgroundServices;

public sealed class VencimientoSolicitudesHostedService : BackgroundService
{
    private static readonly TimeSpan IntervaloEjecucion = TimeSpan.FromHours(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VencimientoSolicitudesHostedService> _logger;
    private readonly IConfiguration _configuration;

    public VencimientoSolicitudesHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<VencimientoSolicitudesHostedService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(
            _configuration.GetConnectionString("DefaultConnection")))
        {
            _logger.LogWarning(
                "El proceso automático de vencimientos no inició porque falta ConnectionStrings:DefaultConnection.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var solicitudes = scope.ServiceProvider
                    .GetRequiredService<IProcesadorVencimientosSolicitudes>();
                await solicitudes.ProcesarVencimientosAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló el proceso automático de vencimientos.");
            }

            try
            {
                await Task.Delay(IntervaloEjecucion, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

