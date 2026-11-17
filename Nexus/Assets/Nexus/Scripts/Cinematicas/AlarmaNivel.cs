using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Alarma general del nivel (al extraer el Disco Maestro): todas las luces del edificio pasan a
    /// rojo y laten como una baliza. Las linternas de los androides y la del jugador no cambian.
    /// </summary>
    public class AlarmaNivel : MonoBehaviour
    {
        public static bool Activa { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() => Activa = false;

        readonly List<(Light luz, Color color, float intensidad, float fase)> luces = new List<(Light, Color, float, float)>();

        public static void Activar()
        {
            if (Activa) return;
            Activa = true;
            new GameObject("Alarma_Nivel").AddComponent<AlarmaNivel>();
        }

        void Start()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (l.type == LightType.Directional) continue;
                if (l.GetComponentInParent<AndroideIA>() != null || l.GetComponentInParent<ControladorJugador>() != null) continue;
                // Los fluorescentes que titilan dejan de hacerlo: ahora manda la alarma
                var parpadeo = l.GetComponent<LuzParpadeante>();
                if (parpadeo != null) parpadeo.enabled = false;
                luces.Add((l, l.color, l.intensity, (l.transform.position.x + l.transform.position.z) * 0.15f));
            }
        }

        void OnDestroy() { if (Activa) Activa = false; }

        void Update()
        {
            foreach (var (luz, color, intensidad, fase) in luces)
            {
                if (luz == null) continue;
                float pulso = 0.5f + 0.5f * Mathf.Sin((Time.time * 4.2f) + fase);
                luz.color = Color.Lerp(color, new Color(1f, 0.08f, 0.05f), 0.85f);
                luz.intensity = intensidad * Mathf.Lerp(0.25f, 1.15f, pulso);
            }
        }
    }
}
