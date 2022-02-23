using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using StockDataLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockDataLibraryTests.Mocks
{
    public class MockChonkyConfiguration : Mock<ChonkyConfiguration>
    {
        public MockChonkyConfiguration() : base(SetupConfiguration(), new Mock<IHostEnvironment>().Object)
        {
        }

        private static IConfiguration SetupConfiguration()
        {
            var configMock = new Mock<IConfiguration>();
            var sectionMock = new Mock<IConfigurationSection>();
            sectionMock.SetupGet(x => x[It.IsAny<string>()]).Returns("config");
            configMock.Setup(x => x.GetSection(It.IsAny<string>())).Returns(sectionMock.Object);
            configMock.SetupGet(x => x[It.IsAny<string>()]).Returns("config");
            configMock.SetupGet(x => x["CassandraPort"]).Returns("000");
            configMock.SetupGet(x => x["tickers"]).Returns("");
            configMock.SetupGet(x => x["ServiceBusConnectionString"]).Returns("Endpoint=sb://chonky.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key=");
            return configMock.Object;
        }
    }
}
