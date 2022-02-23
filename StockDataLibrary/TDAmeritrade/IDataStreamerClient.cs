namespace StockDataLibrary.TDAmeritrade
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IDataStreamerClient
    {
        Task<Stream> FetchOptionsStream(string ticker);
        Task<Stream> MakeRequest(string uri, bool retrying = false);
    }
}
