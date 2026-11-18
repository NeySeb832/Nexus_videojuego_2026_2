using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus
{
    /// <summary>
    /// Válvula de refrigeración del Núcleo (Nivel 3) y su puzzle.
    ///
    /// Cada válvula tiene un manómetro con su presión, que cambia en cada partida. El manual de
    /// procedimiento dice que se abren de MENOR a MAYOR presión: hay que recorrer las alas, leer los
    /// manómetros y decidir el orden. Si se abre una fuera de orden hay SOBREPRESIÓN: estalla un
    /// chorro de vapor, todas las válvulas se vuelven a cerrar, el núcleo pierde 15 s y el estruendo
    /// atrae a los androides cercanos.
    ///
    /// Al abrir una en orden, la rueda gira, el núcleo se enfría (suma segundos, solo la primera vez)
    /// y se apaga un tercio del escudo térmico del Disco Maestro 3.
    /// </summary>
    public class ValvulaRefrigeracion : Interactuable
    {
        public static readonly List<ValvulaRefrigeracion> Todas = new List<ValvulaRefrigeracion>();
        static bool presionesAsignadas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { Todas.Clear(); presionesAsignadas = false; }

        [SerializeField] string nombre = "norte";
        [SerializeField] float segundos = 60f;
        [SerializeField] float penalizacion = 15f;
        [SerializeField] Transform rueda;
        [SerializeField] Light luz;
        [SerializeField] GameObject fxVapor;

        bool abierta, bonoDado;
        float presion;
        string ClaveBono => SceneManager.GetActiveScene().name + "/bono_valvula_" + nombre;

        public bool Abierta => abierta;
        public float Presion => presion;
        public string Nombre => nombre;
        public override string TextoAccion => abierta ? $"Válvula {nombre}: abierta" : $"Abrir válvula {nombre} · manómetro {presion:0.0} bar";

        public void Configurar(string nuevoNombre, float segundosQueSuma, Transform ruedaVisual, Light luzEstado, string objetivo, GameObject vapor)
        {
            nombre = nuevoNombre; segundos = segundosQueSuma; rueda = ruedaVisual; luz = luzEstado; objetivoAlCompletar = objetivo; fxVapor = vapor;
        }

        void Awake() => Todas.Add(this);
        protected override void OnDisable() { base.OnDisable(); }
        void OnDestroy() => Todas.Remove(this);

        void Start()
        {
            if (!presionesAsignadas) AsignarPresiones();
            // El tiempo extra de una válvula se da una sola vez, aunque se cargue la partida
            bonoDado = EstadoPartida.Registrados.Contains(ClaveBono);
            if (luz != null) luz.color = new Color(1f, 0.25f, 0.1f);
        }

        /// <summary>Presiones distintas y desordenadas en cada partida (el orden correcto cambia).</summary>
        static void AsignarPresiones()
        {
            presionesAsignadas = true;
            var valores = new List<float>();
            for (int i = 0; i < Todas.Count; i++) valores.Add(1.8f + i * 1.6f + Random.Range(-0.4f, 0.4f));
            for (int i = valores.Count - 1; i > 0; i--) { int k = Random.Range(0, i + 1); (valores[i], valores[k]) = (valores[k], valores[i]); }
            for (int i = 0; i < Todas.Count; i++) Todas[i].presion = Mathf.Round(valores[i] * 10f) / 10f;
        }

        public override void Interactuar(InteraccionJugador jugador)
        {
            if (abierta) { HUDNexus.Aviso($"La válvula {nombre} ya está abierta."); return; }
            var siguiente = Todas.Where(v => !v.abierta).OrderBy(v => v.presion).FirstOrDefault();
            if (siguiente != this) { Sobrepresion(); return; }

            abierta = true;
            string extra = "";
            if (!bonoDado)
            {
                bonoDado = true;
                if (!EstadoPartida.Registrados.Contains(ClaveBono)) EstadoPartida.Registrados.Add(ClaveBono);
                if (NivelNexus.Instancia != null) NivelNexus.Instancia.SumarTiempo(segundos);
                extra = $": el núcleo se enfría +{segundos:0} s";
            }
            int n = EscudoTermico.ValvulaAbierta();
            HUDNexus.Aviso($"Válvula {nombre} abierta ({n}/{EscudoTermico.Total}){extra}");
            if (n >= EscudoTermico.Total) CompletarObjetivo();
            StartCoroutine(Girar(1f));
        }

        /// <summary>Orden equivocado: vapor, todas cerradas, menos tiempo y ruido que atrae androides.</summary>
        void Sobrepresion()
        {
            foreach (var v in Todas) v.Cerrar();
            EscudoTermico.Restablecer();
            if (NivelNexus.Instancia != null) NivelNexus.Instancia.SumarTiempo(-penalizacion);
            HUDNexus.MostrarDialogo("ALERTA DEL SISTEMA", new[]
            {
                $"¡SOBREPRESIÓN EN LA VÁLVULA {nombre.ToUpperInvariant()}! Secuencia de enfriamiento incorrecta.",
                $"Todas las válvulas se cerraron por seguridad. Temperatura del núcleo en aumento (-{penalizacion:0} s)."
            }, null);
            StartCoroutine(Vapor());
            foreach (var a in AndroideIA.Todos)
                if (a != null && !a.Destruido && (a.transform.position - transform.position).sqrMagnitude < 22f * 22f)
                    a.Atraer(transform.position, 5f);
        }

        void Cerrar()
        {
            if (!abierta) return;
            abierta = false;
            if (luz != null) luz.color = new Color(1f, 0.25f, 0.1f);
            StartCoroutine(Girar(-1f));
        }

        IEnumerator Girar(float sentido)
        {
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                if (rueda != null) rueda.Rotate(0f, 0f, 300f * sentido * Time.deltaTime, Space.Self);
                yield return null;
            }
            if (sentido > 0f && luz != null && abierta) luz.color = new Color(0.3f, 0.85f, 1f);
        }

        IEnumerator Vapor()
        {
            Vector3 punto = (rueda != null ? rueda.position : transform.position + Vector3.up * 1.2f);
            if (fxVapor != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var fx = Instantiate(fxVapor, punto + Random.insideUnitSphere * 0.3f, Quaternion.LookRotation(transform.forward));
                    fx.transform.localScale = Vector3.one * 1.6f;
                    Destroy(fx, 2f);
                }
            }
            var flash = new GameObject("Luz_Sobrepresion").AddComponent<Light>();
            flash.transform.position = punto + transform.forward * 0.8f;
            flash.type = LightType.Point; flash.color = new Color(1f, 0.55f, 0.3f); flash.range = 8f; flash.shadows = LightShadows.None;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { flash.intensity = Mathf.Lerp(15f, 0f, t / 0.6f); yield return null; }
            Destroy(flash.gameObject);
        }
    }
}
