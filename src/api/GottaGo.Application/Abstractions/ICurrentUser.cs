namespace GottaGo.Application.Abstractions;

/// <summary>Who is making the current request, if anyone.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated => UserId is not null;
}
