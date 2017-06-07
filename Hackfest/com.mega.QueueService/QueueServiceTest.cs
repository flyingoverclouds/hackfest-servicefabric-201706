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
       
        public async Task RunTest(Uri uri)
        {
            var queue = ServiceProxy.Create<IQueueService>(uri, new ServicePartitionKey()); 

            for (int n = 1; n <= 20; n++)
            {
                await queue.PushAsync(new QueueMessage() { Payload = $"#{n} {DateTime.Now}" });
            }
            Debug.WriteLine("A-Message count in queue :  " + await queue.GetCountAsync());
            var m = await queue.GetMessageAsync();
            while(m!=null)
            {
                Debug.WriteLine("Message : " + m.Payload);
                m = await queue.GetMessageAsync();
            }
            Debug.WriteLine("B-Message count in queue :  " + await queue.GetCountAsync());
        }
    }
}
