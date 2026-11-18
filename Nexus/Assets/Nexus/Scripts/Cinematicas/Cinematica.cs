using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Nexus
{
    public enum AccionPlano { Ninguna, Caminar, Correr, MirarAlrededor, AbrirPuerta, CerrarPuerta, Alarma, DespertarAndroide, Restaurar }
    public enum ReferenciaPlano { Mundo, Jugador, Androide }

    /// <summary>
    /// Un plano de la cinemática: dónde está la cámara al empezar y al terminar (se mueve suavemente
    /// entre las dos), hacia dónde mira, el subtítulo y lo que pasa en escena. Las posiciones pueden
    /// ser del mundo o relativas al jugador o a un androide (en su espacio local: z = hacia adelante).
    /// </summary>
    [Serializable]
    public class PlanoCinematica
    {
        public float duracion = 3f;
        public ReferenciaPlano referencia;
        public int androide;
        public Vector3 camDesde, camHasta, mirarDesde, mirarHasta;
        public float fov = 45f;
        public float temblor;
        public string hablante;
        [TextArea(1, 3)] public string subtitulo;
        public AccionPlano[] acciones;
        public Vector3 destino;
    }

    /// <summary>
    /// Cinemática hecha con el motor del juego: toma el control de la cámara y del personaje,
    /// muestra barras negras y subtítulos, y al terminar devuelve el control. Se puede saltar con
    /// Enter, Esc, Start o tocando la pantalla (se aplica el estado final: puerta cerrada, alarma...).
    ///  - Al empezar el nivel: el técnico entra al centro de datos y mira a su alrededor.
    ///  - Al extraer el Disco Maestro: suena la alarma, las unidades SENTINEL se encienden y salen a cazar.
    /// En el texto, {NOMBRE} se reemplaza por el técnico elegido (Neyder o Deisy).
    /// </summary>
    public class Cinematica : MonoBehaviour
    {
        public enum Momento { AlEmpezarNivel, AlExtraerDisco, AlTerminarJuego }

        /// <summary>Hay una cinemática (o la transición del ascensor) en curso: el jugador no tiene el control.</summary>
        public static bool EnCurso { get; internal set; }
        public static Cinematica Actual { get; private set; }
        /// <summary>Cinemática del final del juego (reinicio manual), si el nivel tiene una.</summary>
        public static Cinematica Final { get; private set; }
        /// <summary>Para pruebas automáticas: salta la cinemática en curso.</summary>
        public static bool SaltoSimulado { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { EnCurso = false; Actual = null; Final = null; SaltoSimulado = false; }

        [SerializeField] Momento momento;
        [SerializeField] PlanoCinematica[] planos;
        [SerializeField] PuertaSeguridad puerta;
        [SerializeField] List<AndroideIA> androides = new List<AndroideIA>();
        [Tooltip("Al terminar la cinemática de alarma: segundos que los androides saben dónde está el técnico")]
        [SerializeField] float segundosCaza = 22f;
        [SerializeField] string objetivoAlTerminar;

        Transform jugador;
        ControladorJugador control;
        Camera camara;
        Canvas lienzo;
        RectTransform barraSup, barraInf;
        TMP_Text textoSub, textoSaltar;
        bool hecha, saltar, alarmaHecha, puertaCerrada;

        [Tooltip("Intro: dónde empieza el técnico (detrás de la puerta de servicio)")]
        [SerializeField] bool conInicio;
        [SerializeField] Vector3 inicio;
        [SerializeField] float giroInicio;

        public void Configurar(Momento cuando, PlanoCinematica[] lista, PuertaSeguridad puertaEscena, List<AndroideIA> unidades, string objetivo)
        {
            momento = cuando; planos = lista; puerta = puertaEscena; androides = unidades; objetivoAlTerminar = objetivo;
        }

        public void ConfigurarInicio(Vector3 pos, float giro) { conInicio = true; inicio = pos; giroInicio = giro; }

        void OnEnable()
        {
            if (momento == Momento.AlExtraerDisco) Coleccionable.AlRecoger += AlRecoger;
            if (momento == Momento.AlTerminarJuego) Final = this;
        }
        void OnDisable()
        {
            Coleccionable.AlRecoger -= AlRecoger;
            if (Actual == this) { EnCurso = false; Actual = null; }
            if (Final == this) Final = null;
        }

        /// <summary>Final del juego: se reproduce y al terminar se llama a "alTerminar" (pantalla de victoria).</summary>
        public void ReproducirFinal(Action alTerminar) => StartCoroutine(Reproducir(alTerminar));

        void Start()
        {
            var j = GameObject.FindGameObjectWithTag("Player");
            if (j != null) { jugador = j.transform; control = j.GetComponent<ControladorJugador>(); }
            if (momento != Momento.AlEmpezarNivel) return;
            // La intro no se repite al cargar una partida guardada ni al reintentar el nivel
            if (EstadoPartida.IntroVista(gameObject.scene.name) || jugador == null)
            {
                if (puerta != null) puerta.Cerrar(true);   // la puerta de servicio ya quedó sellada
                return;
            }
            if (conInicio)
            {
                NivelNexus.Teletransportar(jugador, inicio);
                jugador.rotation = Quaternion.Euler(0f, giroInicio, 0f);
            }
            EnCurso = true; Actual = this;   // desde ya: la puerta no se abre sola ni el jugador se mueve
            StartCoroutine(Reproducir());
        }

        void AlRecoger(Coleccionable c)
        {
            if (c.Tipo == TipoColeccionable.DiscoDuro && !hecha) StartCoroutine(Reproducir());
        }

        // ======================= Reproducción =======================
        IEnumerator Reproducir(Action alTerminar = null)
        {
            if (hecha || jugador == null || planos == null || planos.Length == 0) { alTerminar?.Invoke(); yield break; }
            hecha = true;
            if (momento == Momento.AlEmpezarNivel) EstadoPartida.MarcarIntro(gameObject.scene.name);
            yield return null;   // deja que el resto del nivel termine de arrancar
            while (HUDNexus.DialogoAbierto) yield return null;

            EnCurso = true; Actual = this; saltar = false;
            AndroideIA.Pausados = true;
            if (PantallaInventario.Abierta) PantallaInventario.Instancia.Cerrar();
            var gestor = GestorCamaras.Instancia;
            if (gestor != null) gestor.Pausar();
            // El HUD se oculta mientras dura (las barras y los subtítulos ocupan la pantalla)
            var lienzosHud = new List<Canvas>();
            var hud = FindFirstObjectByType<HUDNexus>();
            if (hud != null)
                foreach (var cv in hud.GetComponentsInChildren<Canvas>())
                    if (cv.enabled) { cv.enabled = false; lienzosHud.Add(cv); }
            CrearCamaraYLienzo();
            control.EntradaSimulada = Vector2.zero; control.CorrerSimulado = false;

            yield return Barras(true, 0.45f);
            foreach (var plano in planos)
            {
                if (saltar) break;
                yield return Plano(plano);
            }
            if (saltar) AplicarFinal();
            textoSub.text = "";
            yield return Barras(false, 0.35f);

            control.EntradaSimulada = null; control.CorrerSimulado = null;
            Destroy(camara.gameObject); Destroy(lienzo.gameObject);
            foreach (var cv in lienzosHud) if (cv != null) cv.enabled = true;
            if (gestor != null) gestor.Reanudar();
            AndroideIA.Pausados = false;
            if (momento == Momento.AlExtraerDisco) AndroideIA.AlarmaGeneral(segundosCaza);
            if (!string.IsNullOrEmpty(objetivoAlTerminar)) HUDNexus.Objetivo(objetivoAlTerminar);
            EnCurso = false; Actual = null;
            alTerminar?.Invoke();
        }

        IEnumerator Plano(PlanoCinematica p)
        {
            textoSub.text = string.IsNullOrEmpty(p.subtitulo) ? "" :
                (string.IsNullOrEmpty(p.hablante) ? "" : $"<color=#7fe9ff>{Nombre(p.hablante)}:</color> ") + Nombre(p.subtitulo);
            camara.fieldOfView = p.fov;
            Coroutine accionMovimiento = null;
            if (p.acciones != null)
                foreach (var a in p.acciones)
                {
                    var c = Ejecutar(a, p);
                    if (c != null) accionMovimiento = c;
                }

            for (float t = 0f; t < p.duracion; t += Time.deltaTime)
            {
                if (PulsoSaltar()) { saltar = true; break; }
                float k = Mathf.SmoothStep(0f, 1f, t / p.duracion);
                var (pos, mirar) = Encuadre(p, k);
                if (p.temblor > 0f) pos += UnityEngine.Random.insideUnitSphere * p.temblor * (1f - k * 0.5f);
                camara.transform.position = pos;
                camara.transform.rotation = Quaternion.LookRotation(mirar - pos);
                yield return null;
            }
            if (accionMovimiento != null) StopCoroutine(accionMovimiento);
            control.EntradaSimulada = Vector2.zero; control.CorrerSimulado = false;
        }

        (Vector3 pos, Vector3 mirar) Encuadre(PlanoCinematica p, float k)
        {
            Vector3 pos = Vector3.Lerp(p.camDesde, p.camHasta, k), mirar = Vector3.Lerp(p.mirarDesde, p.mirarHasta, k);
            Transform r = p.referencia == ReferenciaPlano.Jugador ? jugador
                        : p.referencia == ReferenciaPlano.Androide && p.androide < androides.Count && androides[p.androide] != null ? androides[p.androide].transform
                        : null;
            if (r == null) return (pos, mirar);
            var giro = Quaternion.Euler(0f, r.eulerAngles.y, 0f);
            return (r.position + giro * pos, r.position + giro * mirar);
        }

        Coroutine Ejecutar(AccionPlano a, PlanoCinematica p)
        {
            switch (a)
            {
                case AccionPlano.Caminar: return StartCoroutine(IrA(p.destino, false));
                case AccionPlano.Correr: return StartCoroutine(IrA(p.destino, true));
                case AccionPlano.MirarAlrededor:
                    var ik = jugador.GetComponentInChildren<IKTecnico>();
                    if (ik != null) ik.MirarAlrededor(p.duracion);
                    break;
                case AccionPlano.AbrirPuerta: if (puerta != null) puerta.Abrir(); break;
                case AccionPlano.CerrarPuerta: CerrarPuerta(); break;
                case AccionPlano.Alarma: Alarma(); break;
                case AccionPlano.DespertarAndroide:
                    if (p.androide < androides.Count && androides[p.androide] != null) androides[p.androide].DespertarEnCinematica();
                    break;
                case AccionPlano.Restaurar: Restaurar(); break;
            }
            return null;
        }

        /// <summary>Camina (o corre) hasta el destino con los mismos controles tipo tanque del jugador.</summary>
        IEnumerator IrA(Vector3 destino, bool correr)
        {
            control.CorrerSimulado = correr;
            while (true)
            {
                Vector3 d = destino - jugador.position; d.y = 0f;
                if (d.magnitude < 0.3f) break;
                float ang = Vector3.SignedAngle(jugador.forward, d, Vector3.up);
                control.EntradaSimulada = new Vector2(Mathf.Clamp(ang / 25f, -1f, 1f), Mathf.Abs(ang) < 60f ? 1f : 0f);
                yield return null;
            }
            control.EntradaSimulada = Vector2.zero; control.CorrerSimulado = false;
        }

        void CerrarPuerta()
        {
            if (puerta == null || puertaCerrada) return;
            puertaCerrada = true;
            puerta.Cerrar(true);
        }

        bool restaurado;

        /// <summary>Reinicio manual: vuelve la luz normal y todas las unidades SENTINEL se desconectan.</summary>
        void Restaurar()
        {
            if (restaurado) return;
            restaurado = true;
            AlarmaNivel.Desactivar();
            AndroideIA.DesconectarTodos();
        }

        void Alarma()
        {
            if (alarmaHecha) return;
            alarmaHecha = true;
            AlarmaNivel.Activar();
        }

        /// <summary>Al saltar: el estado en que habría quedado todo al terminar.</summary>
        void AplicarFinal()
        {
            foreach (var p in planos)
            {
                if (p.acciones == null) continue;
                foreach (var a in p.acciones)
                {
                    if (a == AccionPlano.Caminar || a == AccionPlano.Correr)
                    {
                        var d = p.destino - jugador.position; d.y = 0f;
                        if (d.sqrMagnitude > 0.01f) jugador.rotation = Quaternion.LookRotation(d);
                        NivelNexus.Teletransportar(jugador, new Vector3(p.destino.x, jugador.position.y, p.destino.z));
                    }
                    else if (a == AccionPlano.AbrirPuerta && puerta != null) puerta.Abrir();
                    else if (a == AccionPlano.CerrarPuerta) CerrarPuerta();
                    else if (a == AccionPlano.Alarma) Alarma();
                    else if (a == AccionPlano.Restaurar) Restaurar();
                }
            }
        }

        static string Nombre(string s)
        {
            string n = EstadoPartida.PersonajeElegido == 0 ? "Neyder" : "Deisy";
            return s.Replace("{NOMBRE}", n).Replace("{NOMBRE_MAY}", n.ToUpperInvariant());
        }

        bool PulsoSaltar()
        {
            if (SaltoSimulado) { SaltoSimulado = false; return true; }
            var k = Keyboard.current;
            if (k != null && (k.enterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) return true;
            var g = Gamepad.current;
            if (g != null && (g.startButton.wasPressedThisFrame || g.buttonSouth.wasPressedThisFrame)) return true;
            var ts = Touchscreen.current;
            return ts != null && ts.primaryTouch.press.wasPressedThisFrame;
        }

        // ======================= Cámara, barras y subtítulos =======================
        void CrearCamaraYLienzo()
        {
            var go = new GameObject("Cam_Cinematica");
            camara = go.AddComponent<Camera>();
            camara.nearClipPlane = 0.05f; camara.farClipPlane = 60f; camara.depth = 20f;
            camara.clearFlags = CameraClearFlags.SolidColor; camara.backgroundColor = Color.black;
            camara.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var (pos, mirar) = Encuadre(planos[0], 0f);
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mirar - pos));

            var lg = new GameObject("Lienzo_Cinematica");
            lienzo = lg.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceOverlay; lienzo.sortingOrder = 60;
            var esc = lg.AddComponent<CanvasScaler>();
            esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; esc.referenceResolution = new Vector2(1920f, 1080f); esc.matchWidthOrHeight = 0.5f;
            barraSup = Barra("Barra_Sup", true);
            barraInf = Barra("Barra_Inf", false);

            TMP_FontAsset fuente = null;
            var hud = FindFirstObjectByType<HUDNexus>();
            if (hud != null) { var t = hud.GetComponentInChildren<TMP_Text>(true); if (t != null) fuente = t.font; }
            textoSub = Texto("Subtitulo", fuente, 40f, new Vector2(0f, 150f), new Vector2(1500f, 120f));
            textoSaltar = Texto("Saltar", fuente, 22f, Vector2.zero, new Vector2(360f, 40f));
            textoSaltar.alignment = TextAlignmentOptions.Right;
            var rs = (RectTransform)textoSaltar.transform;   // esquina inferior derecha, sobre la barra
            rs.anchorMin = rs.anchorMax = rs.pivot = new Vector2(1f, 0f);
            rs.anchoredPosition = new Vector2(-40f, 30f);
            textoSaltar.color = new Color(1f, 1f, 1f, 0.45f);
            textoSaltar.text = "ENTER / ESC  saltar";
        }

        RectTransform Barra(string nombre, bool arriba)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(lienzo.transform, false);
            go.GetComponent<Image>().color = Color.black;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, arriba ? 1f : 0f); rt.anchorMax = new Vector2(1f, arriba ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, arriba ? 1f : 0f);
            rt.sizeDelta = new Vector2(0f, 0f);
            return rt;
        }

        TMP_Text Texto(string nombre, TMP_FontAsset fuente, float tam, Vector2 pos, Vector2 caja)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(lienzo.transform, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (fuente != null) t.font = fuente;
            t.fontSize = tam; t.alignment = TextAlignmentOptions.Center; t.color = Color.white;
            t.textWrappingMode = TextWrappingModes.Normal;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos; rt.sizeDelta = caja;
            return t;
        }

        IEnumerator Barras(bool entrar, float duracion)
        {
            const float alto = 130f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duracion)
            {
                float h = Mathf.SmoothStep(0f, 1f, entrar ? t : 1f - t) * alto;
                barraSup.sizeDelta = barraInf.sizeDelta = new Vector2(0f, h);
                yield return null;
            }
            barraSup.sizeDelta = barraInf.sizeDelta = new Vector2(0f, entrar ? alto : 0f);
        }
    }
}
