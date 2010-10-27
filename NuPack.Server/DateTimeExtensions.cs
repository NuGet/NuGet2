using System;

namespace NuGet.Server {
    public static class DateTimeExtensions {
        public static DateTimeOffset TrimMilliseconds(this DateTimeOffset value) {
            return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, TimeSpan.Zero);
        }
    }
}
