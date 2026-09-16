namespace GottaGo.Domain.Common;

/// <summary>Raised when an operation would leave a domain object in an invalid state.</summary>
public sealed class DomainException(string message) : Exception(message);
