using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace MyPlugins
{
    public class LicenciaUpdateFechasPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Extract the tracing service for use in debugging sandboxed plug-ins.  
            // If you are not registering the plug-in in the sandbox, then you do  
            // not have to add any tracing service related code.  
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));
          
            // Obtain the organization service reference which you will need for  
            // web service calls.  
            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);



            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];

                try
                {
                    // Solo validar si se está modificando alguna de las fechas
                    if (!entity.Contains("dtt_FechaInicio") && !entity.Contains("dtt_FechaFin"))
                        return;

                    // Obtener Pre-Image o recuperar el registro completo para las fechas
                    // que no estén en el Target
                    Entity preImage = null;
                    if (context.PreEntityImages.Contains("PreImage"))
                    {
                        preImage = context.PreEntityImages["PreImage"];
                    }

                    // Si no hay Pre-Image y falta alguna fecha en el Target,
                    // recuperamos el registro directamente desde CRM
                    if (preImage == null &&
                        (!entity.Contains("dtt_FechaInicio") || !entity.Contains("dtt_FechaFin")))
                    {
                        preImage = service.Retrieve("dtt_licencia", entity.Id,
                            new Microsoft.Xrm.Sdk.Query.ColumnSet("dtt_FechaInicio", "dtt_FechaFin"));
                    }

                    // Fecha Inicio: primero del Target, si no de la Pre-Image/Retrieve
                    DateTime? fechaInicio = entity.Contains("dtt_FechaInicio")
                        ? entity.GetAttributeValue<DateTime?>("dtt_FechaInicio")
                        : (preImage != null ? preImage.GetAttributeValue<DateTime?>("dtt_FechaInicio") : null);

                    // Fecha Fin: primero del Target, si no de la Pre-Image/Retrieve
                    DateTime? fechaFin = entity.Contains("dtt_FechaFin")
                        ? entity.GetAttributeValue<DateTime?>("dtt_FechaFin")
                        : (preImage != null ? preImage.GetAttributeValue<DateTime?>("dtt_FechaFin") : null);

                    if (!fechaInicio.HasValue || !fechaFin.HasValue)
                        return;

                    // Calcular diferencia en años
                    double diffYears = (fechaFin.Value - fechaInicio.Value).TotalDays / 365.25;

                    if (diffYears > 5)
                    {
                        throw new InvalidPluginExecutionException(
                            "Error: La diferencia entre Fecha Inicio y Fecha Fin no puede superar los 5 años. " +
                            "Diferencia actual: " + diffYears.ToString("F1") + " años.");
                    }
                }

                catch (InvalidPluginExecutionException)
                {
                    throw;
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in MyPlug-in.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("MyPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }
    }
}
