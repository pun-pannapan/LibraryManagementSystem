using LibraryManagement.Domain.Enums;

namespace LibraryManagement.Domain.Entities;

public sealed class BorrowTransaction
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public Book Book { get; set; } = null!;

    public Guid UserId { get; set; }

    public DateTime BorrowedAtUtc { get; set; }

    public DateTime DueAtUtc { get; set; }

    public DateTime? ReturnedAtUtc { get; set; }

    public BorrowTransactionStatus Status { get; set; } = BorrowTransactionStatus.Borrowed;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
