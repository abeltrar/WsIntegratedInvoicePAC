using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WsIntegratedInvoicePAC.Data;
using WsIntegratedInvoicePAC.Models;

namespace WsIntegratedInvoicePAC.Controller
{
    public class FacturaController
    {
        private readonly FacturaRepository _repository;

        public FacturaController(FacturaRepository repository)
        {
            _repository = repository;
        }


        public async Task<List<FE_Invoice_Transaccion>> ObtenerFacturasPendientes()
        {
            return await _repository.ObtenerFacturasPendientesAsync();
        }

        public async Task InsertarFacturaAsync()
        {
            await _repository.InsertarFacturaAsync();

        }


        public async Task InsertLog(FE_System_Log log)
        {
            await _repository.insertLog(log);

        }

        public async Task InsertLogEmail(FE_Log_SendEmail log)
        {
            await _repository.InsertLogEmail(log);

        }

        public async Task InsertResponseExit(string invoiceNumber)
        {
            await _repository.InsertResponseExit(invoiceNumber);

        }

        public async Task InsertResposeFail(string dgiErrMsg, string received, string accepted, int status2, string msge, string invoiceNumber)
        {
            await _repository.InsertResposeFail(dgiErrMsg, received, accepted, status2, msge, invoiceNumber);

        }

    }
}
