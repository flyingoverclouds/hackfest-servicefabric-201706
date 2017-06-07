using com.mega.queuecontract;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mega.QueueService
{
    class QueueServiceTest
    {
        /// <summary>
        /// Creation of a servicefabric remoting proxy to QueueService
        /// </summary>
        /// <returns></returns>
        IQueueService GetQueueServiceProxy(string queueName)
        {
            // TODO : implement optionnal security on remoting endpoint
            var svcUrl = $"fabric:/Hackfest/{queueName}";
            var queueSvcProxy = ServiceProxy.Create<IQueueService>(new Uri(svcUrl), new ServicePartitionKey());
            return queueSvcProxy;
        }

        public async Task RunTest()
        {
            var queue = GetQueueServiceProxy("QueueService");

            for (int n = 1; n <= 20; n++)
            {
                await queue.PushAsync(new QueueMessage() { Payload = $"#{n} {DateTime.Now}" });
            }
            Debug.WriteLine("Message count in queue :  " + await queue.GetCountAsync());
            var m = await queue.GetMessageAsync();
            while(m!=null)
            {
                Debug.WriteLine("Message : " + m.Payload);
                m = await queue.GetMessageAsync();
            }
            Debug.WriteLine("Message count in queue :  " + await queue.GetCountAsync());
        }
    }
}
