using LibraryManagement.Domain.Enums;

namespace LibraryManagement.Domain.Entities;

public sealed class Book
{
    public Guid Id { get; set; }

    public required string Isbn { get; set; }

    public required string Title { get; set; }

    public required string Author { get; set; }

    public string? Publisher { get; set; }

    public int? PublishedYear { get; set; }

    public BookAvailabilityStatus AvailabilityStatus { get; set; } = BookAvailabilityStatus.Available;

    public Guid CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<BorrowTransaction> BorrowTransactions { get; set; } = new List<BorrowTransaction>();
}
