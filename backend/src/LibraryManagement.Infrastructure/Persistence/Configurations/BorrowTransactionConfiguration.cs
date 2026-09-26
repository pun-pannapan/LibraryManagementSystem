using LibraryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Persistence.Configurations;

public sealed class BorrowTransactionConfiguration : IEntityTypeConfiguration<BorrowTransaction>
{
    public void Configure(EntityTypeBuilder<BorrowTransaction> builder)
    {
        builder.Property(transaction => transaction.Status).IsRequired();
        builder.Property(transaction => transaction.CreatedAtUtc).IsRequired();
        builder.Property(transaction => transaction.UpdatedAtUtc).IsRequired();
        builder.Property(transaction => transaction.RequestedAtUtc).IsRequired();

        builder.HasIndex(transaction => new { transaction.BookId, transaction.Status });
        builder.HasIndex(transaction => transaction.UserId);
        builder.HasIndex(transaction => transaction.BorrowedAtUtc);
        builder.HasIndex(transaction => new { transaction.UserId, transaction.Status });
        builder.HasIndex(transaction => new { transaction.Status, transaction.CreatedAtUtc });

        builder.HasOne(transaction => transaction.Book)
            .WithMany(book => book.BorrowTransactions)
            .HasForeignKey(transaction => transaction.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
