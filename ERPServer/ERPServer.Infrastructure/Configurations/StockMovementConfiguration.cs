using ERPServer.Domain.Entities;
using ERPServer.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPServer.Infrastructure.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.Property(p => p.NumberOfEntries).HasColumnType("decimal(7,2)");
        builder.Property(p => p.NumberOfOutputs).HasColumnType("decimal(7,2)");
        builder.Property(p => p.Price).HasColumnType("money");

        builder.Property(p => p.Source)
            .HasConversion(source => source.Value, value => StockMovementSourceEnum.FromValue(value));

        builder.Property(p => p.ExternalDocumentNumber).HasMaxLength(40);

        // Defter aynı faturayı yeniden gönderdiğinde eski hareketler bu sütundan
        // bulunup siliniyor; her onayda tablo taranmasın.
        builder.HasIndex(p => p.ExternalDocumentId);
    }
}
