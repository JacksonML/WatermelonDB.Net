namespace SpiceRackAPI.Utilities;

public static class Timestamp
{
    public static DateTime ToDateTime(long value)
    {
        return DateTime.UnixEpoch.AddSeconds(value);
    }

    public static long ToLong(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }

    public static long ToLinuxEpochSeconds(this DateTime dateTime)
    {
        return ToLong(dateTime);
    }
}