using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WsIntegratedInvoicePAC.Models
{
    [Table("FE_System_Log")]
    public class FE_System_Log
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("Invoice")]

        public string Invoice { get; set; }

        [Column("Fecha")]
        public DateTime Fecha { get; set; }

        [Column("Modulo")]
        public string Modulo { get; set; }

        [Column("Mensaje")]
        public string Mensaje { get; set; }

        [Column("StackTrace")]
        public string StackTrace { get; set; }
    }
}
