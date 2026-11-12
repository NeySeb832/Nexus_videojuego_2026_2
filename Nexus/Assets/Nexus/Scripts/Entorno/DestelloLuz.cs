using UnityEngine;

namespace Nexus
{
    /// <summary>Luz de un chispazo: se enciende de golpe y se desvanece en una fracción de segundo.</summary>
    [RequireComponent(typeof(Light))]
    public class DestelloLuz : MonoBehaviour
    {
        [SerializeField] float duracion = 0.25f;
        Light luz;
        float inicial, t;

        void Awake() { luz = GetComponent<Light>(); inicial = luz.intensity; }
        void OnEnable() { t = 0f; if (luz != null) luz.intensity = inicial; }

        void Update()
        {
            t += Time.deltaTime;
            luz.intensity = Mathf.Lerp(inicial, 0f, t / duracion);
            if (t >= duracion) luz.enabled = false;
        }
    }
}
