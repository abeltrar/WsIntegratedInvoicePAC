using Microsoft.EntityFrameworkCore;
using System;
using WsIntegratedInvoicePAC;
using WsIntegratedInvoicePAC.Controller;
using WsIntegratedInvoicePAC.Data;
using System.Net.Http;
using Microsoft.Extensions.Hosting;



var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<FacturaRepository>();
builder.Services.AddScoped<FacturaController>();

builder.Services.AddHttpClient();

builder.Services.AddHostedService<Worker>();

// Registro como servicio de Windows
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "InvoiceWorker";
});


var host = builder.Build();
host.Run();