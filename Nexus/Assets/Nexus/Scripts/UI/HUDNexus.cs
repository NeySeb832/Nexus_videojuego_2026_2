using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nexus
{
    /// <summary>
    /// Interfaz durante la partida.
    /// Canvas 2 (HUD): corazones, temporizador de refrigeración, temperatura, discos, refrigerantes,
    /// objetivo, avisos, "[E] acción" y cuadros de diálogo estilo terminal.
    /// Canvas 3 (pantalla final): Game Over o Victoria con sus botones.
    /// Cualquier script usa los métodos estáticos (HUDNexus.Aviso, HUDNexus.MostrarDialogo...).
    /// </summary>
    public class HUDNexus : MonoBehaviour
    {
        static HUDNexus inst;

        [Header("Estado del técnico")]
        [SerializeField] Image[] corazones;
        [SerializeField] Sprite corazonLleno;
        [SerializeField] Sprite corazonVacio;
        [SerializeField] TMP_Text textoPersonaje;
        [Tooltip("La salud no está siempre en pantalla: aparece unos segundos cuando cambia")]
        [SerializeField] CanvasGroup grupoSalud;
        [SerializeField] TMP_Text textoEstadoSalud;
        [Tooltip("Borde rojo que late cuando queda un solo corazón")]
        [SerializeField] Image vinetaDanio;

        [Header("Refrigeración")]
        [SerializeField] TMP_Text textoTiempo;
        [SerializeField] TMP_Text textoTemperatura;
        [SerializeField] Image barraTemperatura;

        [Header("Recursos")]
        [SerializeField] TMP_Text textoDiscos;
        [SerializeField] TMP_Text textoRefrigerante;
        [SerializeField] TMP_Text textoInventario;
        [Tooltip("Habilidad del técnico y su recarga (Q)")]
        [SerializeField] TMP_Text textoHabilidad;
        [Tooltip("Arma equipada y munición (cargador / reserva)")]
        [SerializeField] TMP_Text textoArma;

        [Header("Mensajes")]
        [SerializeField] TMP_Text textoObjetivo;
        [SerializeField] GameObject panelPrompt;
        [SerializeField] TMP_Text textoPrompt;
        [SerializeField] CanvasGroup grupoAviso;
        [SerializeField] TMP_Text textoAviso;

        [Header("Diálogo")]
        [SerializeField] GameObject panelDialogo;
        [SerializeField] TMP_Text tituloDialogo;
        [SerializeField] TMP_Text textoDialogo;
        [SerializeField] TMP_Text pistaDialogo;

        [Header("Pantalla final (Canvas 3)")]
        [SerializeField] Canvas canvasFinal;

        static readonly Color ColorInfo = new Color(0.55f, 0.95f, 1f);
        static readonly Color ColorAlerta = new Color(1f, 0.32f, 0.25f);

        readonly Queue<(string titulo, string[] lineas, Action fin)> colaDialogos = new Queue<(string, string[], Action)>();
        Coroutine rutinaAviso;
        float escalaPrevia = 1f;

        public static bool DialogoAbierto { get; private set; }

        // ======================= API estática =======================

        public static void Aviso(string mensaje)
        {
            if (inst == null) { Debug.Log("[Aviso] " + mensaje); return; }
            if (inst.rutinaAviso != null) inst.StopCoroutine(inst.rutinaAviso);
            inst.rutinaAviso = inst.StartCoroutine(inst.MostrarAviso(mensaje));
        }

        public static void Objetivo(string texto)
        {
            if (inst == null || inst.textoObjetivo == null) return;
            inst.textoObjetivo.text = "OBJETIVO: " + texto.ToUpperInvariant();
            inst.StartCoroutine(inst.ResaltarObjetivo());
        }

        public static void Prompt(string texto)
        {
            if (inst == null || inst.panelPrompt == null) return;
            bool mostrar = !string.IsNullOrEmpty(texto);
            if (inst.panelPrompt.activeSelf != mostrar) inst.panelPrompt.SetActive(mostrar);
            if (mostrar) inst.textoPrompt.text = texto;
        }

        public static void MostrarDialogo(string titulo, string[] lineas, Action alTerminar)
        {
            if (inst == null) { alTerminar?.Invoke(); return; }
            inst.colaDialogos.Enqueue((titulo, lineas, alTerminar));
            if (!DialogoAbierto) inst.StartCoroutine(inst.ProcesarDialogos());
        }

        /// <summary>Línea de la habilidad especial: cian cuando está lista, gris mientras recarga.</summary>
        public static void Habilidad(string texto, bool lista)
        {
            if (inst == null || inst.textoHabilidad == null) return;
            inst.textoHabilidad.text = texto;
            inst.textoHabilidad.color = lista ? ColorInfo : new Color(0.5f, 0.58f, 0.62f);
        }

        /// <summary>Línea del arma: nombre y munición (vacía si no lleva ninguna).</summary>
        public static void Arma(string texto)
        {
            if (inst == null || inst.textoArma == null) return;
            inst.textoArma.text = texto;
        }

        public static void MostrarFinal(bool victoria, string titulo, string motivo)
        {
            AudioNexus.Final(victoria);
            if (inst != null) inst.Final(victoria, titulo, motivo);
        }

        // ======================= Ciclo de vida =======================

        void Awake()
        {
            inst = this;
            DialogoAbierto = false;
            if (panelDialogo != null) panelDialogo.SetActive(false);
            if (panelPrompt != null) panelPrompt.SetActive(false);
            if (grupoAviso != null) grupoAviso.alpha = 0f;
            if (canvasFinal != null) canvasFinal.gameObject.SetActive(false);
        }

        void OnEnable() => EstadoPartida.AlCambiar += Refrescar;
        void OnDisable() => EstadoPartida.AlCambiar -= Refrescar;

        void OnDestroy()
        {
            if (inst == this) inst = null;
            DialogoAbierto = false;
        }

        void Start() => Refrescar();

        void Update()
        {
            var nivel = NivelNexus.Instancia;
            if (nivel == null || textoTiempo == null) return;

            float t = Mathf.Max(0f, nivel.TiempoRestante);
            textoTiempo.text = $"{(int)(t / 60f):00}:{(int)(t % 60f):00}";
            bool critico = t < 60f;
            textoTiempo.color = critico ? ColorAlerta : ColorInfo;
            if (t < 30f) textoTiempo.alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
            else textoTiempo.alpha = 1f;

            float temp = nivel.Temperatura;
            textoTemperatura.text = $"NÚCLEO {temp:0}%";
            textoTemperatura.color = Color.Lerp(new Color(1f, 0.75f, 0.3f), ColorAlerta, Mathf.InverseLerp(85f, 100f, temp));
            if (barraTemperatura != null) barraTemperatura.fillAmount = Mathf.InverseLerp(60f, 100f, temp);

            if (vinetaDanio != null)
            {
                float objetivo = EstadoPartida.Salud == 1 ? 0.22f + 0.1f * Mathf.Sin(Time.unscaledTime * 4f) : 0f;
                var c = vinetaDanio.color;
                c.a = Mathf.MoveTowards(c.a, objetivo, Time.unscaledDeltaTime * 2f);
                vinetaDanio.color = c;
            }
        }

        int saludMostrada = -1;
        Coroutine rutinaSalud;

        /// <summary>Texto del estado según los corazones que quedan.</summary>
        public static (string texto, Color color) EstadoSalud()
        {
            return EstadoPartida.Salud >= EstadoPartida.SaludMaxima ? ("ESTABLE", new Color(0.4f, 1f, 0.6f))
                 : EstadoPartida.Salud == 2 ? ("HERIDO", new Color(1f, 0.8f, 0.25f))
                 : ("CRÍTICO", ColorAlerta);
        }

        IEnumerator MostrarSalud()
        {
            for (float t = grupoSalud.alpha; t < 1f; t += Time.unscaledDeltaTime * 5f) { grupoSalud.alpha = t; yield return null; }
            grupoSalud.alpha = 1f;
            yield return new WaitForSecondsRealtime(3.5f);
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime * 1.5f) { grupoSalud.alpha = t; yield return null; }
            grupoSalud.alpha = 0f;
        }

        void Refrescar()
        {
            for (int i = 0; i < corazones.Length; i++)
                if (corazones[i] != null) corazones[i].sprite = i < EstadoPartida.Salud ? corazonLleno : corazonVacio;
            if (textoEstadoSalud != null)
            {
                var (texto, color) = EstadoSalud();
                textoEstadoSalud.text = "ESTADO: " + texto;
                textoEstadoSalud.color = color;
            }
            // Solo se muestra cuando la salud cambia (no al empezar el nivel)
            if (grupoSalud != null && saludMostrada >= 0 && saludMostrada != EstadoPartida.Salud)
            {
                if (rutinaSalud != null) StopCoroutine(rutinaSalud);
                rutinaSalud = StartCoroutine(MostrarSalud());
            }
            saludMostrada = EstadoPartida.Salud;
            if (textoPersonaje != null) textoPersonaje.text = "TÉCNICO: " + EstadoPartida.NombrePersonaje;
            if (textoDiscos != null) textoDiscos.text = $"DISCOS {EstadoPartida.Discos}/{EstadoPartida.DiscosTotales}";
            if (textoRefrigerante != null) textoRefrigerante.text = $"CARTUCHOS x{EstadoPartida.Refrigerantes}";
            if (textoInventario != null)
            {
                var partes = new List<string>();
                void Item(TipoColeccionable t, string n) { int c = EstadoPartida.Cantidad(t); if (c > 0) partes.Add(c > 1 ? $"{n} x{c}" : n); }
                Item(TipoColeccionable.TarjetaRoja, "<color=#ff5a50>Tarjeta roja</color>");
                Item(TipoColeccionable.TarjetaAzul, "<color=#50a0ff>Tarjeta azul</color>");
                Item(TipoColeccionable.Fusible, "Fusible");
                Item(TipoColeccionable.USB, "USB");
                Item(TipoColeccionable.Speedmax, "Speedmax");
                Item(TipoColeccionable.Chocorramo, "Chocorramo");
                textoInventario.text = partes.Count > 0 ? string.Join("  ·  ", partes) : "";
            }
        }

        // ======================= Avisos y objetivo =======================

        IEnumerator MostrarAviso(string mensaje)
        {
            textoAviso.text = mensaje;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime * 6f) { grupoAviso.alpha = t; yield return null; }
            grupoAviso.alpha = 1f;
            yield return new WaitForSecondsRealtime(2.6f);
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime * 2f) { grupoAviso.alpha = t; yield return null; }
            grupoAviso.alpha = 0f;
        }

        IEnumerator ResaltarObjetivo()
        {
            for (int i = 0; i < 6; i++)
            {
                textoObjetivo.color = i % 2 == 0 ? Color.white : ColorInfo;
                yield return new WaitForSecondsRealtime(0.15f);
            }
            textoObjetivo.color = ColorInfo;
        }

        // ======================= Diálogos =======================

        /// <summary>Avanza el diálogo desde una prueba automática o un botón táctil.</summary>
        public static bool AvanceSimulado { get; set; }

        static bool PulsoAvanzar()
        {
            if (AvanceSimulado) { AvanceSimulado = false; return true; }
            var k = Keyboard.current;
            if (k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) return true;
            var m = Mouse.current;
            if (m != null && m.leftButton.wasPressedThisFrame) return true;
            var g = Gamepad.current;
            if (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame)) return true;
            var ts = Touchscreen.current;
            return ts != null && ts.primaryTouch.press.wasPressedThisFrame;
        }

        IEnumerator ProcesarDialogos()
        {
            DialogoAbierto = true;
            escalaPrevia = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;                       // el juego se pausa mientras se lee
            panelDialogo.SetActive(true);
            Prompt(null);
            AudioNexus.Interfaz(Sfx.TerminalBeep, 0.5f);
            yield return null;                          // ignora la pulsación que abrió el diálogo

            while (colaDialogos.Count > 0)
            {
                var (titulo, lineas, fin) = colaDialogos.Dequeue();
                tituloDialogo.text = "[!] " + titulo.ToUpperInvariant();
                foreach (var linea in lineas)
                {
                    textoDialogo.text = linea;
                    textoDialogo.maxVisibleCharacters = 0;
                    pistaDialogo.alpha = 0f;
                    int total = linea.Length;
                    float visibles = 0f;
                    while (visibles < total)
                    {
                        visibles += Time.unscaledDeltaTime * 55f;
                        textoDialogo.maxVisibleCharacters = (int)visibles;
                        if (PulsoAvanzar()) visibles = total;
                        yield return null;
                    }
                    textoDialogo.maxVisibleCharacters = total;
                    pistaDialogo.alpha = 1f;
                    yield return null;
                    while (!PulsoAvanzar())
                    {
                        pistaDialogo.alpha = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
                        yield return null;
                    }
                    AudioNexus.Interfaz(Sfx.UIBlip, 0.35f);
                    yield return null;
                }
                fin?.Invoke();
            }

            panelDialogo.SetActive(false);
            if (NivelNexus.Instancia == null || !NivelNexus.Instancia.Terminado) Time.timeScale = escalaPrevia;
            DialogoAbierto = false;
        }

        // ======================= Pantalla final =======================

        void Final(bool victoria, string titulo, string motivo)
        {
            StopAllCoroutines();
            colaDialogos.Clear();
            panelDialogo.SetActive(false);
            DialogoAbierto = false;
            Prompt(null);
            Time.timeScale = 0f;
            if (PantallaInventario.Abierta) PantallaInventario.Instancia.Cerrar();
            Time.timeScale = 0f;

            // La pantalla final (Canvas 3) se encarga de su propia animación
            canvasFinal.gameObject.SetActive(true);
            GetComponent<PantallaFinal>().Mostrar(victoria, titulo, motivo);
        }
    }
}
