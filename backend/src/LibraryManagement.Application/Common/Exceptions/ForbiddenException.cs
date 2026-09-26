namespace LibraryManagement.Application.Common.Exceptions;

public sealed class ForbiddenException(string message) : LibraryManagementException(message);
