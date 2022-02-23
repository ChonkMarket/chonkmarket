using System;
using System.Collections.Generic;
using System.Globalization;
using NodaTime;
using StockDataLibrary.Models;

namespace StockDataLibrary
{
    public static class TradingHours
    {
        private static readonly DateTimeZone easternTimeZone = DateTimeZoneProviders.Tzdb["America/New_York"];
        private static readonly DateTime epochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        private static readonly List<DateTime> Holidays = new() { 
            new DateTime(2021, 4, 2),
            new DateTime(2021, 5, 31),
            new DateTime(2021, 7, 5),
            new DateTime(2021, 9, 6),
            new DateTime(2021, 11, 25),
            new DateTime(2021, 12, 24),
            new DateTime(2022, 1, 17),
            new DateTime(2022, 2, 21),
            new DateTime(2022, 4, 15),
            new DateTime(2022, 5, 30),
            new DateTime(2022, 7, 4),
            new DateTime(2022, 9, 5),
            new DateTime(2022, 11, 24),
            new DateTime(2022, 12, 26)
        };

        private static readonly List<DateTime> ShortDates = new()
        {
            new DateTime(2021, 11, 26),
            new DateTime(2022, 11, 25)
        };

        public static bool IsTradingDay(DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                return false;
            if (Holidays.Contains(date.Date))
                return false;
            return true;
        }

        public static DateTime GetOpen(DateTime date)
        {
            date = TradingDay(date);
            var openTime = new LocalDateTime(date.Year, date.Month, date.Day, 9, 30);
            return openTime.InZoneLeniently(easternTimeZone).ToDateTimeUtc();
        }

        public static DateTime GetClose(DateTime date)
        {
            date = TradingDay(date);
            var closeTime = new LocalDateTime(date.Year, date.Month, date.Day, 16, 0);
            if (ShortDates.Contains(date))
            {
                closeTime = new LocalDateTime(date.Year, date.Month, date.Day, 13, 0);
            }
            return closeTime.InZoneLeniently(easternTimeZone).ToDateTimeUtc();
        }

        private static DateTime TradingDay(DateTime date)
        {
            while (!IsTradingDay(date))
                date = date.AddDays(-1);
            return date;
        }

        public static MarketHours GetMarketHours(DateTime date)
        {
            DateTimeOffset open = GetOpen(date);
            DateTimeOffset close = GetClose(date);
            return new MarketHours(open.ToUnixTimeMilliseconds(), close.ToUnixTimeMilliseconds(), TradingDay(date).ToString("M/d/yyyy"));
        }

        public static MarketHours GetMarketHours(string date)
        {
            return GetMarketHours(DateTime.ParseExact(date, "M/d/yyyy", CultureInfo.InvariantCulture));
        }

        public static MarketHours GetMarketHours(long epochTimeMilliseconds)
        {
            return GetMarketHours(epochTime.AddMilliseconds(epochTimeMilliseconds));
        }

        public static bool IsMarketOpen(long epochTimeMilliseconds)
        {
            var date = epochTime.AddMilliseconds(epochTimeMilliseconds);
            if (!IsTradingDay(date))
                return false;
            if (GetOpen(date) > date)
                return false;
            if (GetClose(date) <= date)
                return false;
            return true;
        }
    }
}
