namespace LibraryManagement.Application.Common.Exceptions;

public sealed class NotFoundException(string message) : LibraryManagementException(message);
