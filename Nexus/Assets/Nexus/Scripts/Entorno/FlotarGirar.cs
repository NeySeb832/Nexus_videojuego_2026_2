using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Hace que un objeto gire sobre su eje vertical y flote suavemente arriba y abajo.
    /// Se usa en los Discos Duros Maestros y en las Tarjetas de Acceso para que destaquen.
    /// </summary>
    public class FlotarGirar : MonoBehaviour
    {
        [SerializeField] float gradosPorSegundo = 90f;
        [SerializeField] float alturaFlotacion = 0.06f;
        [SerializeField] float velocidadFlotacion = 2f;

        Vector3 posicionInicial;
        float desfase;

        void Start()
        {
            posicionInicial = transform.localPosition;
            desfase = Random.value * 10f; // para que dos objetos iguales no se muevan sincronizados
        }

        void Update()
        {
            transform.Rotate(0f, gradosPorSegundo * Time.deltaTime, 0f, Space.World);
            Vector3 p = posicionInicial;
            p.y += Mathf.Sin((Time.time + desfase) * velocidadFlotacion) * alturaFlotacion;
            transform.localPosition = p;
        }
    }
}
