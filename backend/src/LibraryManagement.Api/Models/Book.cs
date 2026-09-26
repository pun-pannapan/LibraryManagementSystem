namespace LibraryManagement.Api.Models;

public sealed class Book
{
    public int Id { get; set; }

    public required string Isbn { get; set; }

    public required string Title { get; set; }

    public required string Author { get; set; }

    public string? Publisher { get; set; }

    public int? PublishedYear { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<BorrowTransaction> BorrowTransactions { get; set; } = new List<BorrowTransaction>();
}
