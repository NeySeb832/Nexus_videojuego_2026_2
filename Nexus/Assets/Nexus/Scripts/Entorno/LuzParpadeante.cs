using System.Collections;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Lámpara fluorescente en mal estado: casi siempre encendida, pero de vez en cuando
    /// titila o se apaga un instante. Ambientación de lugar abandonado.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LuzParpadeante : MonoBehaviour
    {
        [SerializeField] float pausaMinima = 1.5f, pausaMaxima = 6f;
        [Tooltip("Brillo mínimo durante el parpadeo (0 = se apaga por completo)")]
        [SerializeField] float minimo = 0.05f;

        Light luz;
        float intensidadBase;

        void Awake()
        {
            luz = GetComponent<Light>();
            intensidadBase = luz.intensity;
        }

        void OnEnable() => StartCoroutine(Parpadear());

        IEnumerator Parpadear()
        {
            yield return new WaitForSeconds(Random.Range(0f, pausaMaxima));
            while (true)
            {
                int destellos = Random.Range(2, 7);
                for (int i = 0; i < destellos; i++)
                {
                    luz.intensity = intensidadBase * Random.Range(minimo, 0.35f);
                    yield return new WaitForSeconds(Random.Range(0.03f, 0.12f));
                    luz.intensity = intensidadBase * Random.Range(0.8f, 1.1f);
                    yield return new WaitForSeconds(Random.Range(0.02f, 0.15f));
                }
                luz.intensity = intensidadBase;
                yield return new WaitForSeconds(Random.Range(pausaMinima, pausaMaxima));
            }
        }
    }
}
