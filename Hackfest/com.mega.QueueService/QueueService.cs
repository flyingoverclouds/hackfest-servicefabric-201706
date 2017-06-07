using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using com.mega.queuecontract;

namespace com.mega.QueueService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class QueueService : StatefulService , IQueueService
    {
        private const string QueueName = "messageQueue";
        public QueueService(StatefulServiceContext context)
            : base(context)
        { }


        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            // TODO : implement security on remoting endpoint
            var listeners = new ServiceReplicaListener[1]
            {
                new ServiceReplicaListener( (context) => this.CreateServiceRemotingListener(context), "ServiceEndpoint")
            };
            return listeners;
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            int testCounter = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                testCounter++;
                //if (testCounter==20 && Environment.MachineName=="NICLERCSB") // HACK for Nico test
                //{
                //    var qt = new QueueServiceTest();
                //    await qt.RunTest(); 
                //}
            }
        }

        

        public async Task<long> GetCountAsync()
        {
            try
            {
                var queue = await this.StateManager.GetOrAddAsync<IReliableQueue<QueueMessage>>(QueueName).ConfigureAwait(false);
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var count = await queue.GetCountAsync(tx).ConfigureAwait(false);
                    tx.Abort();
                    return count;
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, $"QueueService.GetMessageAsync() EXCEPTION : {ex}");
                return 0; // HACK : should not return a value if exception occured !!!
            }
        }

        public async Task<QueueMessage> GetMessageAsync()
        {
            try
            {
                var queue = await this.StateManager.GetOrAddAsync<IReliableQueue<QueueMessage>>(QueueName).ConfigureAwait(false);
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var msgCV = await queue.TryDequeueAsync(tx).ConfigureAwait(false) ;
                    if (msgCV.HasValue)
                    {
                        await tx.CommitAsync();
                        return msgCV.Value;
                    }
                    tx.Abort();
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, $"QueueService.GetMessageAsync() EXCEPTION : {ex}");
            }
            return null;
        }

        public async Task PushAsync(QueueMessage message)
        {
            try
            {
                var queue = await this.StateManager.GetOrAddAsync<IReliableQueue<QueueMessage>>(QueueName).ConfigureAwait(false);
                using (var tx = this.StateManager.CreateTransaction())
                {
                    await queue.EnqueueAsync(tx, message);
                    await tx.CommitAsync();
                }
            }
            catch(Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, $"QueueService.PushAsync() EXCEPTION : {ex}");
            }
        }
    }
}
