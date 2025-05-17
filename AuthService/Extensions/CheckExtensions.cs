using System.Text.RegularExpressions;

namespace AuthService.Extensions;

public static class CheckExtensions
{
    public static bool IsNumberPhone(this string login)
    {
        string pattern = @"^8\d{10}$";

        bool isMatch = Regex.IsMatch(login, pattern);
        return isMatch;
    }

    public static bool IsEmail(this string login)
    {
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        bool isMatch = Regex.IsMatch(login, pattern);
        return isMatch;
    }
}