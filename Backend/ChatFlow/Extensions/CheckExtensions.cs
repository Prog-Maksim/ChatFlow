using System.Text.RegularExpressions;

namespace ChatFlow.Extensions;

public static class CheckExtensions
{
    /// <summary>
    /// Проверка строки на номер телефона
    /// </summary>
    /// <param name="text">Проверяемая строка</param>
    /// <returns>True - это номер телефона</returns>
    public static bool IsNumberPhone(this string text)
    {
        string pattern = @"^8\d{10}$";

        bool isMatch = Regex.IsMatch(text, pattern);
        return isMatch;
    }

    /// <summary>
    /// Проверка строки на почту
    /// </summary>
    /// <param name="text">Проверяемая строка</param>
    /// <returns>True - это почта</returns>
    public static bool IsEmail(this string text)
    {
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        bool isMatch = Regex.IsMatch(text, pattern);
        return isMatch;
    }
}