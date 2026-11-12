using System;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Inventario del jugador. Los datos viven en <see cref="EstadoPartida"/> para que el inventario
    /// se conserve al pasar de un nivel a otro.
    /// </summary>
    public class InventarioJugador : MonoBehaviour
    {
        /// <summary>Se dispara cada vez que cambia la cantidad de un tipo de objeto.</summary>
        public event Action<TipoColeccionable, int> AlCambiar;

        public int Cantidad(TipoColeccionable tipo) => EstadoPartida.Cantidad(tipo);

        public bool Tiene(TipoColeccionable tipo) => Cantidad(tipo) > 0;

        public void Agregar(TipoColeccionable tipo, int cantidad = 1)
        {
            EstadoPartida.Inventario[tipo] = Cantidad(tipo) + cantidad;
            AlCambiar?.Invoke(tipo, EstadoPartida.Inventario[tipo]);
            EstadoPartida.Notificar();
        }

        public bool Consumir(TipoColeccionable tipo, int cantidad = 1)
        {
            if (Cantidad(tipo) < cantidad) return false;
            EstadoPartida.Inventario[tipo] = Cantidad(tipo) - cantidad;
            AlCambiar?.Invoke(tipo, EstadoPartida.Inventario[tipo]);
            EstadoPartida.Notificar();
            return true;
        }
    }
}
