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
    /// Plugin que se ejecuta en la MODIFICACIÓN de una Licencia (Pre-Operation)
    /// cuando se cambia el campo Tipo. Recalcula el Nombre.
    ///
    /// Registro del plugin:
    ///   Mensaje         : Update
    ///   Entidad         : dtt_licencia
    ///   Fase            : Pre-Operation
    ///   Modo            : Síncrono
    ///   Filtering Attr. : dtt_Tipo
    ///   Pre-Image       : Nombre = "PreImage", Atributos = dtt_Contact, dtt_Tipo
    /// </summary>
    public class LicenciaUpdateTipoPlugin : IPlugin
    {
        private const string EntityName = "dtt_licencia";
        private const string AttrNombre = "dtt_Nombre";
        private const string AttrTipo = "dtt_Tipo";
        private const string AttrContact = "dtt_Contact";
        private const string PreImageAlias = "PreImage";

        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracing =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.MessageName != "Update")
                return;

            if (!context.InputParameters.Contains("Target") ||
                !(context.InputParameters["Target"] is Entity))
                return;

            Entity target = (Entity)context.InputParameters["Target"];

            // Solo actuar si se está modificando el campo Tipo
            if (!target.Contains(AttrTipo))
                return;

            IOrganizationServiceFactory factory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            try
            {
                tracing.Trace("LicenciaUpdateTipoPlugin: inicio de ejecución.");

                // Obtener el nuevo valor de Tipo desde el Target
                var tipoValue = target.GetAttributeValue<OptionSetValue>(AttrTipo);
                if (tipoValue == null)
                {
                    tracing.Trace("LicenciaUpdateTipoPlugin: tipo es null, se omite.");
                    return;
                }

                // Obtener el Contacto.
                // Primero intentar desde el Target; si no está, usar la Pre-Image.
                var contactRef = target.GetAttributeValue<EntityReference>(AttrContact);

                if (contactRef == null &&
                    context.PreEntityImages.Contains(PreImageAlias))
                {
                    Entity preImage = context.PreEntityImages[PreImageAlias];
                    contactRef = preImage.GetAttributeValue<EntityReference>(AttrContact);
                }

                if (contactRef == null)
                {
                    tracing.Trace("LicenciaUpdateTipoPlugin: no se encontró contacto.");
                    return;
                }

                // Obtener nombre del contacto y etiqueta del tipo
                string contactName = GetContactName(service, contactRef);
                string tipoLabel = GetOptionSetLabel(service, tipoValue.Value);

                // Recalcular el nombre
                target[AttrNombre] = $"{contactName} - {tipoLabel}";

                tracing.Trace("LicenciaUpdateTipoPlugin: nombre actualizado correctamente.");
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                throw new InvalidPluginExecutionException(
                    "Error en LicenciaUpdateTipoPlugin: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                tracing.Trace("LicenciaUpdateTipoPlugin: {0}", ex.ToString());
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
