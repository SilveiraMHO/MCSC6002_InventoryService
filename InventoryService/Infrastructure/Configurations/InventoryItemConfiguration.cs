using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
    {
        public void Configure(EntityTypeBuilder<InventoryItem> builder)
        {
            builder.ToTable("inventory_item");
            builder.HasKey(column => column.Id);

            builder.Property(column => column.Id)
                   .HasColumnName("id");

            builder.Property(column => column.ProductId)
                   .HasColumnName("product_id");

            builder.Property(column => column.Barcode)
                   .HasColumnName("barcode")
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(column => column.Status)
                   .HasColumnName("status")
                   .HasMaxLength(50)
                   .IsRequired();
        }
    }
}
