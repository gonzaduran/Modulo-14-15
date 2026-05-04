using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.ServiceModel;

namespace MyPlugins
{
    /// <summary>
    /// Plugin que se ejecuta en la CREACIÓN de una Licencia (Pre-Operation).
    /// Construye el campo Nombre con el formato: "NombreContacto - Tipo".
    ///
    /// Registro del plugin:
    ///   Mensaje  : Create
    ///   Entidad  : dtt_licencia
    ///   Fase     : Pre-Operation
    ///   Modo     : Síncrono
    /// </summary>
    public class LicenciaCreatePlugin : IPlugin
    {
        private const string EntityName = "dtt_licencia";
        private const string AttrNombre = "dtt_Nombre";
        private const string AttrTipo = "dtt_Tipo";
        private const string AttrContact = "dtt_Contact";

        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracing =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.MessageName != "Create")
                return;

            if (!context.InputParameters.Contains("Target") ||
                !(context.InputParameters["Target"] is Entity))
                return;

            Entity licencia = (Entity)context.InputParameters["Target"];

            IOrganizationServiceFactory factory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            try
            {
                tracing.Trace("LicenciaCreatePlugin: inicio de ejecución.");

                // Obtener el Contacto (lookup → EntityReference)
                var contactRef = licencia.GetAttributeValue<EntityReference>(AttrContact);
                if (contactRef == null)
                {
                    tracing.Trace("LicenciaCreatePlugin: no se encontró contacto, se omite.");
                    return;
                }

                // Obtener el valor numérico del OptionSet Tipo
                var tipoValue = licencia.GetAttributeValue<OptionSetValue>(AttrTipo);
                if (tipoValue == null)
                {
                    tracing.Trace("LicenciaCreatePlugin: no se encontró tipo, se omite.");
                    return;
                }

                // Obtener nombre del contacto desde CRM
                string contactName = GetContactName(service, contactRef);

                // Obtener etiqueta del OptionSet desde los metadatos
                string tipoLabel = GetOptionSetLabel(service, tipoValue.Value);

                // Construir el nombre y asignarlo al Target.
                // Al estar en Pre-Operation, el valor se persiste automáticamente.
                licencia[AttrNombre] = $"{contactName} - {tipoLabel}";

                tracing.Trace("LicenciaCreatePlugin: nombre asignado correctamente.");
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                throw new InvalidPluginExecutionException(
                    "Error en LicenciaCreatePlugin: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                tracing.Trace("LicenciaCreatePlugin: {0}", ex.ToString());
                throw;
            }
        }

        private string GetContactName(IOrganizationService service, EntityReference contactRef)
        {
            Entity contact = service.Retrieve(
                "contact", contactRef.Id, new ColumnSet("fullname"));
            return contact.GetAttributeValue<string>("fullname") ?? "Sin contacto";
        }

        private string GetOptionSetLabel(IOrganizationService service, int optionValue)
        {
            var request = new RetrieveAttributeRequest
            {
                EntityLogicalName = EntityName,
                LogicalName = AttrTipo,
                RetrieveAsIfPublished = true
            };

            var response = (RetrieveAttributeResponse)service.Execute(request);
            var metadata = (PicklistAttributeMetadata)response.AttributeMetadata;

            var option = metadata.OptionSet.Options
                .FirstOrDefault(o => o.Value == optionValue);

            return option?.Label?.UserLocalizedLabel?.Label
                   ?? optionValue.ToString();
        }
    }
}
