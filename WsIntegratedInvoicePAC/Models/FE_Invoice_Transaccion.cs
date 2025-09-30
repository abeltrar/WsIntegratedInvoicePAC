using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WsIntegratedInvoicePAC.Models
{
    public class FE_Invoice_Transaccion
    {
        public string? Factura_Numero { get; set; }
        public DateTime Factura_Fecha_Emision { get; set; }
        public string? Tipo_Factura { get; set; }
        public string? Factura_Afectada_NC { get; set; }

        public string? Cliente_Nombre { get; set; }
        public string? Cliente_Documento_Identidad { get; set; }
        public string? Direccion_Envio1 { get; set; }
        public string? Direccion_Envio2 { get; set; }

        public string? Cliente_Direccion1 { get; set; }
        public string? Cliente_Direccion2 { get; set; }
        public string? Cliente_Email { get; set; }

        public byte Indicador_TaxMode { get; set; } 

        public string? Orden_Numero { get; set; }
        public string? Linea_Numero { get; set; }
        public string? Entrega_Numero { get; set; }

        public decimal Cantidad_Enviada { get; set; }
        public decimal Cantidad_Ordenada { get; set; }

        public decimal Precio_Unitario { get; set; }
        public string? Producto_Codigo { get; set; }
        public string? Producto_Descripcion { get; set; }

        public byte Impuesto_Tipo { get; set; } 

        public string? Lote_Numero { get; set; }

        public DateTime Factura_Fecha { get; set; }

        public string? NCF { get; set; }
        public string? Orden_Compra_Cliente { get; set; }
        public string? Condiciones_Pago { get; set; }

        public string? Metodo_Envio { get; set; }
        public string? SHPVIA_31 { get; set; }
        public string? Punto_Entrega { get; set; }
        public string? Solicitado_Por { get; set; }
        public DateTime Orden_Fecha { get; set; }

        public string? Cliente_Codigo { get; set; }
        public string? Unidad_Medida { get; set; }

        public decimal Subtotal_Linea { get; set; }
        public decimal Total_Factura { get; set; }

        public string? Estado { get; set; }
        public string? Mensaje_Respuesta { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? companyLicCod { get; set; }
        public string? branchCod { get; set; }
        public string? posCod { get; set; }
        public decimal? tasa_cambio { get; set; }

        public string? ABA { get; set; }
        public string? SWIFT { get; set; }
        public string? ACCT { get; set; }
        public string? DateDue_Sec { get; set; }


    }
}
