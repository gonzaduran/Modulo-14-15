using System;
using Microsoft.Xrm.Sdk;
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
    ///   Filtering Attr. : dtt_tipo
    ///   Pre-Image       : Nombre = "PreImage", Atributos = dtt_contact, dtt_tipo
    /// </summary>
    public class LicenciaUpdateTipoPlugin : IPlugin
    {
        private const string EntityName = "dtt_licencia";
        private const string AttrNombre = "dtt_nombre";
        private const string AttrTipo = "dtt_tipo";
        private const string AttrContact = "dtt_contact";
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

                // Recalcular el nombre
                target[AttrNombre] = PluginHelper.BuildLicenciaName(
                    service, contactRef, tipoValue, EntityName, AttrTipo);

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
    }
}
