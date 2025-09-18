using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WsIntegratedInvoicePAC.Models
{
    public class ItemFactura
    {

      
        public string Linea_Numero { get; set; }
        public decimal Cantidad_Enviada { get; set; }
        public decimal Precio_Unitario { get; set; }
        public string Producto_Codigo { get; set; }
        public string Producto_Descripcion { get; set; }
        public string entrega_numero { get; set; }
        public decimal Cantidad_ordenada { get; set; }
        public string Lote_Numero { get; set; }
        public string Unidad_Medida { get; set; }
        public decimal Total_Factura { get; set; }
        public decimal Subtotal_Linea { get; set; }
      
    }
}
