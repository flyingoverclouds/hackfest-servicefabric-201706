using System;
using System.Fabric;

namespace com.mega.generator
{
    public class ServiceUriBuilder
    {
        public string ApplicationInstance { get; set; }
        public string ServiceInstance { get; set; }
        public ICodePackageActivationContext ActivationContext { get; set; }


        public ServiceUriBuilder(string serviceInstance)
        {
            ActivationContext = FabricRuntime.GetActivationContext();
            ServiceInstance = serviceInstance;
        }

        public ServiceUriBuilder(ICodePackageActivationContext context, string serviceInstance)
        {
            ActivationContext = context;
            ServiceInstance = serviceInstance;
        }

        public ServiceUriBuilder(ICodePackageActivationContext context, string applicationInstance, string serviceInstance)
        {
            ActivationContext = context;
            ApplicationInstance = applicationInstance;
            ServiceInstance = serviceInstance;
        }

        public Uri ToUri()
        {
            string applicationInstance = ApplicationInstance;

            if (String.IsNullOrEmpty(applicationInstance))
            {
                // the ApplicationName property here automatically prepends "fabric:/" for us
                applicationInstance = ActivationContext.ApplicationName.Replace("fabric:/", String.Empty);
            }

            return new Uri("fabric:/" + applicationInstance + "/" + ServiceInstance);
        }
    }
}
