
using QuestPDF;
using QuestPDF.Fluent;
using WsIntegratedInvoicePAC.Models;
using QuestPDF.Infrastructure;
using WsIntegratedInvoicePAC.ViewPDF;
using WsIntegratedInvoicePAC.Controller;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using WsIntegratedInvoicePAC.Data;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using MailKit.Net.Smtp;
using MimeKit;
using static System.Formats.Asn1.AsnWriter;






namespace WsIntegratedInvoicePAC
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly FacturaController _facturaController;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;



        public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory, IConfiguration config, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _config = config;
            _scopeFactory = scopeFactory;




        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            if (_logger.IsEnabled(LogLevel.Information))
            {



                //while (!stoppingToken.IsCancellationRequested)
                //{








                //}
                //await Task.Delay(1000, stoppingToken);


                _logger.LogInformation("Insertando información de facturas pendientes...");

                using (var scope = _scopeFactory.CreateScope())
                {
                    var facturaController = scope.ServiceProvider.GetRequiredService<FacturaController>();

                    await facturaController.InsertarFacturaAsync();

                }


                _logger.LogInformation("Obteniendo token...");

                var token = await GetTokenAsync();



                using (var scope = _scopeFactory.CreateScope())
                {
                    var facturaController = scope.ServiceProvider.GetRequiredService<FacturaController>();

                    var facturas = await facturaController.ObtenerFacturasPendientes();


                    // Obtener token


                    if (string.IsNullOrEmpty(token))
                    {
                        Console.WriteLine("No se pudo obtener el token.");
                        var log = new FE_System_Log
                        {
                            Invoice = "Token", 
                            Fecha = DateTime.Now,
                            Modulo = "ObteniendoToken",
                            Mensaje = "No se pudo obtener el token de WebPos",
                            StackTrace = "No se pudo obtener token está vacío o NULL, intente el proceso de nuevo"
                        };

                        await facturaController.InsertLog(log);


                        return;
                    }

                }


                // Leer facturas desde BD
                _logger.LogInformation("Buscando información de facturas pendientes...");


                string baseurl = _config["ApiSettings:BaseUrl"];
                string SMTP = _config["ApiSettings:SMTP"];
                string UsuarioSMTP = _config["ApiSettings:UsuarioSMTP"];
                string PassSMTP = _config["ApiSettings:PassSMTP"];
                string EmailEmisor = _config["ApiSettings:EmailEmisor"];





                using (var scope = _scopeFactory.CreateScope())
                {
                    var facturaController = scope.ServiceProvider.GetRequiredService<FacturaController>();

                    var facturas = await facturaController.ObtenerFacturasPendientes();

                    if (!facturas.Any())
                    {
                        Console.WriteLine("El listado de facturas está vacío");
                        var log = new FE_System_Log
                        {
                            Invoice = "",
                            Fecha = DateTime.Now,
                            Modulo = "ObtenerFacturasPendientes",
                            Mensaje = "La lista de facturas pendientes de encuentra vacia",
                            StackTrace = "La lista de facturas no se llenó intente el proceso de nuevo o revise errores de SQL del SP Sp_FE_GetInvoicePending"
                        };

                        await facturaController.InsertLog(log);

                        return ;

                    }


                    var facturasAgrupadas = facturas.GroupBy(f => f.Factura_Numero);

                    foreach (var grupo in facturasAgrupadas)
                    {
                        var facturaBase = grupo.First();

                        string companyLicCod = facturaBase.companyLicCod;
                        string tipo_factura = "";
                        string docNumber = "";
                        string invoiceNumber = "";
                        string posCode = facturaBase.posCod;
                        string branchCod = facturaBase.branchCod;

                        if (facturaBase.Tipo_Factura == "CU")
                        {
                            tipo_factura = "46";
                            docNumber = facturaBase.NCF;
                            //docNumber = "E461234567739";
                            invoiceNumber = "";

                        }
                        else if (facturaBase.Tipo_Factura == "CR")
                        {
                            tipo_factura = "34";
                            docNumber = facturaBase.NCF;
                            //docNumber = "E341234565431";
                            invoiceNumber = facturaBase.Factura_Afectada_NC;

                        }

                        //CONSUTRUCCIÓN DE ARRAY PARA CONSTRUIR LA FACTURA

                        var items_fact = grupo.Select(linea => new ItemFactura
                        {
                            Linea_Numero = linea.Linea_Numero,
                            entrega_numero = linea.Entrega_Numero,
                            Cantidad_Enviada = linea.Cantidad_Enviada,
                            Precio_Unitario = linea.Precio_Unitario,
                            Producto_Codigo = linea.Producto_Codigo,
                            Producto_Descripcion = linea.Producto_Descripcion,
                            Cantidad_ordenada = linea.Cantidad_Ordenada,
                            Lote_Numero = linea.Lote_Numero,
                            Unidad_Medida = linea.Unidad_Medida,
                            Total_Factura = linea.Total_Factura,
                            Subtotal_Linea = linea.Subtotal_Linea



                        }).ToArray();


                        // Construimos el array de items con todas las líneas
                        var items = grupo.Select(linea => new 
                        {
                            id = linea.Linea_Numero,
                            qty = linea.Cantidad_Enviada,
                            price = linea.Precio_Unitario,
                            code = linea.Producto_Codigo,
                            desc = linea.Producto_Descripcion,
                            itemClass = 0,
                            tax = 0,
                            comments = "",
                            damt = 0.00m
                        }).ToArray();

                        var payload = new
                        {
                            fiscalDoc = new
                            {
                                companyLicCod = facturaBase.companyLicCod,
                                branchCod = facturaBase.branchCod,
                                posCod = facturaBase.posCod,
                                docType = tipo_factura,
                                docNumber = docNumber,
                                docNumberDueDate = "",
                                docDate = facturaBase.Factura_Fecha_Emision.ToString("yyyy-MM-dd"),
                                invoiceNumber = invoiceNumber,
                                customerName = facturaBase.Cliente_Nombre,
                                customerRUC = facturaBase.Cliente_Documento_Identidad,
                                customerType = "06",
                                customerAddress = facturaBase.Cliente_Direccion1,
                                email = facturaBase.Cliente_Email,
                                currency = "USD",
                                currencyRate = facturaBase.tasa_cambio,
                                fedo = new
                                {
                                    taxMode = facturaBase.Indicador_TaxMode,
                                    operMode = "01",
                                    paymentType = 0,
                                    incoterm = "FOB",
                                    destCountry = "",
                                    totValEst = 0
                                },
                                items = items,
                                discount = new
                                {
                                    perc = "0%",
                                    amt = 0
                                },
                                payments = Array.Empty<object>(),
                                trailer = new[]
                                {
                                    new { id = 1, value = "" },
                                    new { id = 2, value = "" }
                                }
                                        }
                            };



                        _logger.LogInformation("Consumiendo servicio de WebPos para enviar información de facturas...");

                        // Enviar JSON
                        var json = JsonSerializer.Serialize(payload);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        var response = await _httpClient.PostAsync(baseurl + "api/fedo/v1/test/SendFileToProcess/" + companyLicCod, content);

                        var respContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Factura {facturaBase.Factura_Numero} => {respContent}");
                        JObject resp = JObject.Parse(respContent);

                        bool received = resp.Value<bool>("received");
                        bool accepted = resp.Value<bool>("accepted");


                  






                        if (received && accepted)
                        {
                            string dateSentToDgi = resp.Value<string>("dateSentToDgi");
                            string msg = resp.Value<string>("msg");
                            string xmlFeSigned = resp.Value<string>("xmlFeSigned");
                            string qrB64 = resp.Value<string>("qrB64");

                            XDocument xmlDoc = XDocument.Parse(xmlFeSigned);

                            // Obtener primeros 6 caracteres de SignatureValue esto por normatividad
                            XNamespace ds = "http://www.w3.org/2000/09/xmldsig#";
                            string signatureValue = xmlDoc.Descendants(ds + "SignatureValue").FirstOrDefault()?.Value ?? "";
                            string signatureFirst6 = signatureValue.Length >= 6 ? signatureValue.Substring(0, 6) : signatureValue;

                            string fechaVencimiento = xmlDoc.Descendants("FechaVencimientoSecuencia").FirstOrDefault()?.Value ?? "";

                          
                            _logger.LogInformation("Generando factura en PDF...");

                            // Leer el logo
                            var logoPath = Path.Combine(AppContext.BaseDirectory, "img", "logo.png");
                            var logoBytes = File.ReadAllBytes(logoPath);


                            var facturaData = new FacturaData
                            {
                                Logo = logoBytes,
                                Encabezado = facturaBase,
                                Items = items_fact,
                                DateSentToDgi = dateSentToDgi,
                                SignatureFirst6 = signatureFirst6,
                                FechaVencimiento = fechaVencimiento,
                                QrB64 = qrB64
                            };



                            // Crear el documento
                            var factura = new FacturaPDF(facturaData);
                            string folder = Path.Combine(AppContext.BaseDirectory, "Invoice_generate");

                            if (!Directory.Exists(folder))
                            {
                                Directory.CreateDirectory(folder);
                            }

                            string fileName = $"Factura_{facturaBase.Factura_Numero}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                            // Ruta completa
                            string outputPath = Path.Combine(folder, fileName);


                            if (logoPath == null) throw new Exception("Logo no encontrado");

                            using (var stream = File.Create(outputPath))
                            {
                                factura.GeneratePdf(stream);
                            }
                            if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
                            {
                                throw new Exception("El PDF no se generó correctamente o está vacío");
                            }

                            _logger.LogInformation("Factura generada en: {path}", outputPath);


                            //try
                            //{
                            //    _logger.LogInformation("Enviando correo eléctronico");

                            //    var message = new MimeMessage();
                            //    message.From.Add(new MailboxAddress("B LEATHER MANUFACTURING", EmailEmisor));
                            //    message.To.Add(new MailboxAddress("Destinatario", facturaBase.Cliente_Email));
                            //    message.Subject = $"Invoice No. {facturaBase.Factura_Numero} – B'LEATHER MANUFACTURING, INC.";


                            //    string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates/email_template.html");


                            //    string htmlTemplate = File.ReadAllText(templatePath);

                            //    var builder = new BodyBuilder();
                            //    builder.HtmlBody = htmlTemplate;


                            //    builder.Attachments.Add(outputPath);


                            //    byte[] xmlBytes = System.Text.Encoding.UTF8.GetBytes(xmlFeSigned);
                            //    builder.Attachments.Add($"Factura_{facturaBase.Factura_Numero}.xml", xmlBytes, new ContentType("text", "xml"));

                            //    message.Body = builder.ToMessageBody();

                            //    using (var client = new SmtpClient())
                            //    {
                            //        client.Connect(SMTP, 587, MailKit.Security.SecureSocketOptions.StartTls);

                            //        client.Authenticate(UsuarioSMTP, PassSMTP);

                            //        client.Send(message);
                            //        client.Disconnect(true);
                            //    }

                            //    // Registrar log de envío exitoso
                            //    var log = new FE_Log_SendEmail
                            //    {
                            //        Email = facturaBase.Cliente_Email,
                            //        Invoice = facturaBase.Factura_Numero,
                            //        Status = "Email sent successfully",
                            //        SendStatus = 1, 
                            //        CreationDate = DateTime.Now
                            //    };

                            //    await facturaController.InsertLogEmail(log);


                            //    _logger.LogInformation("Email Enviado");

                            //    _logger.LogInformation("Creando confirmación de transacción");

                            //    await facturaController.InsertResponseExit(facturaBase.Factura_Numero);


                            //    _logger.LogInformation("Proceso finalizado");




                            //}
                            //catch (Exception ex)
                            //{
                            //    _logger.LogInformation("Error al enviar correo: " + ex.Message);

                            //    // Registrar log de envío fallido
                            //    var log = new FE_Log_SendEmail
                            //    {
                            //        Email = facturaBase.Cliente_Email,
                            //        Invoice = facturaBase.Factura_Numero,
                            //        Status = ex.Message,
                            //        SendStatus = 0, 
                            //        CreationDate = DateTime.Now
                            //    };

                            //    await facturaController.InsertLogEmail(log);

                            //}

                        }
                        else
                        {
                            int status = resp.Value<int>("status");
                            string msg = resp.Value<string>("msg");
                            string dgiResp = resp.Value<string>("dgiResp");
                            string received1 = resp.Value<string>("received");
                            string accepted1 = resp.Value<string>("accepted");



                            await facturaController.InsertResposeFail(dgiResp, received1, accepted1, status, msg, facturaBase.Factura_Numero);


                            string mensajeConcatenado = $"status : {status} + msg : {msg} + dgiResp : {dgiResp}";
                            Console.WriteLine($"{mensajeConcatenado}");
                        }
                    }



                }


               
            }
        }


        private async Task<string?> GetTokenAsync()
        {
            string apiUser = _config["ApiSettings:Username"];
            string apiPass = _config["ApiSettings:Password"];
            string BaseUrl = _config["ApiSettings:BaseUrl"];

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", apiUser),
                new KeyValuePair<string, string>("password", apiPass),
                new KeyValuePair<string, string>("grant_type", "password")
            });

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.PostAsync(BaseUrl+ "Token", content);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString();
        }


    }
}
