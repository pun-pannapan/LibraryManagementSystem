namespace LibraryManagement.Api.Models;

public sealed class BorrowTransaction
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public Book Book { get; set; } = null!;

    public required string BorrowerEmail { get; set; }

    public DateTime BorrowedAtUtc { get; set; }

    public DateTime DueAtUtc { get; set; }

    public DateTime? ReturnedAtUtc { get; set; }

    public string Status { get; set; } = "Borrowed";
}
