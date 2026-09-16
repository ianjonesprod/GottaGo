using GottaGo.Application.Identity;
using Microsoft.AspNetCore.Identity;

namespace GottaGo.Infrastructure.Identity;

/// <summary>
/// Password hashing, delegated to Microsoft's implementation.
///
/// We are writing our own identity flow, but not our own crypto. PasswordHasher is PBKDF2
/// with a sensible iteration count, a versioned output format so the cost can be raised
/// later, and a constant-time comparison. Reimplementing that by hand is exactly the kind of
/// code where a subtle mistake is invisible until it matters.
///
/// It is referenced on its own - no Entity Framework stores, no UserManager, none of the rest
/// of ASP.NET Core Identity.
/// </summary>
internal sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> hasher = new();
    private static readonly object Placeholder = new();

    public string Hash(string password) => hasher.HashPassword(Placeholder, password);

    public bool Verify(string password, string hash)
    {
        var result = hasher.VerifyHashedPassword(Placeholder, hash, password);

        // SuccessRehashNeeded means the stored hash used older parameters. It is still a
        // correct password, so the sign-in succeeds; upgrading the stored hash would be a
        // worthwhile follow-up.
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
