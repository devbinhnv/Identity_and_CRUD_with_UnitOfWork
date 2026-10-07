using IdentityUoW.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityUoW.Api.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("products", Schemas.Catalog);

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.No).HasMaxLength(50);
        builder.Property(p => p.Name).HasMaxLength(250);
        builder.Property(p => p.Summary).HasMaxLength(500);
        builder.Property(p => p.Price).HasPrecision(12, 2);

        builder.HasIndex(p => p.No).IsUnique();
        builder.HasIndex(p => p.CreatedBy);
    }
}
