namespace StockDataLibrary.Services
{
    using Azure.Messaging.ServiceBus;
    using System.Threading.Tasks;

    public interface IServiceBusProvider
    {
        Task<ServiceBusSender> GetServiceBusSender(string topic, bool autoCreate = false);
    }
}