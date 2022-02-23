namespace StockDataLibraryTests
{
    using StockDataLibrary;
    using StockDataLibrary.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class TradingHoursTests
    {
        [Fact]
        public void TestTradingDays()
        {
            Assert.True(TradingHours.IsTradingDay(new DateTime(2021, 3, 26)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2021, 3, 27)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2021, 3, 28)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2021, 4, 2)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2021, 5, 31)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2021, 7, 5)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2021, 9, 6)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2021, 11, 25)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2021, 12, 24)));

            Assert.False(TradingHours.IsTradingDay(new DateTime(2022, 1, 17)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2022, 2, 21)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2022, 4, 15)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2022, 5, 30)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2022, 7, 4)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2022, 9, 5)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2022, 11, 24)));
            Assert.False(TradingHours.IsTradingDay(new DateTime(2022, 12, 26)));
        }

        [Fact]
        public void TestReturnsCorrectOpenDates()
        {
            var openTime = DateTimeOffset.FromUnixTimeMilliseconds(1616765400000).DateTime;
            Assert.Equal(openTime, TradingHours.GetOpen(new DateTime(2021, 3, 26)));
            Assert.Equal(openTime, TradingHours.GetOpen(new DateTime(2021, 3, 27)));
            Assert.Equal(openTime, TradingHours.GetOpen(new DateTime(2021, 3, 28)));
        }

        [Fact]
        public void TestReturnsCorrectCloseDates()
        {
            var normalCloseTime = DateTimeOffset.FromUnixTimeMilliseconds(1616788800000).DateTime;
            var shortCloseTime = DateTimeOffset.FromUnixTimeMilliseconds(1637949600000).DateTime;
            Assert.Equal(normalCloseTime, TradingHours.GetClose(new DateTime(2021, 3, 26)));
            Assert.Equal(shortCloseTime, TradingHours.GetClose(new DateTime(2021, 11, 26)));
        }

        [Fact]
        public void TestReturnsCorrectTimes()
        {
            var normalDay = new DateTime(2021, 3, 26);
            var normalExpectedHours = new MarketHours(1616765400000, 1616788800000, "3/26/2021");
            Assert.Equal(normalExpectedHours, TradingHours.GetMarketHours(normalDay));

            var weekendDay = new DateTime(2021, 3, 28);
            Assert.Equal(normalExpectedHours, TradingHours.GetMarketHours(weekendDay));

            var holidayDay = new DateTime(2021, 4, 2, 11, 1, 1, 1);
            var holidayExpectedHours = new MarketHours(1617283800000, 1617307200000, "4/1/2021");
            Assert.Equal(holidayExpectedHours, TradingHours.GetMarketHours(holidayDay));

            var shortDay = new DateTime(2021, 11, 26);
            var shortDayExpectedHours = new MarketHours(1637937000000, 1637949600000, "11/26/2021");
            Assert.Equal(shortDayExpectedHours, TradingHours.GetMarketHours(shortDay));
        }

        [Fact]
        public void TestIfMarketOpenDuringTime()
        {
            Assert.True(TradingHours.IsMarketOpen(1617199151029));
            // holiday day
            Assert.False(TradingHours.IsMarketOpen(1617389581000));
            // preopen
            Assert.False(TradingHours.IsMarketOpen(1617283539000));
            // after close
            Assert.False(TradingHours.IsMarketOpen(1617312339000));
        }
    }
}
