using System;
using System.Diagnostics;
using System.Globalization;

namespace Abc.IdentityServer4.Extensions
{
    internal static class DateTimeExtensions
    {
        private static volatile string[] s_dateTimeFormats;

        [DebuggerStepThrough]
        public static bool InFuture(this DateTime serverTime, DateTime now, int toleranceInSeconds = 10)
        {
            return now.AddSeconds(toleranceInSeconds) < serverTime;
        }

        [DebuggerStepThrough]
        public static bool InPast(this DateTime serverTime, DateTime now, int toleranceInSeconds = 10)
        {
            return now > serverTime.AddSeconds(toleranceInSeconds);
        }

        [DebuggerStepThrough]
        public static bool TryParseToUtcDateTime(this string s, out DateTime result)
        {
            result = default;

            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            return DateTime.TryParseExact(s, AcceptedDateTimeFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite, out result);
        }

        private static string[] AcceptedDateTimeFormats
        {
            get
            {
                if (s_dateTimeFormats == null)
                {
                    s_dateTimeFormats = new string[] {
                                            "yyyy-MM-ddTHH:mm:ss.fffffffZ", "yyyy-MM-ddTHH:mm:ss.ffffffZ",
                                            "yyyy-MM-ddTHH:mm:ss.fffffZ", "yyyy-MM-ddTHH:mm:ss.ffffZ",
                                            "yyyy-MM-ddTHH:mm:ss.fffZ", "yyyy-MM-ddTHH:mm:ss.ffZ",
                                            "yyyy-MM-ddTHH:mm:ss.fZ", "yyyy-MM-ddTHH:mm:ssZ",
                                            "yyyy-MM-ddTHH:mm:ss.fffffffzzz", "yyyy-MM-ddTHH:mm:ss.ffffffzzz",
                                            "yyyy-MM-ddTHH:mm:ss.fffffzzz", "yyyy-MM-ddTHH:mm:ss.ffffzzz",
                                            "yyyy-MM-ddTHH:mm:ss.fffzzz", "yyyy-MM-ddTHH:mm:ss.ffzzz",
                                            "yyyy-MM-ddTHH:mm:ss.fzzz", "yyyy-MM-ddTHH:mm:sszzz"
                                        };
                }

                return s_dateTimeFormats;
            }
        }
    }
}
