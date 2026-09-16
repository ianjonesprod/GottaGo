namespace GottaGo.Application.Abstractions;

/// <summary>Injectable time, so anything time-dependent can be tested without waiting.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
