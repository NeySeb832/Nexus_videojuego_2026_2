using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Terminal de respaldo: guarda la partida consumiendo una memoria USB, como las máquinas de
    /// escribir de los survival horror clásicos. Sin USB no se puede guardar.
    /// </summary>
    public class TerminalGuardado : Interactuable
    {
        void Reset() => textoAccion = "Terminal de respaldo (guardar)";

        public override void Interactuar(InteraccionJugador jugador)
        {
            if (!jugador.Inventario.Tiene(TipoColeccionable.USB))
            {
                HUDNexus.MostrarDialogo("TERMINAL DE RESPALDO", new[]
                {
                    "Inserte una memoria USB de respaldo para guardar el estado del sistema.",
                    "(Necesitas una memoria USB para guardar la partida.)"
                }, null);
                return;
            }
            jugador.Inventario.Consumir(TipoColeccionable.USB);
            var nivel = NivelNexus.Instancia;
            EstadoPartida.Guardar(gameObject.scene.name, jugador.transform.position, nivel != null ? nivel.TiempoRestante : 0f);
            HUDNexus.MostrarDialogo("TERMINAL DE RESPALDO", new[]
            {
                $"Respaldo completado. Partida guardada. (Memorias USB restantes: {jugador.Inventario.Cantidad(TipoColeccionable.USB)})"
            }, CompletarObjetivo);
        }
    }
}
