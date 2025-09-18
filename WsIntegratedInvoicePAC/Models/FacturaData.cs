using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WsIntegratedInvoicePAC.Models;

namespace WsIntegratedInvoicePAC.Models
{
    public class FacturaData
    {

        public byte[] Logo { get; set; }
        public FE_Invoice_Transaccion Encabezado { get; set; }
        public IEnumerable<ItemFactura> Items { get; set; }
        public string DateSentToDgi { get; set; }
        public string SignatureFirst6 { get; set; }
        public string FechaVencimiento { get; set; }
        public string QrB64 { get; set; }
    }
}
