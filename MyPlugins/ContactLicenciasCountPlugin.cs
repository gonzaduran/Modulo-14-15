using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace MyPlugins
{
    public class ContactLicenciasCountPlugin : IPlugin
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
                    // Obtener el contacto actual (del Target o de la Pre-Image)
                    var contactRef = entity.GetAttributeValue<EntityReference>("dtt_Contact");
                    EntityReference previousContactRef = null;

                    if (context.PreEntityImages.Contains("PreImage"))
                    {
                        Entity preImage = context.PreEntityImages["PreImage"];
                        previousContactRef = preImage.GetAttributeValue<EntityReference>("dtt_Contact");
                    }

                    // Si no hay contacto en el Target, usar el de la Pre-Image
                    if (contactRef == null)
                    {
                        contactRef = previousContactRef;
                        previousContactRef = null;
                    }

                    // Actualizar el contacto actual
                    if (contactRef != null)
                    {
                        int count = CountLicencias(service, contactRef.Id);
                        Entity contactUpdate = new Entity("contact", contactRef.Id);
                        contactUpdate["dtt_numerolicencias"] = count;
                        service.Update(contactUpdate);
                    }

                    // Si el contacto cambió, actualizar también el contacto anterior
                    if (previousContactRef != null &&
                        (contactRef == null || previousContactRef.Id != contactRef.Id))
                    {
                        int countPrev = CountLicencias(service, previousContactRef.Id);
                        Entity contactUpdate = new Entity("contact", previousContactRef.Id);
                        contactUpdate["dtt_numerolicencias"] = countPrev;
                        service.Update(contactUpdate);
                    }
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

        /// <summary>
        /// Cuenta las licencias de un contacto usando FetchXML aggregate.
        /// </summary>
        private int CountLicencias(IOrganizationService service, Guid contactId)
        {
            string fetchXml =
                "<fetch aggregate='true'>" +
                "  <entity name='dtt_licencia'>" +
                "    <attribute name='dtt_Contact' aggregate='count' alias='liccount'/>" +
                "    <filter>" +
                "      <condition attribute='dtt_Contact' operator='eq' value='" + contactId.ToString() + "'/>" +
                "    </filter>" +
                "  </entity>" +
                "</fetch>";

            EntityCollection results = service.RetrieveMultiple(
                new Microsoft.Xrm.Sdk.Query.FetchExpression(fetchXml));

            int count = 0;
            if (results.Entities.Count > 0 && results.Entities[0].Contains("liccount"))
            {
                var aliasedValue = (AliasedValue)results.Entities[0]["liccount"];
                count = (int)aliasedValue.Value;
            }

            return count;
        }
    }
}
