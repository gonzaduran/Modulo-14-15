using System;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace MyPlugins
{
    /// <summary>
    /// Plugin que se ejecuta en el UPDATE de una Licencia (Pre-Operation).
    /// Valida que la diferencia entre Fecha Fin y Fecha Inicio no supere 5 años.
    /// Si la supera, interrumpe el guardado lanzando una excepción.
    ///
    /// Registro del plugin:
    ///   Mensaje         : Update
    ///   Entidad         : dtt_licencia
    ///   Fase            : Pre-Operation (o Pre-Validation)
    ///   Modo            : Síncrono
    ///   Filtering Attr. : dtt_FechaInicio, dtt_FechaFin
    ///   Pre-Image       : Nombre = "PreImage", Atributos = dtt_FechaInicio, dtt_FechaFin
    /// </summary>
    public class LicenciaUpdateFechasPlugin : IPlugin
    {
        private const string AttrFechaInicio = "dtt_FechaInicio";
        private const string AttrFechaFin = "dtt_FechaFin";
        private const string PreImageAlias = "PreImage";
        private const int MaxYears = 5;

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

            // Solo validar si se está modificando alguna de las fechas
            if (!target.Contains(AttrFechaInicio) && !target.Contains(AttrFechaFin))
                return;

            try
            {
                tracing.Trace("LicenciaUpdateFechasPlugin: inicio de ejecución.");

                // Obtener Pre-Image para los valores que no estén en el Target
                Entity preImage = null;
                if (context.PreEntityImages.Contains(PreImageAlias))
                {
                    preImage = context.PreEntityImages[PreImageAlias];
                }

                // Fecha Inicio: primero del Target, si no de la Pre-Image
                DateTime? fechaInicio = target.Contains(AttrFechaInicio)
                    ? target.GetAttributeValue<DateTime?>(AttrFechaInicio)
                    : preImage?.GetAttributeValue<DateTime?>(AttrFechaInicio);

                // Fecha Fin: primero del Target, si no de la Pre-Image
                DateTime? fechaFin = target.Contains(AttrFechaFin)
                    ? target.GetAttributeValue<DateTime?>(AttrFechaFin)
                    : preImage?.GetAttributeValue<DateTime?>(AttrFechaFin);

                if (!fechaInicio.HasValue || !fechaFin.HasValue)
                {
                    tracing.Trace("LicenciaUpdateFechasPlugin: alguna fecha es null, se omite la validación.");
                    return;
                }

                // Calcular diferencia en años
                double diffYears = (fechaFin.Value - fechaInicio.Value).TotalDays / 365.25;

                tracing.Trace(
                    "LicenciaUpdateFechasPlugin: Inicio={0}, Fin={1}, Diferencia={2:F2} años.",
                    fechaInicio.Value, fechaFin.Value, diffYears);

                if (diffYears > MaxYears)
                {
                    throw new InvalidPluginExecutionException(
                        $"Error: La diferencia entre Fecha Inicio y Fecha Fin no puede superar los {MaxYears} años. " +
                        $"Diferencia actual: {diffYears:F1} años.");
                }

                tracing.Trace("LicenciaUpdateFechasPlugin: validación superada.");
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                throw new InvalidPluginExecutionException(
                    "Error en LicenciaUpdateFechasPlugin: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                tracing.Trace("LicenciaUpdateFechasPlugin: {0}", ex.ToString());
                throw;
            }
        }
    }
}
