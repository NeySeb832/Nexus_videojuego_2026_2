using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Salida del nivel (ascensor, escaleras o salida de emergencia). Solo funciona cuando el
    /// jugador ya extrajo el Disco Duro Maestro de este nivel.
    /// </summary>
    public class SalidaNivel : Interactuable
    {
        [SerializeField] string mensajeSinDisco = "El ascensor no responde. Primero extrae el Disco Duro Maestro de este nivel.";

        void Reset() => textoAccion = "Usar el ascensor";

        public void Configurar(string accion, string sinDisco)
        {
            textoAccion = accion; mensajeSinDisco = sinDisco;
        }

        public override void Interactuar(InteraccionJugador jugador)
        {
            var nivel = NivelNexus.Instancia;
            if (nivel == null) return;
            if (!nivel.DiscoObtenido)
            {
                HUDNexus.Aviso(mensajeSinDisco);
                return;
            }
            if (JefeSentinel.Instancia != null && !JefeSentinel.Derrotado)
            {
                HUDNexus.Aviso("BLOQUEADA. La unidad SENTINEL-Ω mantiene el bloqueo de CERBERUS: destrúyela.");
                return;
            }
            jugador.GetComponent<ControladorJugador>().PuedeMoverse = false;
            nivel.CompletarNivel(transform);   // con el ascensor: transición animada al nivel siguiente
        }
    }
}
