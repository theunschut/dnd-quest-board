using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Extensions;

public static class UserExtensions
{
    public static IEnumerable<User> WhereEmailConfirmed(this IEnumerable<User> users) =>
        users.Where(u => u.EmailConfirmed);
}
