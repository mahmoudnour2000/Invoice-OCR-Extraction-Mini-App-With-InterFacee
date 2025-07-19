using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace InvoiceOcr.Model
{
    public class InvoiceDetailConfiguration : IEntityTypeConfiguration<InvoiceDetail>
    {
        public void Configure(EntityTypeBuilder<InvoiceDetail> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.InvoiceId)
                  .IsRequired();

            builder.Property(e => e.Description)
                  .IsRequired();

            builder.Property(e => e.Quantity)
                  .IsRequired();

            builder.Property(e => e.UnitPrice)
                  .HasColumnType("DECIMAL(10,2)")
                  .IsRequired();

            builder.Property(e => e.LineTotal)
                  .HasColumnType("DECIMAL(10,2)")
                  .IsRequired();

            builder.HasOne(d => d.Invoice)
                  .WithMany(i => i.Details)
                  .HasForeignKey(d => d.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
