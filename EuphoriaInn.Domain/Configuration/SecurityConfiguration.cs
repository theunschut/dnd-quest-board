namespace EuphoriaInn.Domain.Configuration;

internal class SecurityConfiguration
{
    public int? PasswordIterations { get; set; }

    public int? SaltSize { get; set; }

    public int? HashSize { get; set; }
}