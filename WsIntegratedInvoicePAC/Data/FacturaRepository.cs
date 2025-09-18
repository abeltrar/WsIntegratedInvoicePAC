using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WsIntegratedInvoicePAC.Models;

namespace WsIntegratedInvoicePAC.Data
{
    public class FacturaRepository
    {
        private readonly AppDbContext _context;

        public FacturaRepository(AppDbContext context)
        {
            _context = context;
        }


        public async Task<List<FE_Invoice_Transaccion>> ObtenerFacturasPendientesAsync()
        {
            try
            {

                   return await _context.FE_Invoice_Transaccion
                  .FromSqlRaw("EXEC Sp_FE_GetInvoicePending")
                  .ToListAsync();

            }
            catch (SqlException sqlEx)
            {
                string detalles = $"Error {sqlEx.Number} en procedimiento {sqlEx.Procedure}, línea {sqlEx.LineNumber}: {sqlEx.Message}";

                var log = new FE_System_Log
                {
                    Invoice = "",
                    Fecha = DateTime.Now,
                    Modulo = "ObtenerFacturasPendientesAsync",
                    Mensaje = detalles,
                    StackTrace = sqlEx.StackTrace
                };



                _context.FE_System_Log.Add(log);
                await _context.SaveChangesAsync();
                return new List<FE_Invoice_Transaccion>();

            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error buscando facturas pendientes: {ex.Message}");

                var log = new FE_System_Log
                {
                    Invoice = "",
                    Fecha = DateTime.Now,
                    Modulo = "ObtenerFacturasPendientesAsync",
                    Mensaje = ex.Message,
                    StackTrace = ex.StackTrace
                };



                _context.FE_System_Log.Add(log);
                await _context.SaveChangesAsync();
                return new List<FE_Invoice_Transaccion>();




            }


        }

        //INSERTAR LOGS

        public async Task insertLog(FE_System_Log log)
        {
            try
            {

                _context.FE_System_Log.Add(log);
                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                Console.WriteLine("Error al insertar Log" + ex);

            }
          



        }

        //INSERTAR LOG DE CORREO



        public async Task InsertLogEmail(FE_Log_SendEmail log)
        {
            try
            {

                _context.FE_Log_SendEmail.Add(log);
                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                Console.WriteLine("Error al insertar Log de correo" + ex);

            }




        }




        public async Task InsertarFacturaAsync()
        {

            try
            {
                await _context.Database.ExecuteSqlRawAsync("EXEC Sp_FE_InsertPendingInvoices");



            }
            catch (SqlException sqlEx)
            {
                string detalles = $"Error {sqlEx.Number} en procedimiento {sqlEx.Procedure}, línea {sqlEx.LineNumber}: {sqlEx.Message}";

                var log = new FE_System_Log
                {
                    Invoice = "",
                    Fecha = DateTime.Now,
                    Modulo = "InsertarFacturaPendientesAsync",
                    Mensaje = detalles,
                    StackTrace = sqlEx.StackTrace
                };

                _context.FE_System_Log.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error insertando factura: {ex.Message}");

                var log = new FE_System_Log
                {
                    Invoice = "",
                    Fecha = DateTime.Now,
                    Modulo = "InsertarFacturaPendientesAsync",
                    Mensaje = ex.Message,
                    StackTrace = ex.StackTrace
                };

                _context.FE_System_Log.Add(log);
                await _context.SaveChangesAsync();

            }

        }


        //INSERTAR RESPUESTA DE EXITOSO

        public async Task InsertResponseExit(string invoiceNumber)
        {

            try
            {
                await _context.Database.ExecuteSqlRawAsync("EXEC Sp_FE_InsertExitResponse @InvoiceNumber = {0}", invoiceNumber);

            }
            catch (SqlException sqlEx)
            {
                string detalles = $"Error {sqlEx.Number} en procedimiento {sqlEx.Procedure}, línea {sqlEx.LineNumber}: {sqlEx.Message}";

                var log = new FE_System_Log
                {
                    Invoice = invoiceNumber,
                    Fecha = DateTime.Now,
                    Modulo = "InsertResponseExit",
                    Mensaje = detalles,
                    StackTrace = sqlEx.StackTrace
                };

                _context.FE_System_Log.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error Insertando Log de Exit: {ex.Message}");

                var log = new FE_System_Log
                {
                    Invoice = invoiceNumber,
                    Fecha = DateTime.Now,
                    Modulo = "InsertResponseExit",
                    Mensaje = ex.Message,
                    StackTrace = ex.StackTrace
                };

                _context.FE_System_Log.Add(log);
                await _context.SaveChangesAsync();

            }

        }


        public async Task InsertResposeFail(string dgiErrMsg, string received, string accepted, int status2, string msge, string invoiceNumber)
        {

            try
            {

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC Sp_FE_InsertFailProcessInvoices {0}, {1}, {2}, {3}, {4}, {5}",
                    invoiceNumber,
                    received,
                    accepted,
                    msge,
                    dgiErrMsg,
                    status2
                );
            }
            catch (SqlException sqlEx)
            {
                string detalles = $"Error {sqlEx.Number} en procedimiento {sqlEx.Procedure}, línea {sqlEx.LineNumber}: {sqlEx.Message}";

                var log = new FE_System_Log
                {
                    Invoice = invoiceNumber,
                    Fecha = DateTime.Now,
                    Modulo = "InsertResposeFail",
                    Mensaje = detalles,
                    StackTrace = sqlEx.StackTrace
                };

                _context.FE_System_Log.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error Insertando respuesta de error: {ex.Message}");

                var log = new FE_System_Log
                {
                    Invoice = invoiceNumber,
                    Fecha = DateTime.Now,
                    Modulo = "InsertResposeFail",
                    Mensaje = ex.Message,
                    StackTrace = ex.StackTrace
                };

                _context.FE_System_Log.Add(log);
                await _context.SaveChangesAsync();

            }

        }







    }
}
