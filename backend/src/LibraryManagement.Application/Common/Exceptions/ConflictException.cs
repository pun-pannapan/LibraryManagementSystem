namespace LibraryManagement.Application.Common.Exceptions;

public sealed class ConflictException(string message) : LibraryManagementException(message);
