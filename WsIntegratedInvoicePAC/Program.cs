
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WsIntegratedInvoicePAC;
using WsIntegratedInvoicePAC.Controller;
using WsIntegratedInvoicePAC.Data;

var builder = Host.CreateApplicationBuilder(args);

// Configurar servicios
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<FacturaRepository>();
builder.Services.AddScoped<FacturaController>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<Worker>();

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddEventLog(); // Para Windows Event Log

// Configurar como servicio de Windows
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "InvoiceWorker";
});

try
{
    // Crear directorio de logs manualmente
    var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
    if (!Directory.Exists(logDirectory))
    {
        Directory.CreateDirectory(logDirectory);
       // Console.WriteLine($"Directorio de logs creado: {logDirectory}");
    }

    var host = builder.Build();

    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Iniciando Worker Service InvoiceWorker...");
    logger.LogInformation("Logs directory: {LogDirectory}", logDirectory);

    // Crear un log manual en archivo para depuración
    var startupLogFile = Path.Combine(logDirectory, $"startup-{DateTime.Now:yyyy-MM-dd}.log");
    await File.AppendAllTextAsync(startupLogFile,
        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Worker Service iniciado correctamente\n");

    host.Run();
}
catch (Exception ex)
{
    //Console.WriteLine($"Error crítico: {ex}");

    // Intentar guardar el error en archivo
    try
    {
        var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        if (!Directory.Exists(logDirectory))
            Directory.CreateDirectory(logDirectory);

        var errorLogFile = Path.Combine(logDirectory, $"error-{DateTime.Now:yyyy-MM-dd}.log");
        await File.AppendAllTextAsync(errorLogFile,
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR CRÍTICO:\n{ex}\n\n");

        //Console.WriteLine($"Error guardado en: {errorLogFile}");
    }
    catch (Exception logEx)
    {
       // Console.WriteLine($"No se pudo guardar el log de error: {logEx.Message}");
    }

    //Console.WriteLine("Presiona ENTER para cerrar...");
    //Console.ReadLine();
}