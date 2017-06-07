using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mega.queuecontract
{
    public interface IQueueService : IService
    {
        void PushAsync(QueueMessage message);

        Task<QueueMessage> GetMessageAsync();

        Task<long> GetCountAsync();
    }
}
