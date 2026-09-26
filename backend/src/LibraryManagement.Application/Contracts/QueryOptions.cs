namespace LibraryManagement.Application.Contracts;

public static class BookSortFields
{
    public const string Title = "title";
    public const string Author = "author";
    public const string Isbn = "isbn";
    public const string Category = "category";
    public const string CategoryName = "categoryname";
    public const string PublicationYear = "publicationyear";
    public const string PublishedYear = "publishedyear";
    public const string ShelfCode = "shelfcode";
    public const string Location = "location";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Title,
        Author,
        Isbn,
        Category,
        CategoryName,
        PublicationYear,
        PublishedYear,
        ShelfCode,
        Location
    };
}

public static class BorrowingHistorySortFields
{
    public const string RequestedAt = "requestedat";
    public const string AssignedAt = "assignedat";
    public const string ReturnRequestedAt = "returnrequestedat";
    public const string BorrowedAt = "borrowedat";
    public const string DueAt = "dueat";
    public const string ReturnedAt = "returnedat";
    public const string Status = "status";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        RequestedAt,
        AssignedAt,
        ReturnRequestedAt,
        BorrowedAt,
        DueAt,
        ReturnedAt,
        Status
    };
}
