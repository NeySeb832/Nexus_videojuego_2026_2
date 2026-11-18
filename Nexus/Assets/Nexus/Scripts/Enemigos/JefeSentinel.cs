using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    /// <summary>
    /// Jefe final: SENTINEL-Ω, la unidad prototipo donde CERBERUS se descarga al perder el último
    /// disco. Usa la IA de los androides (persigue y golpea) y le agrega un ciclo de combate:
    ///  1. BLINDADO: persigue al técnico; las balas le hacen la mitad de daño.
    ///  2. CARGA (2,2 s): se detiene y su núcleo brilla cada vez más. Pierde el blindaje (daño normal).
    ///     Al terminar suelta una descarga que hiere a todo lo que esté cerca.
    ///  3. SOBRECALENTADO (2,5 s): EXPUESTO (doble daño), quieto y casi apagado. Es el momento de atacar,
    ///     incluso con el cuchillo.
    /// A la mitad de su vida llama a las unidades de reserva y carga más seguido. Al destruirlo,
    /// CERBERUS pierde el control: todas las unidades se desconectan y la salida se desbloquea.
    /// </summary>
    [RequireComponent(typeof(AndroideIA))]
    public class JefeSentinel : MonoBehaviour
    {
        public static JefeSentinel Instancia { get; private set; }
        public static bool Derrotado { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { Instancia = null; Derrotado = false; }

        [SerializeField] string nombre = "SENTINEL-Ω";
        [SerializeField] List<AndroideIA> refuerzos = new List<AndroideIA>();
        [SerializeField] float radioDescarga = 6.5f;
        [SerializeField] float segundosEntreCargas = 8f;
        [SerializeField] string objetivoAlDerrotar = "Fuerza el reinicio en la salida de emergencia (sur)";

        AndroideIA ia;
        Transform jugador;
        VidaJugador vida;
        float proximaCarga;
        bool cargando, faseDos, enCombate;
        Light luzNucleo;
        TMP_Text barra;
        Canvas lienzo;

        public bool Cargando => cargando;

        public void Configurar(string nombreJefe, List<AndroideIA> reserva, string objetivo)
        {
            nombre = nombreJefe; refuerzos = reserva; objetivoAlDerrotar = objetivo;
        }

        void Awake()
        {
            Instancia = this;
            Derrotado = false;
            ia = GetComponent<AndroideIA>();
            luzNucleo = new GameObject("Luz_Nucleo_Jefe").AddComponent<Light>();
            luzNucleo.transform.SetParent(transform, false);
            luzNucleo.transform.localPosition = new Vector3(0f, 1.4f, 0.5f);
            luzNucleo.type = LightType.Point; luzNucleo.color = new Color(1f, 0.6f, 0.15f);
            luzNucleo.range = 7f; luzNucleo.intensity = 0f; luzNucleo.shadows = LightShadows.None;
        }

        void OnDestroy() { if (Instancia == this) Instancia = null; }

        void Start()
        {
            var j = GameObject.FindGameObjectWithTag("Player");
            if (j != null) { jugador = j.transform; vida = j.GetComponent<VidaJugador>(); }
        }

        void Update()
        {
            if (Derrotado) return;
            if (ia.Destruido) { Morir(); return; }
            bool activo = ia.EstadoActual != AndroideIA.Estado.Inactivo && ia.EstadoActual != AndroideIA.Estado.Apagado;
            if (!activo || AndroideIA.Pausados || Cinematica.EnCurso || jugador == null)
            {
                if (lienzo != null) lienzo.enabled = false;
                return;
            }
            if (!enCombate)
            {
                enCombate = true;
                ia.MultiplicadorDanio = 0.5f;
                proximaCarga = Time.time + 4f;
                HUDNexus.Aviso($"{nombre}: blindaje activo. Espera a que cargue su descarga para dañarlo.");
            }
            MostrarBarra();

            if (!faseDos && ia.Salud <= ia.SaludMaxima * 0.5f)
            {
                faseDos = true;
                foreach (var r in refuerzos) if (r != null && !r.Destruido) r.Activar(20f);
                HUDNexus.Aviso($"{nombre}: «Unidades de reserva, a mí.»");
            }
            if (!cargando && Time.time >= proximaCarga) StartCoroutine(Descarga());
        }

        IEnumerator Descarga()
        {
            cargando = true;
            ia.Detenido = true;
            ia.MultiplicadorDanio = 1f;     // mientras carga pierde el blindaje (daño normal), pero acercarse es peligroso
            HUDNexus.Aviso($"¡{nombre} carga una descarga! ¡Aléjate! Al descargar quedará expuesto.");
            for (float t = 0f; t < 2.2f; t += Time.deltaTime)
            {
                if (ia.Destruido) yield break;
                float k = t / 2.2f;
                ia.Destello(1f + 4f * k * (0.6f + 0.4f * Mathf.Sin(t * 30f)));
                luzNucleo.intensity = Mathf.Lerp(2f, 14f, k);
                yield return null;
            }
            // La descarga: onda expansiva que hiere al técnico si está cerca
            StartCoroutine(Onda());
            if (vida != null && Vector3.Distance(jugador.position, transform.position) < radioDescarga)
                vida.RecibirDanio(transform.position);
            // Sobrecalentado: quieto y expuesto (doble daño) — el momento de castigarlo
            ia.MultiplicadorDanio = 2f;
            for (float t = 0f; t < 2.5f; t += Time.deltaTime)
            {
                if (ia.Destruido) yield break;
                ia.Destello(0.15f);
                luzNucleo.intensity = Mathf.Lerp(14f, 0f, t / 0.4f);
                yield return null;
            }
            ia.Destello(1f);
            ia.Detenido = false;
            ia.MultiplicadorDanio = 0.5f;
            proximaCarga = Time.time + (faseDos ? segundosEntreCargas * 0.65f : segundosEntreCargas);
            cargando = false;
        }

        IEnumerator Onda()
        {
            var onda = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(onda.GetComponent<Collider>());
            onda.transform.position = transform.position + Vector3.up * 0.15f;
            var r = onda.GetComponent<MeshRenderer>();
            r.material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(1f, 0.7f, 0.3f) };
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var luz = new GameObject("Luz_Descarga").AddComponent<Light>();
            luz.transform.position = transform.position + Vector3.up * 1.5f;
            luz.type = LightType.Point; luz.color = new Color(1f, 0.65f, 0.25f); luz.range = radioDescarga * 1.6f; luz.shadows = LightShadows.None;
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                float d = Mathf.Lerp(0.5f, radioDescarga * 2f, t / 0.4f);
                onda.transform.localScale = new Vector3(d, 0.03f, d);
                luz.intensity = Mathf.Lerp(20f, 0f, t / 0.4f);
                yield return null;
            }
            Destroy(onda); Destroy(luz.gameObject);
        }

        void Morir()
        {
            Derrotado = true;
            cargando = false; ia.Detenido = false;
            StopAllCoroutines();
            luzNucleo.intensity = 0f;
            if (lienzo != null) lienzo.enabled = false;
            // Sin la unidad que lo aloja, CERBERUS pierde el control de las demás
            AndroideIA.DesconectarTodos();
            HUDNexus.Aviso($"{nombre} DESTRUIDA. CERBERUS perdió el control: unidades desconectadas.");
            if (!string.IsNullOrEmpty(objetivoAlDerrotar)) HUDNexus.Objetivo(objetivoAlDerrotar);
        }

        /// <summary>Barra de vida en texto, arriba al centro (el HUD del juego es solo texto).</summary>
        void MostrarBarra()
        {
            if (lienzo == null)
            {
                var lg = new GameObject("Lienzo_Jefe");
                lienzo = lg.AddComponent<Canvas>();
                lienzo.renderMode = RenderMode.ScreenSpaceOverlay; lienzo.sortingOrder = 5;
                var esc = lg.AddComponent<CanvasScaler>();
                esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; esc.referenceResolution = new Vector2(1920f, 1080f); esc.matchWidthOrHeight = 0.5f;
                var go = new GameObject("Barra_Jefe", typeof(RectTransform));
                go.transform.SetParent(lg.transform, false);
                barra = go.AddComponent<TextMeshProUGUI>();
                var hud = FindFirstObjectByType<HUDNexus>();
                if (hud != null) { var t = hud.GetComponentInChildren<TMP_Text>(true); if (t != null) barra.font = t.font; }
                barra.fontSize = 30f; barra.alignment = TextAlignmentOptions.Center;
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -70f); rt.sizeDelta = new Vector2(1000f, 50f);
            }
            lienzo.enabled = true;
            int total = 20, llenos = Mathf.CeilToInt(Mathf.Clamp01(ia.Salud / ia.SaludMaxima) * total);
            string estado = !cargando ? "  <color=#9aa4ad>BLINDADO</color>" : ia.MultiplicadorDanio > 1.5f ? "  <color=#ffd060>EXPUESTO</color>" : "  <color=#ff8a5a>CARGANDO</color>";
            barra.text = $"<color=#ff7a3a>{nombre}</color>  <color=#ff5a3a>{new string('|', llenos)}</color><color=#3a2a28>{new string('|', total - llenos)}</color>{estado}";
        }
    }
}
