using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using com.mega.contract.Result;

namespace com.mega.Result
{
  /// <summary>
  /// An instance of this class is created for each service replica by the Service Fabric runtime.
  /// </summary>
  internal sealed class Result : StatefulService, IResultService
  {
        private const string resultDictName = "results";

        public Result(StatefulServiceContext context)
            : base(context)
        {
        }

        public async Task<string> Get(Guid key)
        {
            var resultDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, TimestampedValue>>(resultDictName);
            using (var tx = this.StateManager.CreateTransaction())
            {
                var condValue = await resultDictionary.TryGetValueAsync(tx, key);
                return (condValue.HasValue) ? condValue.Value.Value : null;
            }
        }

        public async Task Set(Guid key, string value)
        {
            var resultDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, TimestampedValue>>(resultDictName);
            using (var tx = this.StateManager.CreateTransaction())
            {
                var timestampedValue = new TimestampedValue
                {
                    Value = value,
                    CreatedDateTime = DateTime.UtcNow
                };
                await resultDictionary.TryAddAsync(tx, key, timestampedValue);
                await tx.CommitAsync();
            }
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var resultDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, TimestampedValue>>("results");

            while (!cancellationToken.IsCancellationRequested)
            {
                using (var tx = this.StateManager.CreateTransaction())
                {

                    var dictEnumerable = await resultDictionary.CreateEnumerableAsync(tx);
                    var dictEnumerator = dictEnumerable.GetAsyncEnumerator();
                    while (await dictEnumerator.MoveNextAsync(cancellationToken))
                    {
                        var value = dictEnumerator.Current;
                        if (DateTime.UtcNow - value.Value.CreatedDateTime >= TimeSpan.FromMinutes(5))
                        {
                            await resultDictionary.TryRemoveAsync(tx, value.Key);
                        }
                    }
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }

    class TimestampedValue
    {
        public string Value { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }

}
