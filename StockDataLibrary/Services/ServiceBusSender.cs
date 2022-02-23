using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using System;
using System.Threading.Tasks;

namespace StockDataLibrary.Services
{
    public class ServiceBusProvider : IServiceBusProvider
    {
        private ChonkyConfiguration _chonkyConfiguration;
        public ServiceBusProvider(ChonkyConfiguration config)
        {
            _chonkyConfiguration = config;
        }

        public async Task<ServiceBusSender> GetServiceBusSender(string topic, bool autoCreate = false)
        {
            var serviceBusClient = new ServiceBusClient(_chonkyConfiguration.ServiceBusConnectionString);
            if (autoCreate)
            {
                var adminClient = new ServiceBusAdministrationClient(_chonkyConfiguration.ServiceBusConnectionString);
                try
                {
                    await adminClient.CreateTopicAsync(topic);
                } 
                catch (Exception)
                {
                    // we throw if the topic exists, so just skip it and keep trucking
                }
            }
            return serviceBusClient.CreateSender(topic);
        }
    }
}