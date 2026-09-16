using GottaGo.Domain.Common;

namespace GottaGo.Domain.Users;

/// <summary>
/// The domain's notion of a person: who wrote a review and what to show next to it.
///
/// Deliberately holds no password hash, no email confirmation flag and no external login
/// details. Those are authentication concerns and live in the identity tables, so the domain
/// never has to care how somebody signed in.
/// </summary>
public sealed class UserProfile
{
    public UserProfile(Guid id, string displayName, bool isSeedUser, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("A user needs a display name to attribute reviews to.");
        }

        Id = id;
        DisplayName = displayName.Trim();
        IsSeedUser = isSeedUser;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public string DisplayName { get; }

    /// <summary>True for the invented reviewers in the demo dataset.</summary>
    public bool IsSeedUser { get; }

    public DateTimeOffset CreatedAt { get; }
}
