using System.Collections;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Repetidor de red que lanza Neyder. Vuela en arco hasta el suelo, se despliega y durante unos
    /// segundos emite una señal falsa (anillo de pulso, luz y LED parpadeando). Cada medio segundo
    /// llama a los androides que estén dentro del radio. Al terminar se sobrecarga y se apaga.
    /// </summary>
    public class SenueloRed : MonoBehaviour
    {
        [SerializeField] float duracion = 8f;
        [SerializeField] float radio = 14f;
        [SerializeField] Light luz;
        [SerializeField] Transform anillo;
        [SerializeField] GameObject efectoFin;

        Vector3 desde, hasta;
        bool lanzado;

        public void Lanzar(Vector3 origen, Vector3 destino)
        {
            desde = origen; hasta = destino; lanzado = true;
        }

        IEnumerator Start()
        {
            if (anillo != null) anillo.gameObject.SetActive(false);
            if (luz != null) luz.enabled = false;

            // Vuelo en arco (0,45 s)
            if (lanzado)
            {
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f)
                {
                    transform.position = Vector3.Lerp(desde, hasta, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.8f;
                    transform.Rotate(0f, 540f * Time.deltaTime, 0f);
                    yield return null;
                }
                transform.position = hasta;
            }

            // Emisión de la señal falsa
            if (anillo != null) anillo.gameObject.SetActive(true);
            if (luz != null) luz.enabled = true;
            float fin = Time.time + duracion, proximoLlamado = 0f;
            while (Time.time < fin)
            {
                if (Time.time >= proximoLlamado)
                {
                    proximoLlamado = Time.time + 0.5f;
                    foreach (var a in AndroideIA.Todos)
                        if (a != null && (a.transform.position - transform.position).sqrMagnitude < radio * radio)
                            a.Atraer(transform.position, fin - Time.time + 1.5f);
                }
                float fase = Mathf.Repeat(Time.time, 1f);
                if (anillo != null)
                {
                    float s = Mathf.Lerp(0.2f, 3.2f, fase);
                    anillo.localScale = new Vector3(s, 1f, s);
                }
                if (luz != null) luz.intensity = fase < 0.15f ? 6f : 1.5f;
                yield return null;
            }

            // Sobrecarga: chispazo y se apaga
            if (efectoFin != null) Destroy(Instantiate(efectoFin, transform.position + Vector3.up * 0.2f, Quaternion.identity), 3f);
            if (anillo != null) anillo.gameObject.SetActive(false);
            if (luz != null) luz.enabled = false;
            yield return new WaitForSeconds(4f);
            Destroy(gameObject);
        }
    }
}
