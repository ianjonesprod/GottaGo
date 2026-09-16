namespace GottaGo.Application.Abstractions;

/// <summary>The caller is signed in but not allowed to do this. The API turns this into a 403.</summary>
public sealed class ForbiddenException(string message) : Exception(message);
