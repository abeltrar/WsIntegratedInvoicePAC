using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WsIntegratedInvoicePAC.Models
{
    [Table("FE_logs_SendEmail")]
    public class FE_Log_SendEmail
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("Email")]
        [StringLength(40)]
        public string Email { get; set; }

        [Column("Invoice")]
        [StringLength(50)]
        public string Invoice { get; set; }

        [Column("STATUS")]
        public string Status { get; set; }

        [Column("Send_status")]
        public int SendStatus { get; set; }

        [Column("Creation_date")]
        public DateTime CreationDate { get; set; } = DateTime.Now;
    }
}
