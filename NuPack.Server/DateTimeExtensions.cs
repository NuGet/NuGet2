using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NuPack.Server {
    public static class DateTimeExtensions {
        public static DateTime TrimToSeconds(this DateTime value) {
            return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second);
        }
    }
}