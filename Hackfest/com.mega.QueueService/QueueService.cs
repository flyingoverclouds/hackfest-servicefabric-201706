using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using com.mega.queuecontract;
using System.Net;

namespace com.mega.QueueService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class QueueService : Microsoft.ServiceFabric.Services.Runtime.StatefulService, IQueueService
    {
        StatelessServiceScaler scaler = null;

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
            var rule = new ScalingRule()
            {
                MinimalInstanceCount = 1, // always maintain 1 instance up
                MaximalInstanceCount = 5, // maximum is 5 instance count
                DecreaseThreshold = 1, // decrease instance count if less than 1 message in queue
                IncreaseThreshold = 10, // increase +1 instance count if higher than 10 message in queue
                DelayBetweenScaling = 10 
            };

            // TODO : replace the services names to scale by dynamic name constructing/retrieving.
            scaler = new StatelessServiceScaler(cancellationToken, rule, "fabric://Hackfest/generator");

            await base.RunAsync(cancellationToken);
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
            long count = -1;
            try
            {
                var queue = await this.StateManager.GetOrAddAsync<IReliableQueue<QueueMessage>>(QueueName).ConfigureAwait(false);
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var msgCV = await queue.TryDequeueAsync(tx).ConfigureAwait(false) ;
                    if (msgCV.HasValue)
                    {
                        count = await  queue.GetCountAsync(tx);
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
            finally
            {
                scaler?.UpdateMetric(count);
            }
            return null;
        }

        public async Task<Tuple<HttpStatusCode, QueueMessage>> PushAsync(QueueMessage message)
        {
            message.CreatedDateTime = DateTime.UtcNow;
            long count = -1;
            try
            {
                var queue = await this.StateManager.GetOrAddAsync<IReliableQueue<QueueMessage>>(QueueName).ConfigureAwait(false);
                using (var tx = this.StateManager.CreateTransaction())
                {
                    await queue.EnqueueAsync(tx, message);
                    count = await queue.GetCountAsync(tx);
                    await tx.CommitAsync();
                }

                return Tuple.Create(HttpStatusCode.OK, message);
            }
            catch(Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, $"QueueService.PushAsync() EXCEPTION : {ex}");
                return Tuple.Create(HttpStatusCode.InternalServerError, message);
            }
            finally
            {
                scaler?.UpdateMetric(count);
            }
        }
    }
}
