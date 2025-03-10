using ERPServer.Domain.Entities;
using ERPServer.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPServer.Infrastructure.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).HasColumnType("varchar(50)");
        builder.Property(p => p.ProductType)
            .HasConversion(v => v.Value, v => ProductTypeEnum.FromValue(v))
            .HasColumnName("ProductType");
    }
}
