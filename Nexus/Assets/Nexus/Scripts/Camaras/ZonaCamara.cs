using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Caja invisible (Box Collider en modo Trigger) en el suelo de un pasillo.
    /// Cuando el jugador la pisa, el <see cref="GestorCamaras"/> apaga la cámara anterior
    /// y enciende la de este pasillo.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ZonaCamara : MonoBehaviour
    {
        [SerializeField] Camera camara;
        [Tooltip("Sala a la que pertenece esta celda; el gestor elige el mejor ángulo de esa sala")]
        [SerializeField] string sala;

        public Camera Camara
        {
            get => camara;
            set => camara = value;
        }

        public string Sala
        {
            get => sala;
            set => sala = value;
        }

        void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider otro)
        {
            if (otro.CompareTag("Player") && GestorCamaras.Instancia != null)
                GestorCamaras.Instancia.EntrarSala(sala, camara);
        }

        void OnDrawGizmos()
        {
            var caja = GetComponent<BoxCollider>();
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.12f);
            Gizmos.DrawCube(caja.center, caja.size);
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(caja.center, caja.size);
            if (camara != null)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawLine(transform.TransformPoint(caja.center), camara.transform.position);
            }
        }
    }
}
