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
            if (string.IsNullOrEmpty(_queueName)) // HACK for dev environment (defaultServices instanciation)
                _queueName = "RequestQueue";

        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                var queueClient = QueueClient.Create(_queueName);
                var message = await queueClient.GetMessageAsync();

                if (message != null)
                {
                    var response = await GetOrCreateSproExeAsync(message.SessionType, message.UserName);
                    var responseMessage = new QueueMessage("sessionType", "userName");

                    var answerQueue = QueueClient.Create("AnswerQueue");

                    await answerQueue.PushAsync(responseMessage).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
        }

        private async Task<string> GetOrCreateSproExeAsync(string sessionType, string username)
        {
            var sprocInstance = await GetSprocInstanceAsync(sessionType, username);

            var channel = new Channel(sprocInstance.Ip, sprocInstance.Port, ChannelCredentials.Insecure);

            var client =
                new NativeSession.NativeSessionClient(channel);

            var request = new GenerateRequest
            {
                Username = "admin",
                Type = "BPMN",
                Payload = DateTime.Now.Ticks.ToString()
            };


            var generateReply = await client.GenerateAsync(request, new CallOptions());

            var response = generateReply.Response;

            channel.ShutdownAsync().Wait();

            return response;
        }

        private async Task<SprocAddressStruct> GetSprocInstanceAsync(string messageSessionType, string messageUserName)
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
                service = null;
                // nothing -> exception mean no service with name
            }
            if (service == null)
            {

                StatelessServiceDescription newGeneratorDescription = new StatelessServiceDescription()
                {
                    ApplicationName = new Uri(this.Context.CodePackageActivationContext.ApplicationName),
                    ServiceName = url,
                    InstanceCount=1,
                    ServiceTypeName = "com.mega.SproGuestExeType",
                    PartitionSchemeDescription = new SingletonPartitionSchemeDescription()
                };

                await fabricClient.ServiceManager.CreateServiceAsync(newGeneratorDescription).ConfigureAwait(false);

                service = await fabricClient.ServiceManager.GetServiceDescriptionAsync(url);
            }

            var serviceName = service?.ServiceName;
            if (serviceName != null)
            {
                return new SprocAddressStruct { Ip = serviceName.Host, Port = serviceName.Port };
            }

            ServiceEventSource.Current.ServiceMessage(this.Context, $"Unable to instantiate service {0}", urlPath);
            throw new Exception(string.Format($"Unable to instantiate service {0}", urlPath));
        }
    }
}
