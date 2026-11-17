using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nexus
{
    /// <summary>
    /// Terminal portátil del técnico (inventario). Pausa el juego y muestra:
    ///  - Retrato, signos vitales (línea de pulso animada), estado y oportunidades.
    ///  - OBJETOS: cuadrícula de objetos, vista previa, descripción y uso de objetos de curación.
    ///  - ARCHIVO: notas leídas.
    ///  - MAPA: plano esquemático del nivel con la posición del jugador.
    /// Se abre con TAB o I (o el botón táctil) y se cierra con TAB, I o ESC.
    /// </summary>
    public class PantallaInventario : MonoBehaviour
    {
        public static PantallaInventario Instancia { get; private set; }
        public static bool Abierta { get; private set; }

        [Header("Estructura")]
        [SerializeField] GameObject raizCanvas;
        [Tooltip("El HUD se oculta mientras el inventario está abierto")]
        [SerializeField] GameObject canvasHud;
        [SerializeField] GameObject paginaObjetos, paginaArchivo, paginaMapa;
        [SerializeField] Button tabObjetos, tabArchivo, tabMapa, botonCerrar;

        [Header("Técnico")]
        [SerializeField] Image retrato;
        [SerializeField] Sprite[] retratos;               // 0 = Neyder, 1 = Deisy
        [SerializeField] TMP_Text textoNombre;
        [SerializeField] RawImage pulso;
        [SerializeField] TMP_Text textoEstado;
        [SerializeField] Image[] corazones;
        [SerializeField] Sprite corazonLleno, corazonVacio;
        [SerializeField] TMP_Text textoMision;

        [Header("Objetos")]
        [SerializeField] RanuraInventario[] ranuras;
        [SerializeField] Sprite[] iconos;                 // índice = (int)TipoColeccionable
        [SerializeField] Image vistaPrevia;
        [SerializeField] TMP_Text textoNombreObjeto, textoDescripcion, textoAccion;

        [Header("Archivo")]
        [SerializeField] RectTransform listaNotas;
        [SerializeField] Button plantillaNota;
        [SerializeField] TMP_Text tituloNota, textoNota;

        [Header("Mapa")]
        [SerializeField] RawImage imagenMapa;
        [SerializeField] RectTransform marcadorJugador;
        [SerializeField] TMP_Text plantillaEtiqueta;

        readonly List<TipoColeccionable> contenido = new List<TipoColeccionable>();
        float escalaPrevia = 1f;
        bool mapaPreparado;

        void Awake()
        {
            Instancia = this;
            Abierta = false;
            raizCanvas.SetActive(false);
            for (int i = 0; i < ranuras.Length; i++) { ranuras[i].indice = i; ranuras[i].pantalla = this; }
            tabObjetos.onClick.AddListener(() => MostrarPagina(0));
            tabArchivo.onClick.AddListener(() => MostrarPagina(1));
            tabMapa.onClick.AddListener(() => MostrarPagina(2));
            botonCerrar.onClick.AddListener(Cerrar);
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            Abierta = false;
        }

        void Update()
        {
            if (Cinematica.EnCurso) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            bool alternar = (k != null && (k.tabKey.wasPressedThisFrame || k.iKey.wasPressedThisFrame)) ||
                            (g != null && g.startButton.wasPressedThisFrame);
            bool cancelar = k != null && k.escapeKey.wasPressedThisFrame;

            if (!Abierta)
            {
                if (alternar) Alternar();
                return;
            }
            if (alternar || cancelar) { Cerrar(); return; }
            if (k != null && (k.qKey.wasPressedThisFrame)) MostrarPagina((paginaActual + 2) % 3);
            if (k != null && (k.rKey.wasPressedThisFrame)) MostrarPagina((paginaActual + 1) % 3);

            // Línea de pulso: más rápida y de otro color según el estado
            var (estado, color) = HUDNexus.EstadoSalud();
            float velocidad = EstadoPartida.Salud >= EstadoPartida.SaludMaxima ? 0.35f : EstadoPartida.Salud == 2 ? 0.55f : 0.9f;
            var uv = pulso.uvRect; uv.x += Time.unscaledDeltaTime * velocidad; pulso.uvRect = uv;
            pulso.color = color;

            if (paginaActual == 2 && marcadorJugador != null)
                marcadorJugador.gameObject.SetActive(Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.55f);
        }

        /// <summary>Abre o cierra el inventario (también lo usa el botón táctil).</summary>
        public void Alternar()
        {
            if (Abierta) { Cerrar(); return; }
            if (HUDNexus.DialogoAbierto || Time.timeScale == 0f) return;
            if (NivelNexus.Instancia != null && NivelNexus.Instancia.Terminado) return;
            Abrir();
        }

        void Abrir()
        {
            Abierta = true;
            escalaPrevia = Time.timeScale;
            Time.timeScale = 0f;
            raizCanvas.SetActive(true);
            if (canvasHud != null) canvasHud.SetActive(false);
            HUDNexus.Prompt(null);
            RefrescarTecnico();
            MostrarPagina(0);
        }

        public void Cerrar()
        {
            if (!Abierta) return;
            Abierta = false;
            raizCanvas.SetActive(false);
            if (canvasHud != null) canvasHud.SetActive(true);
            if (NivelNexus.Instancia == null || !NivelNexus.Instancia.Terminado) Time.timeScale = escalaPrevia > 0f ? escalaPrevia : 1f;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        int paginaActual;

        void MostrarPagina(int pagina)
        {
            paginaActual = pagina;
            paginaObjetos.SetActive(pagina == 0);
            paginaArchivo.SetActive(pagina == 1);
            paginaMapa.SetActive(pagina == 2);
            var activo = new Color(0.4f, 0.95f, 1f);
            var inactivo = new Color(0.45f, 0.55f, 0.6f);
            tabObjetos.GetComponentInChildren<TMP_Text>().color = pagina == 0 ? activo : inactivo;
            tabArchivo.GetComponentInChildren<TMP_Text>().color = pagina == 1 ? activo : inactivo;
            tabMapa.GetComponentInChildren<TMP_Text>().color = pagina == 2 ? activo : inactivo;
            if (pagina == 0) RefrescarObjetos();
            if (pagina == 1) RefrescarArchivo();
            if (pagina == 2) RefrescarMapa();
        }

        // ---------------- Técnico ----------------
        void RefrescarTecnico()
        {
            int p = Mathf.Clamp(EstadoPartida.PersonajeElegido, 0, retratos.Length - 1);
            retrato.sprite = retratos[p];
            textoNombre.text = p == 0 ? "NEYDER\n<size=70%>TÉCNICO DE REDES</size>" : "DEISY\n<size=70%>ANALISTA DE SISTEMAS</size>";
            var (estado, color) = HUDNexus.EstadoSalud();
            textoEstado.text = "ESTADO: " + estado;
            textoEstado.color = color;
            for (int i = 0; i < corazones.Length; i++) corazones[i].sprite = i < EstadoPartida.Salud ? corazonLleno : corazonVacio;
            var nivel = NivelNexus.Instancia;
            float t = nivel != null ? nivel.TiempoRestante : 0f;
            textoMision.text =
                $"DISCOS MAESTROS   <color=#8ef3ff>{EstadoPartida.Discos}/{EstadoPartida.DiscosTotales}</color>\n" +
                $"CARTUCHOS         <color=#8ef3ff>{EstadoPartida.Refrigerantes}</color>\n" +
                $"REFRIGERACIÓN     <color=#8ef3ff>{(int)(t / 60f):00}:{(int)(t % 60f):00}</color>\n" +
                $"NOTAS LEÍDAS      <color=#8ef3ff>{EstadoPartida.NotasLeidas.Count}</color>";
        }

        // ---------------- Objetos ----------------
        void RefrescarObjetos()
        {
            contenido.Clear();
            if (EstadoPartida.Discos > 0) contenido.Add(TipoColeccionable.DiscoDuro);
            foreach (TipoColeccionable t in System.Enum.GetValues(typeof(TipoColeccionable)))
                if (t != TipoColeccionable.DiscoDuro && t != TipoColeccionable.Refrigerante && EstadoPartida.Cantidad(t) > 0)
                    contenido.Add(t);

            for (int i = 0; i < ranuras.Length; i++)
            {
                bool hay = i < contenido.Count;
                ranuras[i].icono.enabled = hay;
                if (hay)
                {
                    var t = contenido[i];
                    ranuras[i].icono.sprite = iconos[(int)t];
                    int n = t == TipoColeccionable.DiscoDuro ? EstadoPartida.Discos : EstadoPartida.Cantidad(t);
                    ranuras[i].texto.text = t == TipoColeccionable.DiscoDuro ? $"{n}/{EstadoPartida.DiscosTotales}" : (n > 1 ? $"x{n}" : "");
                }
                else ranuras[i].texto.text = "";
            }
            EventSystem.current?.SetSelectedGameObject(ranuras[0].gameObject);
            Seleccionar(0);
        }

        public void Seleccionar(int i)
        {
            if (i < contenido.Count)
            {
                var t = contenido[i];
                vistaPrevia.enabled = true;
                vistaPrevia.sprite = iconos[(int)t];
                textoNombreObjeto.text = DatosObjetos.Nombre(t).ToUpperInvariant();
                textoDescripcion.text = DatosObjetos.Descripcion(t);
                textoAccion.text = DatosObjetos.Usable(t) ? DatosObjetos.Accion(t) : "";
            }
            else
            {
                vistaPrevia.enabled = false;
                textoNombreObjeto.text = "— RANURA VACÍA —";
                textoDescripcion.text = "";
                textoAccion.text = "";
            }
        }

        public void Usar(int i)
        {
            if (i >= contenido.Count) return;
            var t = contenido[i];
            if (!DatosObjetos.Usable(t)) return;
            var jugador = GameObject.FindGameObjectWithTag("Player");
            if (DatosObjetos.EsArma(t))
            {
                var arma = jugador != null ? jugador.GetComponent<ArmaJugador>() : null;
                if (arma != null) arma.Equipar(ArmaJugador.DesdeObjeto(t));
                textoAccion.text = $"EQUIPASTE: {DatosObjetos.Nombre(t).ToUpperInvariant()}";
                return;
            }
            var vida = jugador != null ? jugador.GetComponent<VidaJugador>() : null;
            if (vida == null) return;
            int curar = t == TipoColeccionable.Chocorramo ? EstadoPartida.SaludMaxima : 1;
            if (!vida.Curar(curar))
            {
                textoAccion.text = "TUS OPORTUNIDADES YA ESTÁN AL MÁXIMO";
                return;
            }
            jugador.GetComponent<InventarioJugador>().Consumir(t);
            RefrescarTecnico();
            RefrescarObjetos();
            textoAccion.text = $"USASTE: {DatosObjetos.Nombre(t).ToUpperInvariant()}";
        }

        // ---------------- Archivo ----------------
        void RefrescarArchivo()
        {
            foreach (Transform hijo in listaNotas)
                if (hijo != plantillaNota.transform) Destroy(hijo.gameObject);
            plantillaNota.gameObject.SetActive(false);

            var notas = FindObjectsByType<NotaLore>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            tituloNota.text = EstadoPartida.NotasLeidas.Count == 0 ? "ARCHIVO VACÍO" : "SELECCIONA UNA NOTA";
            textoNota.text = EstadoPartida.NotasLeidas.Count == 0 ? "Las notas que leas quedarán guardadas aquí." : "";
            Button primero = null;
            foreach (var titulo in EstadoPartida.NotasLeidas)
            {
                var b = Instantiate(plantillaNota, listaNotas);
                b.gameObject.SetActive(true);
                b.GetComponentInChildren<TMP_Text>().text = titulo.ToUpperInvariant();
                string texto = null;
                foreach (var n in notas) if (n.Titulo == titulo) texto = n.Texto;
                string t = titulo, tx = texto ?? "(Nota de un nivel anterior)";
                b.onClick.AddListener(() => { tituloNota.text = t.ToUpperInvariant(); textoNota.text = tx; });
                if (primero == null) primero = b;
            }
            if (primero != null) { EventSystem.current?.SetSelectedGameObject(primero.gameObject); primero.onClick.Invoke(); }
        }

        // ---------------- Mapa ----------------
        void RefrescarMapa()
        {
            var mapa = FindFirstObjectByType<MapaNivel>();
            if (mapa == null || mapa.textura == null) return;
            imagenMapa.texture = mapa.textura;
            var rt = imagenMapa.rectTransform;
            // Ajusta el tamaño respetando la proporción del nivel
            float aspecto = mapa.tamanoMundo.x / mapa.tamanoMundo.y;
            float alto = 860f, ancho = alto * aspecto;
            if (ancho > 1080f) { ancho = 1080f; alto = ancho / aspecto; }
            rt.sizeDelta = new Vector2(ancho, alto);

            if (!mapaPreparado)
            {
                plantillaEtiqueta.gameObject.SetActive(false);
                for (int i = 0; i < mapa.nombresSalas.Length; i++)
                {
                    var e = Instantiate(plantillaEtiqueta, rt);
                    e.gameObject.SetActive(true);
                    e.text = mapa.nombresSalas[i].ToUpperInvariant();
                    var n = new Vector2(mapa.posicionesSalas[i].x / mapa.tamanoMundo.x, mapa.posicionesSalas[i].y / mapa.tamanoMundo.y);
                    e.rectTransform.anchorMin = e.rectTransform.anchorMax = n;
                    e.rectTransform.anchoredPosition = Vector2.zero;
                }
                mapaPreparado = true;
            }

            var jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
            {
                var n = mapa.Normalizar(jugador.transform.position);
                marcadorJugador.SetParent(rt, false);
                marcadorJugador.anchorMin = marcadorJugador.anchorMax = n;
                marcadorJugador.anchoredPosition = Vector2.zero;
                marcadorJugador.localRotation = Quaternion.Euler(0f, 0f, -jugador.transform.eulerAngles.y);
                marcadorJugador.SetAsLastSibling();
            }
            EventSystem.current?.SetSelectedGameObject(tabMapa.gameObject);
        }
    }
}
