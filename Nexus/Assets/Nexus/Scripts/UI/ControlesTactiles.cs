using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus
{
    /// <summary>
    /// Controles táctiles para Android y para el navegador del celular (bitácora 3.16). Se crean solos
    /// cuando el juego corre en un dispositivo móvil y solo se ven mientras el jugador tiene el control
    /// (no en el menú, cinemáticas, diálogos, inventario, ascensor ni pantalla final).
    ///
    /// Siguen el estilo del HUD: anillos finos y texto, sin recuadros.
    ///  - Izquierda: joystick que aparece donde se apoya el pulgar (arriba/abajo avanza y retrocede,
    ///    a los lados gira: los mismos controles tipo tanque), CORRER (se deja activado) e INVENTARIO.
    ///  - Derecha: INTERACTUAR, HABILIDAD y, según el arma, ATACAR o DISPARAR, APUNTAR (se deja
    ///    activado), RECARGAR y ARMA (cambia de arma).
    ///  - DISPARAR sin estar apuntando apunta solo al androide más cercano y dispara (apuntado asistido).
    /// Los botones usan las mismas entradas que las pruebas automáticas (PulsacionSimulada, etc.).
    /// </summary>
    public class ControlesTactiles : MonoBehaviour
    {
        public static Vector2 Mover { get; private set; }
        public static bool Correr { get; private set; }
        public static ControlesTactiles Instancia { get; private set; }
        /// <summary>Para pruebas: si los controles están a la vista.</summary>
        public static bool Visibles => Instancia != null && Instancia.grupo.alpha > 0.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { Mover = Vector2.zero; Correr = false; Instancia = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AlArrancar() { if (Application.isMobilePlatform) Crear(); }

        /// <summary>Crea los controles (en el celular se llama solo; en el editor, desde las pruebas).</summary>
        public static void Crear()
        {
            if (Instancia != null) return;
            var go = new GameObject("Controles_Tactiles");
            DontDestroyOnLoad(go);
            Instancia = go.AddComponent<ControlesTactiles>();
            Instancia.Construir();
        }

        static readonly Color ColorTexto = new Color(0.62f, 0.72f, 0.76f);
        static readonly Color ColorActivo = new Color(0.55f, 0.95f, 1f);

        CanvasGroup grupo;
        Joystick joystick;
        BotonTactil bDisparar, bApuntar, bRecargar, bArma, bInteractuar, bHabilidad, bCorrer, bInventario;
        Sprite anillo, disco;
        TMP_FontAsset fuente;
        ArmaJugador arma;
        InteraccionJugador interaccion;
        HabilidadJugador habilidad;
        VidaJugador vida;
        bool apuntarFijo;
        Coroutine disparoAsistido;

        void OnDestroy() { if (Instancia == this) { Instancia = null; Mover = Vector2.zero; Correr = false; } }

        // ======================= Construcción =======================
        void Construir()
        {
            anillo = Circulo(true); disco = Circulo(false);
            var lienzo = gameObject.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceOverlay; lienzo.sortingOrder = 5;
            var esc = gameObject.AddComponent<CanvasScaler>();
            esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; esc.referenceResolution = new Vector2(1920f, 1080f); esc.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            grupo = gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0f; grupo.blocksRaycasts = false; grupo.interactable = false;

            // Zona del joystick: la mitad izquierda baja de la pantalla
            var zona = NuevoRect("Zona_Joystick", transform, new Vector2(0f, 0f), new Vector2(0.42f, 0.78f), Vector2.zero, Vector2.zero);
            var img = zona.gameObject.AddComponent<Image>(); img.color = new Color(0f, 0f, 0f, 0f);
            joystick = zona.gameObject.AddComponent<Joystick>();
            var baseJ = Imagen("Base", zona, anillo, 300f, new Color(1f, 1f, 1f, 0.22f));
            var perilla = Imagen("Perilla", baseJ, disco, 120f, new Color(0.55f, 0.95f, 1f, 0.28f));
            joystick.Configurar(zona, baseJ, perilla, new Vector2(300f, 290f), 125f);

            // Izquierda
            bCorrer = Boton("CORRER", new Vector2(0f, 0f), new Vector2(560f, 120f), 120f);
            bCorrer.alPulsar = () => { Correr = !Correr; bCorrer.Activo = Correr; };
            bInventario = Boton("INVENTARIO", new Vector2(0f, 0f), new Vector2(120f, 640f), 130f);
            bInventario.alPulsar = () => { if (PantallaInventario.Instancia != null) PantallaInventario.Instancia.Alternar(); };

            // Derecha (posiciones desde la esquina inferior derecha)
            var der = new Vector2(1f, 0f);
            bDisparar = Boton("DISPARAR", der, new Vector2(-210f, 215f), 200f);
            bDisparar.alPulsar = Disparar;
            bApuntar = Boton("APUNTAR", der, new Vector2(-430f, 125f), 135f);
            bApuntar.alPulsar = () => { apuntarFijo = !apuntarFijo; if (arma != null) arma.ApuntarSimulado = apuntarFijo; bApuntar.Activo = apuntarFijo; };
            bInteractuar = Boton("INTERACTUAR", der, new Vector2(-410f, 345f), 160f);
            bInteractuar.alPulsar = () => { if (interaccion != null) interaccion.PulsacionSimulada = true; };
            bHabilidad = Boton("HABILIDAD", der, new Vector2(-610f, 230f), 130f);
            bHabilidad.alPulsar = () => { if (habilidad != null) habilidad.PulsacionSimulada = true; };
            bRecargar = Boton("RECARGAR", der, new Vector2(-120f, 445f), 115f);
            bRecargar.alPulsar = () => { if (arma != null) arma.RecargaSimulada = true; };
            bArma = Boton("ARMA", der, new Vector2(-300f, 520f), 115f);
            bArma.alPulsar = () => { if (arma != null) arma.Ciclar(1); };
        }

        /// <summary>Atacar con el cuchillo o disparar; sin estar apuntando, apunta solo y dispara.</summary>
        void Disparar()
        {
            if (arma == null) return;
            if (arma.Equipada == TipoArma.Cuchillo || arma.Apuntando) { arma.DisparoSimulado = true; return; }
            if (disparoAsistido == null) disparoAsistido = StartCoroutine(ApuntarYDisparar());
        }

        IEnumerator ApuntarYDisparar()
        {
            arma.ApuntarSimulado = true;
            float limite = Time.time + 0.3f;
            while (arma != null && !arma.Apuntando && Time.time < limite) yield return null;
            yield return new WaitForSeconds(0.12f);   // el tiempo de girar hacia el objetivo
            if (arma != null) arma.DisparoSimulado = true;
            yield return new WaitForSeconds(0.6f);
            if (arma != null && !apuntarFijo) arma.ApuntarSimulado = false;
            disparoAsistido = null;
        }

        // ======================= Cada cuadro =======================
        void Update()
        {
            if (arma == null || interaccion == null)
            {
                var j = GameObject.FindGameObjectWithTag("Player");
                if (j != null)
                {
                    arma = j.GetComponent<ArmaJugador>(); interaccion = j.GetComponent<InteraccionJugador>();
                    habilidad = j.GetComponent<HabilidadJugador>(); vida = j.GetComponent<VidaJugador>();
                    apuntarFijo = false; bApuntar.Activo = false;
                }
                if (fuente == null)
                {
                    var hud = FindFirstObjectByType<HUDNexus>();
                    var t = hud != null ? hud.GetComponentInChildren<TMP_Text>(true) : null;
                    if (t != null) { fuente = t.font; foreach (var b in GetComponentsInChildren<BotonTactil>(true)) b.Fuente(fuente); }
                }
            }

            var nivel = NivelNexus.Instancia;
            bool mostrar = nivel != null && !nivel.Terminado && arma != null && !Cinematica.EnCurso && !TransicionAscensor.EnCurso
                           && !PantallaInventario.Abierta && !HUDNexus.DialogoAbierto && (vida == null || !vida.Muerto);
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, mostrar ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            grupo.blocksRaycasts = grupo.interactable = mostrar;
            if (!mostrar) { joystick.Soltar(); Mover = Vector2.zero; }
            else Mover = joystick.Valor;
            if (arma == null) return;

            // Los botones del arma cambian según lo que se tenga en la mano
            bool fuego = arma.Equipada == TipoArma.Pistola || arma.Equipada == TipoArma.Escopeta;
            bDisparar.gameObject.SetActive(arma.Equipada != TipoArma.Ninguna);
            bDisparar.Texto = fuego ? "DISPARAR" : "ATACAR";
            bApuntar.gameObject.SetActive(fuego);
            bRecargar.gameObject.SetActive(fuego);
            bArma.gameObject.SetActive(arma.TieneArma(TipoArma.Cuchillo) || arma.TieneArma(TipoArma.Pistola) || arma.TieneArma(TipoArma.Escopeta));
            if (!fuego && apuntarFijo) { apuntarFijo = false; arma.ApuntarSimulado = false; bApuntar.Activo = false; }
            bHabilidad.Atenuado = habilidad != null && !habilidad.Lista;
        }

        // ======================= Piezas =======================
        static RectTransform NuevoRect(string nombre, Transform padre, Vector2 min, Vector2 max, Vector2 pos, Vector2 tam)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = tam;
            return rt;
        }

        static RectTransform Imagen(string nombre, RectTransform padre, Sprite sprite, float diametro, Color color)
        {
            var rt = NuevoRect(nombre, padre, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * diametro);
            var i = rt.gameObject.AddComponent<Image>();
            i.sprite = sprite; i.color = color; i.raycastTarget = false;
            return rt;
        }

        BotonTactil Boton(string texto, Vector2 ancla, Vector2 pos, float diametro)
        {
            var rt = NuevoRect("Boton_" + texto, transform, ancla, ancla, pos, Vector2.one * diametro);
            var b = rt.gameObject.AddComponent<BotonTactil>();
            b.Construir(texto, anillo, disco, diametro);
            return b;
        }

        /// <summary>Círculo suavizado de 128 px: anillo fino o disco lleno.</summary>
        static Sprite Circulo(bool soloBorde)
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            float r = n / 2f - 1.5f, grosor = 3.2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                    float a = soloBorde ? Mathf.Clamp01(1f - Mathf.Abs(d - (r - grosor / 2f)) / (grosor / 2f) * 0.85f)
                                        : Mathf.Clamp01(r - d + 0.5f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        // ======================= Joystick =======================
        /// <summary>Joystick flotante: aparece donde se apoya el dedo dentro de su zona.</summary>
        class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            RectTransform zona, baseJ, perilla;
            Vector2 reposo; float radio;
            int dedo = int.MinValue;
            public Vector2 Valor { get; private set; }

            public void Configurar(RectTransform z, RectTransform b, RectTransform p, Vector2 posReposo, float r)
            {
                zona = z; baseJ = b; perilla = p; radio = r;
                // Posición de reposo, medida desde la esquina inferior izquierda de la zona
                baseJ.anchorMin = baseJ.anchorMax = Vector2.zero;
                reposo = posReposo; baseJ.anchoredPosition = reposo;
            }

            Vector2 Local(PointerEventData e)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(zona, e.position, e.pressEventCamera, out var l);
                return l - zona.rect.min;   // desde la esquina inferior izquierda
            }

            public void OnPointerDown(PointerEventData e)
            {
                if (dedo != int.MinValue) return;
                dedo = e.pointerId;
                baseJ.anchoredPosition = Local(e);
                Arrastrar(e);
            }

            public void OnDrag(PointerEventData e) { if (e.pointerId == dedo) Arrastrar(e); }
            public void OnPointerUp(PointerEventData e) { if (e.pointerId == dedo) Soltar(); }

            void Arrastrar(PointerEventData e)
            {
                var d = Vector2.ClampMagnitude(Local(e) - baseJ.anchoredPosition, radio);
                perilla.anchoredPosition = d;
                var v = d / radio;
                Valor = v.magnitude < 0.12f ? Vector2.zero : v;   // zona muerta
            }

            public void Soltar()
            {
                dedo = int.MinValue; Valor = Vector2.zero;
                if (perilla != null) perilla.anchoredPosition = Vector2.zero;
                if (baseJ != null) baseJ.anchoredPosition = reposo;
            }
        }

        // ======================= Botón =======================
        /// <summary>Anillo con texto: se ilumina al tocarlo; los que se dejan activados quedan en cian.</summary>
        class BotonTactil : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public Action alPulsar;
            Image borde, relleno;
            TMP_Text etiqueta;
            bool presionado, activo, atenuado;

            public bool Activo { get => activo; set { activo = value; Pintar(); } }
            public bool Atenuado { get => atenuado; set { if (atenuado != value) { atenuado = value; Pintar(); } } }
            public string Texto { set { if (etiqueta.text != value) etiqueta.text = value; } }

            public void Construir(string texto, Sprite anillo, Sprite disco, float diametro)
            {
                var rt = (RectTransform)transform;
                relleno = gameObject.AddComponent<Image>();   // el disco recibe los toques
                relleno.sprite = disco;
                borde = Imagen("Borde", rt, anillo, diametro, Color.white).GetComponent<Image>();
                var tr = NuevoRect("Texto", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(diametro * 1.3f, 40f));
                etiqueta = tr.gameObject.AddComponent<TextMeshProUGUI>();
                etiqueta.text = texto; etiqueta.alignment = TextAlignmentOptions.Center; etiqueta.raycastTarget = false;
                etiqueta.fontSize = diametro >= 180f ? 30f : diametro >= 140f ? 24f : 21f;
                etiqueta.characterSpacing = 2f; etiqueta.fontStyle = FontStyles.Bold;
                etiqueta.textWrappingMode = TextWrappingModes.NoWrap;
                Pintar();
            }

            public void Fuente(TMP_FontAsset f) { if (f != null) etiqueta.font = f; }

            public void OnPointerDown(PointerEventData e) { presionado = true; Pintar(); alPulsar?.Invoke(); }
            public void OnPointerUp(PointerEventData e) { presionado = false; Pintar(); }
            void OnDisable() { presionado = false; }

            void Pintar()
            {
                var c = activo ? ColorActivo : ColorTexto;
                float a = atenuado ? 0.35f : 1f;
                borde.color = new Color(c.r, c.g, c.b, (presionado ? 0.95f : activo ? 0.8f : 0.45f) * a);
                relleno.color = new Color(c.r, c.g, c.b, presionado ? 0.22f : activo ? 0.1f : 0.03f);
                etiqueta.color = new Color(c.r, c.g, c.b, (presionado || activo ? 1f : 0.85f) * a);
            }
        }
    }
}
