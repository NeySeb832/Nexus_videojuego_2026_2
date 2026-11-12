using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus
{
    /// <summary>
    /// Controla un nivel: temporizador de refrigeración, objetivo inicial, aparición del jugador,
    /// paso al nivel siguiente, Game Over y Victoria. Hay uno por escena de nivel.
    /// </summary>
    [DefaultExecutionOrder(-50)]   // la malla de navegación debe existir antes de que se activen los androides
    public class NivelNexus : MonoBehaviour
    {
        public static NivelNexus Instancia { get; private set; }

        [SerializeField] string nombreNivel = "NIVEL 1 — CUARTO DE MANTENIMIENTO";
        [Tooltip("Tiempo de refrigeración al empezar el nivel, en segundos")]
        [SerializeField] float tiempoInicial = 420f;
        [Tooltip("Temperatura (%) al empezar; llega a 100% cuando el tiempo se agota")]
        [SerializeField] float temperaturaInicial = 80f;
        [SerializeField] string objetivoInicial = "Revisa la terminal de mantenimiento";
        [Tooltip("Escena que se carga al completar el nivel (vacío = Victoria)")]
        [SerializeField] string escenaSiguiente = "02_Almacenamiento";
        [SerializeField] Transform puntoInicio;

        public float TiempoRestante { get; private set; }
        public float TiempoInicial => tiempoInicial;
        public bool DiscoObtenido { get; set; }
        public bool Terminado { get; private set; }
        public string NombreNivel => nombreNivel;

        /// <summary>Temperatura del centro de datos: sube a medida que se agota el tiempo.</summary>
        public float Temperatura => Mathf.Lerp(100f, temperaturaInicial, Mathf.Clamp01(TiempoRestante / tiempoInicial));

        void Awake()
        {
            Instancia = this;
            Time.timeScale = 1f;
            // La malla de navegación de los androides se calcula al cargar el nivel
            foreach (var superficie in GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>())
                superficie.BuildNavMesh();
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        void Start()
        {
            // Las tarjetas y fusibles son de cada nivel; el resto del inventario se conserva
            EstadoPartida.Inventario.Remove(TipoColeccionable.TarjetaRoja);
            EstadoPartida.Inventario.Remove(TipoColeccionable.TarjetaAzul);
            EstadoPartida.Inventario.Remove(TipoColeccionable.Fusible);

            TiempoRestante = EstadoPartida.TiempoCargado ?? tiempoInicial;
            var jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null && EstadoPartida.PosicionCargada.HasValue)
                Teletransportar(jugador.transform, EstadoPartida.PosicionCargada.Value);
            EstadoPartida.PosicionCargada = null;
            EstadoPartida.TiempoCargado = null;

            EstadoPartida.GuardarInicioNivel(SceneManager.GetActiveScene().name);
            HUDNexus.Objetivo(objetivoInicial);
            HUDNexus.Aviso(nombreNivel);
            EstadoPartida.Notificar();
        }

        void Update()
        {
            if (Terminado) return;
            TiempoRestante -= Time.deltaTime;
            if (TiempoRestante <= 0f)
            {
                TiempoRestante = 0f;
                GameOver("COLAPSO TÉRMICO", "La temperatura llegó al 100%. Los servidores de Nexus se incendiaron.");
            }
        }

        public void SumarTiempo(float segundos) => TiempoRestante += segundos;

        public static void Teletransportar(Transform t, Vector3 pos)
        {
            var cc = t.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            t.position = pos;
            if (cc != null) cc.enabled = true;
        }

        // ---------- Final del nivel ----------
        public void CompletarNivel()
        {
            if (Terminado) return;
            Terminado = true;
            if (string.IsNullOrEmpty(escenaSiguiente)) HUDNexus.MostrarFinal(true, "SISTEMA RESTAURADO",
                "Los tres Discos Duros Maestros forzaron el reinicio manual. Nexus está a salvo.");
            else StartCoroutine(CargarSiguiente());
        }

        IEnumerator CargarSiguiente()
        {
            HUDNexus.Aviso("Nivel completado");
            yield return new WaitForSecondsRealtime(1.5f);
            SceneManager.LoadScene(escenaSiguiente);
        }

        public void GameOver(string titulo, string motivo)
        {
            if (Terminado) return;
            Terminado = true;
            StartCoroutine(MostrarGameOver(titulo, motivo));
        }

        IEnumerator MostrarGameOver(string titulo, string motivo)
        {
            yield return new WaitForSecondsRealtime(2.2f);   // deja ver la animación de muerte
            HUDNexus.MostrarFinal(false, titulo, motivo);
        }

        // ---------- Botones de la pantalla final ----------
        public void Reintentar()
        {
            EstadoPartida.RestaurarInicioNivel();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void CargarPartida()
        {
            string escena = EstadoPartida.Cargar();
            if (escena != null) SceneManager.LoadScene(escena);
        }

        public void IrAlMenu() => SceneManager.LoadScene("00_Menu");
    }
}
