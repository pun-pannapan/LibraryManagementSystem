using LibraryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Data;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<BorrowTransaction> BorrowTransactions => Set<BorrowTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name).HasMaxLength(120).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(500);
            entity.HasIndex(category => category.Name).IsUnique();
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.Property(book => book.Isbn).HasMaxLength(20).IsRequired();
            entity.Property(book => book.Title).HasMaxLength(250).IsRequired();
            entity.Property(book => book.Author).HasMaxLength(200).IsRequired();
            entity.Property(book => book.Publisher).HasMaxLength(200);
            entity.HasIndex(book => book.Isbn).IsUnique();
            entity.HasOne(book => book.Category)
                .WithMany(category => category.Books)
                .HasForeignKey(book => book.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BorrowTransaction>(entity =>
        {
            entity.Property(transaction => transaction.BorrowerEmail).HasMaxLength(320).IsRequired();
            entity.Property(transaction => transaction.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(transaction => new { transaction.BookId, transaction.Status });
            entity.HasOne(transaction => transaction.Book)
                .WithMany(book => book.BorrowTransactions)
                .HasForeignKey(transaction => transaction.BookId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
