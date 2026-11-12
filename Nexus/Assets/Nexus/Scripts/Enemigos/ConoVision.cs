using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Cono invisible frente al androide (Mesh Collider convexo en modo Trigger).
    /// Avisa al androide cuando el jugador entra o sale del cono; el androide confirma después
    /// con un rayo que no haya un rack o una pared en medio (línea de visión).
    /// </summary>
    [RequireComponent(typeof(MeshCollider))]
    public class ConoVision : MonoBehaviour
    {
        [SerializeField] AndroideIA androide;

        public AndroideIA Androide { get => androide; set => androide = value; }

        void OnTriggerEnter(Collider otro)
        {
            if (otro.CompareTag("Player")) androide.JugadorEnCono = true;
        }

        void OnTriggerExit(Collider otro)
        {
            if (otro.CompareTag("Player")) androide.JugadorEnCono = false;
        }
    }
}
