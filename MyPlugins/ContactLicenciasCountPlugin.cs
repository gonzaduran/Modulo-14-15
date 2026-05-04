using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace MyPlugins
{
    /// <summary>
    /// Plugin que calcula el número de licencias asociadas a un contacto
    /// y actualiza el campo dtt_numerolicencias en el contacto.
    ///
    /// Registro del plugin:
    ///   Mensaje         : Create y Update
    ///   Entidad         : dtt_licencia
    ///   Fase            : Post-Operation
    ///   Modo            : Asíncrono
    ///   Pre-Image       : Nombre = "PreImage", Atributos = dtt_contact
    ///
    /// Notas:
    ///   - Se usa Pre-Image porque en un Update el campo dtt_contact podría
    ///     no estar incluido en los campos actualizados (Target).
    ///   - Si el contacto cambia (está en Target y en PreImage con valor distinto),
    ///     se recalculan ambos contactos (el anterior y el nuevo).
    /// </summary>
    public class ContactLicenciasCountPlugin : IPlugin
    {
        private const string EntityName = "dtt_licencia";
        private const string AttrContact = "dtt_contact";
        private const string AttrNumLicencias = "dtt_numerolicencias";
        private const string PreImageAlias = "PreImage";

        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracing =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.MessageName != "Create" && context.MessageName != "Update")
                return;

            if (!context.InputParameters.Contains("Target") ||
                !(context.InputParameters["Target"] is Entity))
                return;

            Entity target = (Entity)context.InputParameters["Target"];

            IOrganizationServiceFactory factory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            try
            {
                tracing.Trace("ContactLicenciasCountPlugin: inicio de ejecución ({0}).",
                    context.MessageName);

                // Obtener el contacto actual (del Target o de la Pre-Image)
                var contactRef = target.GetAttributeValue<EntityReference>(AttrContact);
                EntityReference previousContactRef = null;

                if (context.PreEntityImages.Contains(PreImageAlias))
                {
                    Entity preImage = context.PreEntityImages[PreImageAlias];
                    previousContactRef = preImage.GetAttributeValue<EntityReference>(AttrContact);
                }

                // Si no hay contacto en el Target, usar el de la Pre-Image
                if (contactRef == null)
                {
                    contactRef = previousContactRef;
                    previousContactRef = null; // No hay cambio de contacto
                }

                // Actualizar el contacto actual
                if (contactRef != null)
                {
                    int count = CountLicencias(service, contactRef.Id, tracing);
                    UpdateContactCount(service, contactRef.Id, count, tracing);
                }

                // Si el contacto cambió, actualizar también el contacto anterior
                if (previousContactRef != null &&
                    (contactRef == null || previousContactRef.Id != contactRef.Id))
                {
                    int count = CountLicencias(service, previousContactRef.Id, tracing);
                    UpdateContactCount(service, previousContactRef.Id, count, tracing);
                }

                tracing.Trace("ContactLicenciasCountPlugin: ejecución completada.");
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                throw new InvalidPluginExecutionException(
                    "Error en ContactLicenciasCountPlugin: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                tracing.Trace("ContactLicenciasCountPlugin: {0}", ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Cuenta todas las licencias asociadas a un contacto.
        /// </summary>
        private int CountLicencias(
            IOrganizationService service,
            Guid contactId,
            ITracingService tracing)
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(false) // No necesitamos columnas, solo contar
            };

            query.Criteria.AddCondition(AttrContact, ConditionOperator.Equal, contactId);

            EntityCollection results = service.RetrieveMultiple(query);
            int count = results.Entities.Count;

            tracing.Trace("ContactLicenciasCountPlugin: contacto {0} tiene {1} licencia(s).",
                contactId, count);

            return count;
        }

        /// <summary>
        /// Actualiza el campo Número de Licencias en el contacto.
        /// </summary>
        private void UpdateContactCount(
            IOrganizationService service,
            Guid contactId,
            int count,
            ITracingService tracing)
        {
            Entity contactUpdate = new Entity("contact", contactId);
            contactUpdate[AttrNumLicencias] = count;
            service.Update(contactUpdate);

            tracing.Trace("ContactLicenciasCountPlugin: contacto {0} actualizado con {1} licencia(s).",
                contactId, count);
        }
    }
}
