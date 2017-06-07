using com.mega.queuecontract;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mega.QueueService
{
    class QueueServiceTest
    {
        public async Task RunTest()
        {
            /// <summary>
            /// Creation of a servicefabric remoting proxy to QueueService
            /// </summary>
            /// <returns></returns>
            IQueueService GetQueueServiceProxy(string tenantName, string queueName)
            {
                // TODO : implement optionnal security on remoting endpoint
                var svcUrl = $"fabric:/com.mega.webfront/QueueService";
                var queueSvcProxy = ServiceProxy.Create<IQueueService>(new Uri(svcUrl), new ServicePartitionKey());
                return queueSvcProxy;
            }

        }
    }
}
