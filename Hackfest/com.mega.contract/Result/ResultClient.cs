using com.mega.contract.Result;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;

namespace com.mega.contract.result
{
    public class ResultClient
    {
        private const string svcUrl = "fabric:/Hackfest/Result";

        public static IResultService Create()
        {
            return ServiceProxy.Create<IResultService>(new Uri(svcUrl), new ServicePartitionKey());
        }
    }
}
