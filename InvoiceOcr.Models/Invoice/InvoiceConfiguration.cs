using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceOcr.Model
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(e => e.Id);
            
            builder.Property(e => e.InvoiceNumber)
                  .IsRequired();
                  
            builder.Property(e => e.InvoiceDate)
                  .IsRequired();
                  
            builder.Property(e => e.CustomerName)
                  .IsRequired();
                  
            builder.Property(e => e.TotalAmount)
                  .HasColumnType("DECIMAL(10,2)")
                  .IsRequired();
                  
            builder.Property(e => e.Vat)
                  .HasColumnType("DECIMAL(5,2)");
        }
    }
}
