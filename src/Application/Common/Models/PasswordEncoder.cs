namespace POS.Application.Common.Models;

internal static class PasswordEncoder
{
    public static string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

    public static bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
