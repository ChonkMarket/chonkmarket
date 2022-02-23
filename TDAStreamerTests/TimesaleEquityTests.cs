namespace TDAStreamerTests
{
    using Google.Protobuf;
    using StockDataLibrary.Protos;
    using StockDataLibrary.TDAmeritrade;
    using System;
    using System.IO;
    using System.Text.Json;
    using Xunit;

    public class TimesaleEquityTests
    {
        [Fact]
        public async void TestJsonDeserialization()
        {
            var testData = File.OpenRead(Path.Join("Fixtures", "TimesaleEquity.json"));
            var data = await JsonSerializer.DeserializeAsync<ReceivedMessage>(testData);
            Assert.Equal("TIMESALE_EQUITY", data.Data[0].Service);
            Assert.Equal(1618241280920, data.Data[0].Timestamp);
            Assert.Equal("SUBS", data.Data[0].Command);
            Assert.Equal(22, data.Data[0].Content.Count);
            var contentFirst = data.Data[0].Content[0];
            Assert.Equal("QQQ", contentFirst.Key);
            Assert.Equal(2251, contentFirst.Seq);
            Assert.Equal(1618241280148, contentFirst.TradeTime);
            Assert.Equal(335.8389892578125, contentFirst.Last);
            Assert.Equal(100.0, contentFirst.Size);
        }

        [Fact]
        public async void CanSerializeAndDeserializeToProto()
        {
            var testData = File.OpenRead(Path.Join("Fixtures", "TimesaleEquity.json"));
            var data = await JsonSerializer.DeserializeAsync<ReceivedMessage>(testData);
            var dataContent = data.Data[0].Content[0];
            Trade trade = new()
            {
                TradeTime = dataContent.TradeTime,
                Size = dataContent.Size,
                Last = dataContent.Last
            };
            var bytes = trade.ToByteArray();
            var newTrade = Trade.Parser.ParseFrom(bytes);
            Assert.Equal(trade, newTrade);
        }
    }
}
