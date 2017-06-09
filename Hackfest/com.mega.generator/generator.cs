using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Threading;
using System.Threading.Tasks;
using com.mega.contract;
using com.mega.queuecontract;
using Grpc.Core;
using Mega;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Client;
using System.Linq;
using com.mega.contract.result;
using System.Fabric.Health;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using com.mega.contract.Result;

namespace com.mega.generator
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Generator : StatelessService, IGeneratorService
    {
        private string _queueName;

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                new ServiceInstanceListener(this.CreateServiceRemotingListener),
            };
        }

        public Generator(StatelessServiceContext context)
            : base(context)
        {
            _queueName = System.Text.Encoding.Default.GetString(context.InitializationData);
            if (string.IsNullOrEmpty(_queueName))
            {
                // HACK for dev environment (defaultServices instanciation)
                _queueName = "RequestQueue";
            }
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    var queueClient = QueueClient.Create(_queueName);
                    var message = await queueClient.GetMessageAsync();

                    if (message != null)
                    {
                        string response;

                        response = await RunSproAsync(message.SessionType, message.UserName);

                        //response = $"ANSWER FOR {message.MessageId} : {message.UserName}   {message.SessionType}";
                        var resultClient = ResultClient.Create();
                        await resultClient.Set(message.MessageId, response).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, "EXCEPTION : " + ex.ToString());
                    await Task.Delay(250);
                }
            }
        }

        private async Task<string> RunSproAsync(string sessionType, string username)
        {
            try
            {
                var spro = await GetOrCreateSproServiceInstanceAsync(sessionType, username);


                var fabricClient = new FabricClient();
                var channel = new Channel(spro.Ip, spro.Port, ChannelCredentials.Insecure);
                var client = new NativeSession.NativeSessionClient(channel);

                var request = new GenerateRequest
                {
                    Username = username,
                    Type = sessionType,
                    Payload = DateTime.Now.Ticks.ToString()
                };

                var generateReply = await client.GenerateAsync(request, new CallOptions());

                var response = generateReply.Response;
                await channel.ShutdownAsync();
                return response;
            } catch (Exception e)
            {
                throw;
            }
        }

        private async Task<SprocAddressStruct> GetOrCreateSproServiceInstanceAsync(string messageSessionType, string messageUserName)
        {
            var urlPath = $"SPROC_{messageSessionType}_{messageUserName}";

            var url = new ServiceUriBuilder(urlPath).ToUri();
            var fabricClient = new FabricClient();

            ServiceDescription service=null;
            try
            {
                service = await fabricClient.ServiceManager.GetServiceDescriptionAsync(url);
            }
            catch (Exception ex)
            {
                // nothing to do -> exception mean no service with the requested namename
                service = null;
            }
            if (service == null)
            {
                StatelessServiceDescription newGeneratorDescription = new StatelessServiceDescription()
                {
                    ApplicationName = new Uri(this.Context.CodePackageActivationContext.ApplicationName),
                    ServiceName = url,
                    InstanceCount = 1,
                    ServiceTypeName = "com.mega.SproGuestExeType",
                    PartitionSchemeDescription = new SingletonPartitionSchemeDescription()
                };

                await fabricClient.ServiceManager.CreateServiceAsync(newGeneratorDescription).ConfigureAwait(false);

                service = await fabricClient.ServiceManager.GetServiceDescriptionAsync(url);


                var healthState = HealthState.Unknown;
                var count = 0;
                while (healthState != HealthState.Ok)
                {
                    if (count++ >= 10)
                    {
                        throw new TimeoutException("Time out waiting for " + url);
                    }

                    var serviceList = await fabricClient.QueryManager.GetServiceListAsync(new Uri(this.Context.CodePackageActivationContext.ApplicationName));
                    healthState = serviceList.Single(services => services.ServiceName == url).HealthState;

                    if (healthState != HealthState.Ok)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                }
            }

            ServicePartitionResolver resolver = ServicePartitionResolver.GetDefault();
            ResolvedServicePartition partition = await resolver.ResolveAsync(service.ServiceName,
                new ServicePartitionKey(), CancellationToken.None);

            var s = partition.Endpoints.First();
            // s.Address == {"Endpoints":{"com.mega.SproGuestExeTypeEndpoint":"localhost:33039"}}

            // HACK : exytraction fqdn : should using json deserialization instead of this bad hack
            int start = s.Address.IndexOf(":\"") + 2;
            int stop = s.Address.IndexOf("\"", start);
            var fqdn = s.Address.Substring(start, stop - start);

            var parts = fqdn.Split(':');

            return new SprocAddressStruct { ServiceName = service.ServiceName, Ip = parts[0], Port = Convert.ToInt32(parts[1]) };
        }
    }
}
