using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;

namespace com.mega.queuecontract
{
    public class QueueClient
    {
        public static IQueueService Create(string queueName)
        {
            var svcUrl = $"fabric:/Hackfest/{queueName}";
            return ServiceProxy.Create<IQueueService>(new Uri(svcUrl), new ServicePartitionKey());
        }
    }
}
