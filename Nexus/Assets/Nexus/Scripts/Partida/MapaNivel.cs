using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Datos del mapa del nivel para la pestaña MAPA del inventario: la imagen esquemática
    /// (generada desde el mapa de texto), el tamaño del nivel en metros y los nombres de las salas.
    /// </summary>
    public class MapaNivel : MonoBehaviour
    {
        public Texture2D textura;
        [Tooltip("Ancho (X) y largo (Z) del nivel en metros; el origen es la esquina (0, 0)")]
        public Vector2 tamanoMundo;
        public string[] nombresSalas;
        public Vector2[] posicionesSalas;   // en metros (x, z)

        /// <summary>Convierte una posición del mundo a coordenadas normalizadas (0..1) del mapa.</summary>
        public Vector2 Normalizar(Vector3 mundo) =>
            new Vector2(Mathf.Clamp01(mundo.x / tamanoMundo.x), Mathf.Clamp01(mundo.z / tamanoMundo.y));
    }
}
