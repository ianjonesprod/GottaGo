namespace GottaGo.Application.Abstractions;

/// <summary>The thing the caller asked for does not exist. The API turns this into a 404.</summary>
public sealed class NotFoundException(string what) : Exception($"{what} was not found.");
