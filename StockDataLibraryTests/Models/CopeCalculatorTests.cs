namespace StockDataLibraryTests.Models
{
    using StockDataLibrary;
    using StockDataLibrary.Models;
    using System.IO;
    using Xunit;

    public class CopeCalculatorTests
    {
        private readonly CopeCalculator coper = new();

        [Fact]
        public async void TestCopeForTdaOptionChains()
        {
            var validData = File.OpenRead(Path.Join("Data", "SPY-1617110977-79ec4321-0211-4792-9355-b3b68c65c244.json"));
            var validDataChain = await TdaOptionChain.ConstructFromJsonAsync(validData);
            Assert.Equal(0.0, validDataChain.Nope);
            Assert.Equal(0.0, coper.Calculate(validDataChain));
            var validData2 = File.OpenRead(Path.Join("Data", "SPY-1617111007-0581a20d-8f71-42e7-8dda-fbbc91317b74.json"));
            var validDataChain2 = await TdaOptionChain.ConstructFromJsonAsync(validData2);
            Assert.Equal(0.7091361284255981F, validDataChain2.Nope);
            Assert.Equal(2.3781685829162598F, coper.Calculate(validDataChain2));
            Assert.Equal(753.6169F, validDataChain2.Underlying.LocalCallOptionDelta);
            Assert.Equal(-659.957947F, validDataChain2.Underlying.LocalPutOptionDelta);
            Assert.Equal(393828, validDataChain2.Underlying.LocalVolume);
        }

        [Fact]
        public void TestCopeForOratsOptionChains()
        {
            var validData = File.OpenRead(Path.Join("Data", "ORATS", "SPY-1618927920-02f1b1ec-38b1-4ef8-a108-27c7390aa9ca.csv.gz"));
            var validDataChain = OratsOptionChain.ConstructFromGzippedOratsCsv(validData);
            coper.Calculate(validDataChain);
            Assert.Equal(68198.38F, validDataChain.TotalCallOptionDelta);
            Assert.Equal(-98891.65F, validDataChain.TotalPutOptionDelta);
            Assert.Equal(68198.38F, validDataChain.LocalCallOptionDelta);
            Assert.Equal(-98891.65F, validDataChain.LocalPutOptionDelta);

            validData = File.OpenRead(Path.Join("Data", "ORATS", "SPY-1618927950-290835bd-40af-483b-bb63-4afa9639f836.csv.gz"));
            var secondDataChain = OratsOptionChain.ConstructFromGzippedOratsCsv(validData);
            coper.Calculate(secondDataChain);
            Assert.Equal(68996.26F, secondDataChain.TotalCallOptionDelta);
            Assert.Equal(-99659.65F, secondDataChain.TotalPutOptionDelta);
            Assert.Equal(219.56041F, secondDataChain.LocalCallOptionDelta);
            Assert.Equal(-250.1386F, secondDataChain.LocalPutOptionDelta);
        }

        [Fact]
        public void TestCopeWithWeirdOratsData()
        {
            var validData = File.OpenRead(Path.Join("Data", "ORATS", "SPY-1618926000-b7d3dd3b-673b-4764-9ea5-6e82728f1177.csv.gz"));
            var secondDataChain = OratsOptionChain.ConstructFromGzippedOratsCsv(validData);
            coper.Calculate(secondDataChain);
            validData = File.OpenRead(Path.Join("Data", "ORATS", "SPY-1618926030-4943eba8-79f8-4309-91da-d50302c8b8f9.csv.gz"));
            var thirdDataChain = OratsOptionChain.ConstructFromGzippedOratsCsv(validData);
            coper.Calculate(thirdDataChain);
        }
    }
}
