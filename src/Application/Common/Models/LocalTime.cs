using TimeZoneConverter;

namespace POS.Application.Common.Models;

/// <summary>
/// O'zbekiston vaqti (UTC+5) — Windows va Linux/macOS uchun cross-platform
/// </summary>
public static class LocalTime
{
    // Windows: "Central Asia Standard Time" | Linux/macOS: "Asia/Tashkent"
    private static readonly TimeZoneInfo _tashkentZone = TZConvert.GetTimeZoneInfo("Central Asia Standard Time");

    public static DateTime GetUtc5Time()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tashkentZone);
    }
}
