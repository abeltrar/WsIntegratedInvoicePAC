using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WsIntegratedInvoicePAC.Models;

namespace WsIntegratedInvoicePAC.Data
{
    public class AppDbContext : DbContext
    {

            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public DbSet<FE_Invoice_Transaccion> FE_Invoice_Transaccion { get; set; }
            public DbSet<FE_System_Log> FE_System_Log { get; set; }
            public DbSet<FE_config_WEBPOS> FE_config_WEBPOS { get; set; }
            public DbSet<FE_Log_SendEmail> FE_Log_SendEmail { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
                modelBuilder.Entity<FE_Invoice_Transaccion>()
                    .HasNoKey();

                modelBuilder.Entity<FE_config_WEBPOS>()
                      .HasNoKey();

                modelBuilder.Entity<FE_System_Log>()
                    .Property(x => x.Id)
                    .ValueGeneratedOnAdd();


            modelBuilder.Entity<FE_Log_SendEmail>()
                   .Property(x => x.Id)
                   .ValueGeneratedOnAdd();

            base.OnModelCreating(modelBuilder);

               


        }

        }
}
