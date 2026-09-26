using LibraryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Persistence.Configurations;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.Property(book => book.Isbn).HasMaxLength(20).IsRequired();
        builder.Property(book => book.Title).HasMaxLength(250).IsRequired();
        builder.Property(book => book.Author).HasMaxLength(200).IsRequired();
        builder.Property(book => book.Publisher).HasMaxLength(200);
        builder.Property(book => book.ShelfCode).HasMaxLength(50);
        builder.Property(book => book.Location).HasMaxLength(150);
        builder.Property(book => book.AvailabilityStatus).IsRequired();
        builder.Property(book => book.CreatedAtUtc).IsRequired();
        builder.Property(book => book.UpdatedAtUtc).IsRequired();
        builder.Property(book => book.RowVersion).IsRowVersion();

        builder.HasIndex(book => book.Isbn).IsUnique();
        builder.HasIndex(book => book.Title);
        builder.HasIndex(book => book.Author);
        builder.HasIndex(book => book.CategoryId);
        builder.HasIndex(book => book.AvailabilityStatus);
        builder.HasIndex(book => book.ShelfCode);

        builder.HasOne(book => book.Category)
            .WithMany(category => category.Books)
            .HasForeignKey(book => book.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
