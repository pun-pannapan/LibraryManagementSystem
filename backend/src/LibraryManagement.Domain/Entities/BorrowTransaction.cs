using LibraryManagement.Domain.Enums;

namespace LibraryManagement.Domain.Entities;

public sealed class BorrowTransaction
{
    public Guid Id { get; set; }

    public Guid BookId { get; set; }

    public Book Book { get; set; } = null!;

    public Guid UserId { get; set; }

    public DateTime? BorrowedAtUtc { get; set; }

    public DateTime? DueAtUtc { get; set; }

    public DateTime? ReturnedAtUtc { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public DateTime? AssignedAtUtc { get; set; }

    public Guid? AssignedByUserId { get; set; }

    public DateTime? ReturnRequestedAtUtc { get; set; }

    public Guid? ProcessedByUserId { get; set; }

    public DateTime? RejectedAtUtc { get; set; }

    public Guid? RejectedByUserId { get; set; }

    public DateTime? CancelledAtUtc { get; set; }

    public BorrowTransactionStatus Status { get; set; } = BorrowTransactionStatus.Borrowed;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
