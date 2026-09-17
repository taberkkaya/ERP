using ERPServer.Domain.Demo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPServer.Infrastructure.Configurations;

internal sealed class DemoVisitorConfiguration : IEntityTypeConfiguration<DemoVisitor>
{
    public void Configure(EntityTypeBuilder<DemoVisitor> builder)
    {
        builder.Property(p => p.Email).HasMaxLength(254).IsRequired();
        builder.Property(p => p.DisplayEmail).HasMaxLength(254);
        builder.Property(p => p.CodeHash).HasMaxLength(64);
        builder.Property(p => p.IpAddress).HasMaxLength(45);
        builder.Property(p => p.UserAgent).HasMaxLength(400);
        builder.Property(p => p.Country).HasMaxLength(80);
        builder.Property(p => p.CountryCode).HasMaxLength(2);
        builder.Property(p => p.City).HasMaxLength(80);

        // Adres başına tek satır: aynı kişi tekrar geldiğinde yeni kayıt değil,
        // mevcut kaydın sayaçları büyüyor.
        builder.HasIndex(p => p.Email).IsUnique();

        // Hesaplanan alan; sütunu yok.
        builder.Ignore(p => p.IsVerified);
    }
}
