using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WsIntegratedInvoicePAC.Models
{
    public class ItemWebPOS
    {
        public string id { get; set; }
        public decimal qty { get; set; }
        public decimal price { get; set; }
        public string code { get; set; }
        public string desc { get; set; }
        public int itemClass { get; set; }
        public int tax { get; set; }
        public string comments { get; set; }
        public decimal damt { get; set; }
    }
}
