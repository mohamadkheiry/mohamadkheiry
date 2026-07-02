namespace SmartCall.Application.Common;

public class NotFoundException(string message) : Exception(message);

public class ForbiddenException(string message) : Exception(message);

public class ConflictException(string message) : Exception(message);

public class AppValidationException(string message) : Exception(message);
