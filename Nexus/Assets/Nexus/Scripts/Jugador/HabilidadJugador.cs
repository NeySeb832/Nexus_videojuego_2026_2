using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Nexus
{
    public enum TipoHabilidad { SenueloRed, Killswitch }

    /// <summary>
    /// Habilidad especial de cada técnico (tecla Q, botón Y/triángulo del mando o botón táctil).
    /// Le da a cada personaje una forma distinta de resolver los encuentros con los androides:
    ///
    ///  - Neyder (técnico de redes) — SEÑUELO DE RED: lanza un repetidor de red que emite una
    ///    señal falsa. Los androides cercanos van a revisarlo y se quedan ahí unos segundos.
    ///    Sirve para despejar un pasillo o escapar, pero si te ven por el camino te persiguen igual.
    ///
    ///  - Deisy (analista de sistemas) — KILLSWITCH.exe: ejecuta un programa de apagado sobre el
    ///    androide más cercano que tenga a la vista (máx. 7 m). Queda desconectado 10 s y luego
    ///    se reinicia. Es seguro, pero solo afecta a uno y obliga a acercarse.
    ///
    /// Las dos tienen tiempo de recarga y se muestran en el HUD.
    /// </summary>
    public class HabilidadJugador : MonoBehaviour
    {
        [Header("KILLSWITCH (Deisy)")]
        [SerializeField] float alcanceKillswitch = 7f;
        [SerializeField] float duracionApagado = 10f;
        [SerializeField] float recargaKillswitch = 30f;
        [SerializeField] Material materialRayo;
        [SerializeField] GameObject efectoChispas;

        [Header("Señuelo de red (Neyder)")]
        [SerializeField] GameObject prefabSenuelo;
        [SerializeField] float distanciaLanzamiento = 4f;
        [SerializeField] float recargaSenuelo = 25f;

        public static readonly string[] Nombres = { "SEÑUELO DE RED", "KILLSWITCH.exe" };
        public static readonly string[] Descripciones =
        {
            "Lanza un repetidor que emite una señal falsa: los androides cercanos van a revisarlo.",
            "Apaga 10 s al androide más cercano que tengas a la vista (máx. 7 m)."
        };

        const int MascaraSolidos = ~(1 << 2);

        VidaJugador vida;
        LineRenderer rayo;
        float listaEn;
        int ultimoSegundoMostrado = -1;

        public TipoHabilidad Tipo { get; private set; }
        public string Nombre => Nombres[(int)Tipo];
        public float Recarga => Tipo == TipoHabilidad.Killswitch ? recargaKillswitch : recargaSenuelo;
        public float RestanteRecarga => Mathf.Max(0f, listaEn - Time.time);
        public bool Lista => RestanteRecarga <= 0f;

        /// <summary>Permite activar la habilidad desde un botón táctil o una prueba automática.</summary>
        public bool PulsacionSimulada { get; set; }

        void Awake() => vida = GetComponent<VidaJugador>();

        IEnumerator Start()
        {
            Tipo = EstadoPartida.PersonajeElegido == 1 ? TipoHabilidad.Killswitch : TipoHabilidad.SenueloRed;
            ActualizarHUD(true);
            yield return new WaitForSeconds(2.5f);
            HUDNexus.Aviso($"Habilidad disponible: {Nombre}  [Q]");
        }

        void Update()
        {
            var k = Keyboard.current;
            var g = Gamepad.current;
            bool pulsado = PulsacionSimulada || (k != null && k.qKey.wasPressedThisFrame) || (g != null && g.buttonNorth.wasPressedThisFrame);
            PulsacionSimulada = false;
            ActualizarHUD(false);

            if (!pulsado || Time.timeScale <= 0f || HUDNexus.DialogoAbierto || (vida != null && vida.Muerto)) return;
            if (!Lista)
            {
                HUDNexus.Aviso($"{Nombre}: recargando ({RestanteRecarga:0} s)");
                return;
            }
            bool usada = Tipo == TipoHabilidad.Killswitch ? UsarKillswitch() : UsarSenuelo();
            if (usada) listaEn = Time.time + Recarga;
        }

        void ActualizarHUD(bool forzar)
        {
            int s = Mathf.CeilToInt(RestanteRecarga);
            if (!forzar && s == ultimoSegundoMostrado) return;
            ultimoSegundoMostrado = s;
            HUDNexus.Habilidad(s <= 0 ? $"Q  ·  {Nombre}  ·  LISTA" : $"Q  ·  {Nombre}  ·  {s} s", s <= 0);
        }

        // ======================= KILLSWITCH =======================
        bool UsarKillswitch()
        {
            Vector3 origen = transform.position + Vector3.up * 1.3f;
            AndroideIA mejor = null;
            float mejorDist = alcanceKillswitch;
            foreach (var a in AndroideIA.Todos)
            {
                if (a == null || !a.PuedeApagarse) continue;
                float d = Vector3.Distance(transform.position, a.transform.position);
                if (d > mejorDist) continue;
                // Tiene que estar a la vista: nada sólido entre los dos
                Vector3 destino = a.transform.position + Vector3.up * 1.2f;
                Vector3 dir = destino - origen;
                if (Physics.Raycast(origen, dir.normalized, out RaycastHit hit, dir.magnitude, MascaraSolidos, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<AndroideIA>() != a) continue;
                mejor = a; mejorDist = d;
            }
            if (mejor == null)
            {
                HUDNexus.Aviso($"KILLSWITCH: ningún androide a la vista (máx. {alcanceKillswitch:0} m)");
                return false;
            }
            mejor.Apagar(duracionApagado);
            StartCoroutine(Rayo(mejor.transform));
            HUDNexus.Aviso($"KILLSWITCH.exe ejecutado: SENTINEL fuera de línea {duracionApagado:0} s");
            return true;
        }

        /// <summary>Haz de datos cian que salta entre la tableta de Deisy y el androide.</summary>
        IEnumerator Rayo(Transform objetivo)
        {
            if (rayo == null)
            {
                rayo = new GameObject("Rayo_Killswitch").AddComponent<LineRenderer>();
                rayo.transform.SetParent(transform, false);
                rayo.sharedMaterial = materialRayo;
                rayo.positionCount = 8;
                rayo.widthMultiplier = 0.035f;
                rayo.numCapVertices = 2;
                rayo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            rayo.enabled = true;
            if (efectoChispas != null)
                Destroy(Instantiate(efectoChispas, objetivo.position + Vector3.up * 1.4f, Quaternion.identity), 3f);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                Vector3 a = transform.position + Vector3.up * 1.25f + transform.forward * 0.3f;
                Vector3 b = objetivo.position + Vector3.up * 1.45f;
                for (int i = 0; i < rayo.positionCount; i++)
                {
                    float u = i / (float)(rayo.positionCount - 1);
                    Vector3 ruido = (i == 0 || i == rayo.positionCount - 1) ? Vector3.zero : Random.insideUnitSphere * 0.12f;
                    rayo.SetPosition(i, Vector3.Lerp(a, b, u) + ruido);
                }
                rayo.widthMultiplier = 0.045f * (1f - t / 0.6f) + 0.01f;
                yield return null;
            }
            rayo.enabled = false;
        }

        // ======================= SEÑUELO DE RED =======================
        bool UsarSenuelo()
        {
            if (prefabSenuelo == null) return false;
            Vector3 desde = transform.position + Vector3.up * 1.1f + transform.forward * 0.3f;
            Vector3 dir = transform.forward;
            float distancia = distanciaLanzamiento;
            // No atraviesa paredes ni racks: cae justo antes del obstáculo
            if (Physics.SphereCast(transform.position + Vector3.up * 0.6f, 0.2f, dir, out RaycastHit hit, distancia, MascaraSolidos, QueryTriggerInteraction.Ignore))
                distancia = Mathf.Max(0.6f, hit.distance - 0.3f);
            Vector3 hasta = transform.position + dir * distancia;
            if (NavMesh.SamplePosition(hasta, out NavMeshHit nh, 1.5f, NavMesh.AllAreas)) hasta = nh.position;

            var s = Instantiate(prefabSenuelo, desde, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            var senuelo = s.GetComponent<SenueloRed>();
            if (senuelo != null) senuelo.Lanzar(desde, hasta);
            HUDNexus.Aviso("Señuelo de red desplegado: atrae a los androides cercanos");
            return true;
        }
    }
}
