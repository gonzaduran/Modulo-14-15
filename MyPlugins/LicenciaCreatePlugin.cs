using System;
using Microsoft.Xrm.Sdk;
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
        private const string AttrNombre = "dtt_nombre";
        private const string AttrTipo = "dtt_tipo";
        private const string AttrContact = "dtt_contact";

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

                // Construir el nombre y asignarlo al Target.
                // Al estar en Pre-Operation, el valor se persiste automáticamente.
                licencia[AttrNombre] = PluginHelper.BuildLicenciaName(
                    service, contactRef, tipoValue, EntityName, AttrTipo);

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
    }
}
