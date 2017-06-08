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
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
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
                    var response = await CallserviceAsync(message);
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

        private async Task<string> CallserviceAsync(QueueMessage message)
        {
            var sprocInstance = await GetSprocInstanceAsync(message.SessionType, message.UserName);

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

            var service = await fabricClient.ServiceManager.GetServiceDescriptionAsync(url);

            if (service == null)
            {
                await fabricClient.ServiceManager.CreateServiceAsync(new StatefulServiceDescription
                {
                    ApplicationName = new Uri(this.Context.CodePackageActivationContext.ApplicationName),
                    ServiceName = url,
                    HasPersistedState = true,
                    ServiceTypeName = urlPath
                }).ConfigureAwait(false);

                service = await fabricClient.ServiceManager.GetServiceDescriptionAsync(url);
            }

            var serviceName = service?.ServiceName;

            if(serviceName != null)
                return new SprocAddressStruct { Ip = serviceName.Host, Port = serviceName.Port };

            ServiceEventSource.Current.ServiceMessage(this.Context, $"can not instanciate service {0}", urlPath);

            throw new Exception(string.Format($"can not instanciate service {0}", urlPath));
        }
    }
}
