namespace LibraryManagement.Domain.Enums;

public enum BorrowTransactionStatus
{
    Borrowed = 1,
    Returned = 2,
    Requested = 3,
    ReturnRequested = 4,
    Rejected = 5,
    Cancelled = 6
}
