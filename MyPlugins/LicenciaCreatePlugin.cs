using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace MyPlugins
{
    public class LicenciaCreatePlugin : IPlugin
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
                    // Obtener el Contacto (lookup → EntityReference)
                    var contactRef = entity.GetAttributeValue<EntityReference>("dtt_Contact");
                    if (contactRef == null)
                        return;

                    // Obtener el nombre del contacto.
                    // Si contactRef.Name es null (puede pasar en Pre-Operation),
                    // lo recuperamos directamente desde CRM.
                    string contactName = contactRef.Name;
                    if (string.IsNullOrEmpty(contactName))
                    {
                        Entity contact = service.Retrieve("contact", contactRef.Id,
                            new Microsoft.Xrm.Sdk.Query.ColumnSet("fullname"));
                        contactName = contact.GetAttributeValue<string>("fullname") ?? "Sin contacto";
                    }

                    // Obtener el texto del campo Tipo (OptionSet) desde FormattedValues
                    if (!entity.FormattedValues.Contains("dtt_Tipo"))
                        return;

                    string tipoTexto = entity.FormattedValues["dtt_Tipo"];

                    // Construir el nombre: "NombreContacto - Tipo"
                    // Al estar en Pre-Operation, el valor se persiste automáticamente.
                    entity["dtt_Nombre"] = contactName + " - " + tipoTexto;
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
