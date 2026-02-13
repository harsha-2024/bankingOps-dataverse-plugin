
using System;
using Microsoft.Xrm.Sdk;

namespace BankingOps.Plugin
{
    /// <summary>
    /// Simple base class to centralize context/service/tracing initialization.
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        protected readonly string UnsecureConfig;
        protected readonly string SecureConfig;

        protected PluginBase(string unsecureConfig, string secureConfig)
        {
            UnsecureConfig = unsecureConfig;
            SecureConfig = secureConfig;
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = factory.CreateOrganizationService(context.UserId);

            try
            {
                Execute(tracing, context, service, serviceProvider);
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                tracing.Trace($"Unhandled exception: {ex}");
                throw new InvalidPluginExecutionException("BankingOps plugin error. See Plugin Trace Log for details.", ex);
            }
        }

        protected abstract void Execute(ITracingService tracing, IPluginExecutionContext context, IOrganizationService service, IServiceProvider serviceProvider);
    }
}
