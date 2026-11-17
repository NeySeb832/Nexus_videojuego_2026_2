using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Datos de un cadáver ya posado (los escribe el constructor): su caja en el suelo y los huesos
    /// donde se colocan los objetos que lleva (el arma en la mano, la munición junto a la cadera).
    /// </summary>
    public class MarcadorCadaver : MonoBehaviour
    {
        [SerializeField] Vector3 min, max;
        [SerializeField] Transform manoDerecha, cadera;

        public Vector3 Min => min;
        public Vector3 Max => max;
        public Transform ManoDerecha => manoDerecha;
        public Transform Cadera => cadera;

        public void Configurar(Vector3 minimo, Vector3 maximo, Transform mano, Transform hips)
        {
            min = transform.InverseTransformPoint(minimo); max = transform.InverseTransformPoint(maximo);
            manoDerecha = mano; cadera = hips;
        }
    }
}
