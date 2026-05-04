using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace MyPlugins
{
    /// <summary>
    /// Métodos auxiliares reutilizados por los plugins de Licencia.
    /// </summary>
    public static class PluginHelper
    {
        /// <summary>
        /// Obtiene el texto (label) de un valor OptionSet consultando los metadatos.
        /// En Pre-Operation la colección FormattedValues no está disponible,
        /// por lo que es necesario recurrir a la metadata del atributo.
        /// </summary>
        public static string GetOptionSetLabel(
            IOrganizationService service,
            string entityLogicalName,
            string attributeLogicalName,
            int optionValue)
        {
            var request = new RetrieveAttributeRequest
            {
                EntityLogicalName = entityLogicalName,
                LogicalName = attributeLogicalName,
                RetrieveAsIfPublished = true
            };

            var response = (RetrieveAttributeResponse)service.Execute(request);
            var metadata = (PicklistAttributeMetadata)response.AttributeMetadata;

            var option = metadata.OptionSet.Options
                .FirstOrDefault(o => o.Value == optionValue);

            return option?.Label?.UserLocalizedLabel?.Label
                   ?? optionValue.ToString();
        }

        /// <summary>
        /// Recupera el nombre completo (fullname) de un contacto dado su Id.
        /// </summary>
        public static string GetContactName(
            IOrganizationService service,
            EntityReference contactRef)
        {
            Entity contact = service.Retrieve(
                "contact",
                contactRef.Id,
                new ColumnSet("fullname"));

            return contact.GetAttributeValue<string>("fullname")
                   ?? "Sin contacto";
        }

        /// <summary>
        /// Construye el nombre de la licencia: "NombreContacto - TextoTipo".
        /// </summary>
        public static string BuildLicenciaName(
            IOrganizationService service,
            EntityReference contactRef,
            OptionSetValue tipoValue,
            string entityLogicalName,
            string tipoAttributeName)
        {
            string contactName = GetContactName(service, contactRef);
            string tipoLabel = GetOptionSetLabel(
                service,
                entityLogicalName,
                tipoAttributeName,
                tipoValue.Value);

            return $"{contactName} - {tipoLabel}";
        }
    }
}
