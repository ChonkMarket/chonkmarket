namespace StockDataLibraryTests.TDAmeritrade
{
    using StockDataLibrary;
    using StockDataLibrary.Models;
    using System;
    using System.IO;
    using Xunit;

    public class TdaOptionChainTests
    {
        [Fact]
        public async void TestSmallOptionModel()
        {
            var testData = File.OpenRead(Path.Join("Data", "spy-1615604763-362845c5-ca56-4caf-b081-142b16e46aaa.json"));
            var optionChain = await TdaOptionChain.ConstructFromJsonAsync(testData);
            Assert.Equal(0, optionChain.DaysToExpiration);
            Assert.Equal("SPY", optionChain.Symbol);
            Assert.True(optionChain.CallExpDateMap["2021-03-15:2"]["225.0"][0].Call);
            Assert.Equal("SPY_031521C225", optionChain.CallExpDateMap["2021-03-15:2"]["225.0"][0].Symbol);
            Assert.Equal("SPY Mar 15 2021 225 Call (Weekly)", optionChain.CallExpDateMap["2021-03-15:2"]["225.0"][0].Description);
            Assert.Equal(394.05999755859375F, optionChain.Underlying.Close);
            Assert.Equal(64653565, optionChain.Underlying.TotalVolume);
            Assert.Equal(8.153507F, optionChain.Nope);
            Assert.Equal(-33.240234375, optionChain.Underlying.CallGex - optionChain.Underlying.PutGex);
        }

        [Fact]
        public async void TestFullOptionModel()
        {
            var testData = File.OpenRead(Path.Join("Data", "test.json"));
            var optionChain = await TdaOptionChain.ConstructFromJsonAsync(testData);
            Assert.Equal(0, optionChain.DaysToExpiration);
            Assert.Equal("SPY", optionChain.Symbol);
            Assert.True(optionChain.CallExpDateMap["2021-03-15:3"]["225.0"][0].Call);
            Assert.Equal("SPY_031521C225", optionChain.CallExpDateMap["2021-03-15:3"]["225.0"][0].Symbol);
            Assert.Equal("SPY Mar 15 2021 225 Call (Weekly)", optionChain.CallExpDateMap["2021-03-15:3"]["225.0"][0].Description);
            Assert.Equal(27.724163F, optionChain.Nope);
        }

        [Fact]
        public async void TestArkkData()
        {
            var testData = File.OpenRead(Path.Join("Data", "ARKK-1615674307-8f1de965-3dca-49ea-9e93-77b3908c9d27.json"));
            var optionChain = await TdaOptionChain.ConstructFromJsonAsync(testData);
            Assert.Equal(0, optionChain.DaysToExpiration);
            Assert.Equal("ARKK", optionChain.Symbol);
            Assert.Equal(1615597194785, optionChain.Underlying.QuoteTime);
        }

        [Fact]
        public async void TestWrongUnderlyingStock()
        {
            var testData = File.OpenRead(Path.Join("Data", "IWM-1616428696-dc4d57b6-5b8a-4aa0-9895-e6b32d801f52.json"));
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await TdaOptionChain.ConstructFromJsonAsync(testData);
            });
        }

        [Fact]
        public async void TestEnsureRawDataFieldIsSetCorrectly()
        {
            var testData = File.OpenRead(Path.Join("Data", "ARKK-1615674307-8f1de965-3dca-49ea-9e93-77b3908c9d27.json"));
            var optionChain = await TdaOptionChain.ConstructFromJsonAsync(testData, "testing");
            Assert.Equal("testing", optionChain.RawData);
        }

        [Fact]
        public async void TestWeHandle25CentStrikePrices()
        {

            var testData = File.OpenRead(Path.Join("Data", "AAPL-1618493400-636c2f2e-a322-40bd-bdef-0912097812ce.json"));
            var optionChain = await TdaOptionChain.ConstructFromJsonAsync(testData, "testing");
            Assert.Equal("AAPL", optionChain.Symbol);
        }

        [Fact]
        public async void TestEnsureNopeIsAddedToUnderlying()
        {
            var testData = File.OpenRead(Path.Join("Data", "spy-1615604763-362845c5-ca56-4caf-b081-142b16e46aaa.json"));
            var optionChain = await TdaOptionChain.ConstructFromJsonAsync(testData);
            Assert.Equal(8.153507F, optionChain.Underlying.Nope);
            Assert.Equal(134983.31F, optionChain.Underlying.TotalCallOptionDelta);
            Assert.Equal(-82267.984F, optionChain.Underlying.TotalPutOptionDelta);
        }

        // leaving this test in here, this is an example of TDA returning a bunch of 'NaN' greek values which create a significant skew
        // in the ARKK nope value
        // if we someday figure out how to calculate the greeks ourselves, this is a good one to test it on
        [Fact]
        public async void TestArkkWeirdness()
        {
            var validData = File.OpenRead(Path.Join("Data", "Arkk", "ARKK-1616788641-e8a07c39-feb3-4111-8d4b-cd5fd7694710.json"));
            var validDataChain = await TdaOptionChain.ConstructFromJsonAsync(validData);
            Assert.Equal(-57979.19921875F, validDataChain.Underlying.TotalPutOptionDelta);
            // missing a lot (maybe all) 0dte puts that were included in the file above
            var invalidData = File.OpenRead(Path.Join("Data", "Arkk", "ARKK-1616788701-0c1871d9-c65b-465b-97cd-807ed7cd4876.json"));
            var invalidDataChain = await TdaOptionChain.ConstructFromJsonAsync(invalidData);
            Assert.Equal(-44140.336F, invalidDataChain.Underlying.TotalPutOptionDelta);
        }
    }
}
