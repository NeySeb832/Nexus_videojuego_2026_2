using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nexus
{
    /// <summary>
    /// Transición entre niveles por el ascensor (bitácora 6.11), en tres partes:
    ///  1. Entrar: las puertas se recogen hacia los lados, sale la luz de la cabina y el técnico camina
    ///     hacia ella; fundido a negro.
    ///  2. Viaje: dentro de una cabina de montacargas (construida con primitivas lejos del nivel), con
    ///     cámara fija en una esquina del techo como en los survival horror clásicos. Arranca con un tirón,
    ///     el indicador baja de piso, la luz falla, por la junta de las puertas pasan las luces de cada piso
    ///     y frena de golpe. Se puede saltar con Enter, Esc, Espacio, Start o tocando la pantalla.
    ///  3. Llegada: el nivel siguiente se carga detrás de la pantalla negra y aparece con el ruido de las
    ///     puertas al abrirse.
    /// El objeto sobrevive al cambio de escena (DontDestroyOnLoad) solo para sostener la pantalla negra.
    /// </summary>
    public class TransicionAscensor : MonoBehaviour
    {
        public static bool EnCurso => instancia != null;
        /// <summary>Para pruebas: en qué parte va ("entrar", "viaje", "cargando", "llegada").</summary>
        public static string Fase { get; private set; } = "";
        /// <summary>Para pruebas automáticas: salta el viaje.</summary>
        public static bool SaltoSimulado { get; set; }
        static TransicionAscensor instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { instancia = null; Fase = ""; SaltoSimulado = false; }

        // Piso y nombre de cada nivel: el centro de datos baja hacia el núcleo
        static (string piso, string nombre) Info(string escena) => escena switch
        {
            "01_Mantenimiento" => ("-1", "NIVEL 1 — CUARTO DE MANTENIMIENTO"),
            "02_Almacenamiento" => ("-2", "NIVEL 2 — PASILLOS DE ALMACENAMIENTO"),
            "03_Nucleo" => ("-3", "NIVEL 3 — NÚCLEO DE DATOS"),
            _ => ("?", escena)
        };

        const float AltoBarras = 130f;

        Transform jugador;
        ControladorJugador control;
        Canvas lienzo;
        Image negro;
        RectTransform barraSup, barraInf;
        TMP_Text textoNivel, textoSaltar;
        TMP_FontAsset fuente;
        AudioSource motor;
        bool saltar;

        /// <summary>Empieza la transición desde el ascensor de salida hacia la escena indicada.</summary>
        public static void Iniciar(Transform ascensor, string escenaSiguiente)
        {
            if (instancia != null) return;
            var go = new GameObject("Transicion_Ascensor");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<TransicionAscensor>();
            instancia.StartCoroutine(instancia.Secuencia(ascensor, escenaSiguiente));
        }

        void OnDestroy() { if (instancia == this) { instancia = null; Fase = ""; } }

        IEnumerator Secuencia(Transform ascensor, string escena)
        {
            var j = GameObject.FindGameObjectWithTag("Player");
            if (j == null || ascensor == null) { SceneManager.LoadScene(escena); Destroy(gameObject); yield break; }
            jugador = j.transform; control = j.GetComponent<ControladorJugador>();

            // Mientras dura: sin interacción, armas, inventario ni habilidad; androides y temporizador quietos
            Cinematica.EnCurso = true;
            AndroideIA.Pausados = true;
            if (PantallaInventario.Abierta) PantallaInventario.Instancia.Cerrar();
            var gestor = GestorCamaras.Instancia;
            if (gestor != null) gestor.Pausar();
            var hud = FindFirstObjectByType<HUDNexus>();
            if (hud != null)
            {
                var t = hud.GetComponentInChildren<TMP_Text>(true); if (t != null) fuente = t.font;
                foreach (var cv in hud.GetComponentsInChildren<Canvas>()) cv.enabled = false;
            }
            CrearLienzo();
            var (pisoOrigen, _) = Info(SceneManager.GetActiveScene().name);
            var (pisoDestino, nombreDestino) = Info(escena);

            // ================= 1. Entrar =================
            Fase = "entrar";
            var cam = CrearCamara("Cam_Ascensor_Entrada");
            var (desde, hacia, fov) = PlanoEntrada(ascensor);
            cam.fieldOfView = fov;
            Vector3 mirar = ascensor.TransformPoint(new Vector3(0f, 1.25f, -0.4f));
            StartCoroutine(Barras(true, 0.45f));
            var puertas = new PuertasAscensor(ascensor);
            AudioNexus.Efecto(Sfx.AscensorPuertas, ascensor.TransformPoint(new Vector3(0f, 1.3f, -0.3f)), 1f, 0f);
            Vector3 meta = ascensor.TransformPoint(new Vector3(0f, 0f, -0.5f));
            control.PuedeMoverse = true; control.CorrerSimulado = false;
            float durEntrada = 3.6f, tiempo = 0f;
            bool llego = false;
            while (tiempo < durEntrada && !llego)
            {
                tiempo += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, tiempo / durEntrada);
                cam.transform.position = Vector3.Lerp(desde, hacia, k);
                cam.transform.rotation = Quaternion.LookRotation(mirar - cam.transform.position);
                puertas.Abrir(tiempo / 1.4f);
                // Camina (con los mismos controles tipo tanque) cuando las puertas ya van abiertas
                if (tiempo > 0.7f)
                {
                    Vector3 d = meta - jugador.position; d.y = 0f;
                    llego = d.magnitude < 0.35f && tiempo > 1.6f;
                    float ang = Vector3.SignedAngle(jugador.forward, d, Vector3.up);
                    control.EntradaSimulada = d.magnitude < 0.35f ? Vector2.zero
                        : new Vector2(Mathf.Clamp(ang / 25f, -1f, 1f), Mathf.Abs(ang) < 60f ? 0.75f : 0f);
                }
                else control.EntradaSimulada = Vector2.zero;
                yield return null;
            }
            control.EntradaSimulada = Vector2.zero;
            AudioNexus.Amortiguado = true;
            yield return Fundir(1f, 0.55f);
            Destroy(cam.gameObject);

            // ================= 2. Viaje =================
            Fase = "viaje";
            var cabina = new CabinaAscensor(ascensor, jugador.position + Vector3.down * 300f, fuente, pisoOrigen);
            NivelNexus.Teletransportar(jugador, cabina.PosicionJugador);
            jugador.rotation = Quaternion.LookRotation(Vector3.back);
            var ik = jugador.GetComponentInChildren<IKTecnico>();
            if (ik != null) ik.MirarAlrededor(4.5f);
            cam = CrearCamara("Cam_Ascensor_Cabina");
            cam.fieldOfView = 64f;
            cam.transform.SetPositionAndRotation(cabina.Camara, Quaternion.LookRotation(cabina.Mirar - cabina.Camara));
            textoNivel.text = nombreDestino;
            textoNivel.alpha = 0f;
            motor = gameObject.AddComponent<AudioSource>();
            motor.clip = AudioNexus.Clip(Sfx.AscensorMarcha); motor.spatialBlend = 0f; motor.volume = 0.9f; motor.ignoreListenerPause = true;
            if (motor.clip != null) motor.Play();
            StartCoroutine(Fundir(0f, 0.6f));

            const float durViaje = 6.6f, frenada = 5.1f, cambioPiso = 3.2f;
            bool frenado = false;
            saltar = false;
            for (float t = 0f; t < durViaje; t += Time.deltaTime)
            {
                if (t > 0.3f && PulsoSaltar()) { saltar = true; break; }
                // Tirón al arrancar, vibración constante del motor y golpe al frenar
                float tiron = Mathf.Exp(-6f * t) * 0.045f + (t > frenada ? Mathf.Exp(-5f * (t - frenada)) * 0.06f : 0f);
                float vib = frenado ? 0.002f : 0.006f;
                Vector3 temblor = new Vector3(Mathf.PerlinNoise(t * 9f, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t * 11f) - 0.5f, 0f) * 2f * (vib + tiron);
                cam.transform.position = cabina.Camara + cam.transform.rotation * temblor;
                cabina.Actualizar(t, t >= cambioPiso ? pisoDestino : pisoOrigen, t >= cambioPiso && t < cambioPiso + 0.9f, frenado);
                textoNivel.alpha = Mathf.Clamp01((t - 1f) / 0.8f) * (1f - Mathf.Clamp01((t - durViaje + 0.6f) / 0.6f));
                if (!frenado && t >= frenada)
                {
                    frenado = true;
                    AudioNexus.Interfaz(Sfx.AscensorParada, 1f);
                    StartCoroutine(BajarMotor(0.5f));
                }
                if (t > durViaje - 0.6f && negro.color.a < 0.01f) StartCoroutine(Fundir(1f, 0.55f));
                yield return null;
            }
            if (saltar) { StartCoroutine(BajarMotor(0.3f)); yield return Fundir(1f, 0.3f); }
            while (negro.color.a < 0.99f) yield return null;
            textoNivel.alpha = 0f;

            // ================= 3. Llegada =================
            Fase = "cargando";
            var op = SceneManager.LoadSceneAsync(escena);
            while (!op.isDone) yield return null;
            yield return null; yield return null;   // deja arrancar el nivel nuevo (NivelNexus, HUD, cámaras)
            AndroideIA.Pausados = false;
            if (Cinematica.Actual == null) Cinematica.EnCurso = false;
            AudioNexus.Amortiguado = false;
            Fase = "llegada";
            AudioNexus.Interfaz(Sfx.AscensorPuertas, 0.8f);
            StartCoroutine(Barras(false, 0.9f));
            yield return Fundir(0f, 1.1f);
            Destroy(gameObject);
        }

        /// <summary>El plano de la entrada: la cámara de interés del ascensor que ya armó el constructor del
        /// nivel; si no hay, uno calculado frente a las puertas que no atraviese paredes.</summary>
        static (Vector3 desde, Vector3 hacia, float fov) PlanoEntrada(Transform ascensor)
        {
            Camera mejor = null; float dmin = 12f;
            foreach (var c in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!c.name.Contains("Interes_Ascensor")) continue;
                float d = Vector3.Distance(c.transform.position, ascensor.position);
                if (d < dmin) { dmin = d; mejor = c; }
            }
            Vector3 puerta = ascensor.TransformPoint(new Vector3(0f, 1.25f, -0.4f));
            if (mejor != null)
            {
                var p = mejor.transform.position;
                return (p, Vector3.Lerp(p, puerta, 0.12f), mejor.fieldOfView);
            }
            var deseada = ascensor.TransformPoint(new Vector3(1.1f, 1.9f, -3.8f));
            var dir = deseada - puerta;
            if (Physics.Raycast(puerta, dir.normalized, out var hit, dir.magnitude, ~(1 << 2), QueryTriggerInteraction.Ignore)
                && !hit.transform.CompareTag("Player"))
                deseada = hit.point - dir.normalized * 0.25f;
            return (deseada, Vector3.Lerp(deseada, puerta, 0.12f), 52f);
        }

        IEnumerator BajarMotor(float dur)
        {
            if (motor == null) yield break;
            float v0 = motor.volume;
            for (float t = 0f; t < 1f && motor != null; t += Time.deltaTime / dur) { motor.volume = v0 * (1f - t); yield return null; }
            if (motor != null) motor.Stop();
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

        // ======================= Cámara y pantalla =======================
        static Camera CrearCamara(string nombre)
        {
            var go = new GameObject(nombre);
            var c = go.AddComponent<Camera>();
            c.nearClipPlane = 0.05f; c.farClipPlane = 60f; c.depth = 20f;
            c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = Color.black;
            c.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            return c;
        }

        void CrearLienzo()
        {
            var lg = new GameObject("Lienzo_Ascensor");
            lg.transform.SetParent(transform, false);
            lienzo = lg.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceOverlay; lienzo.sortingOrder = 70;
            var esc = lg.AddComponent<CanvasScaler>();
            esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; esc.referenceResolution = new Vector2(1920f, 1080f); esc.matchWidthOrHeight = 0.5f;

            negro = Rect("Negro", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            negro.color = new Color(0f, 0f, 0f, 0f); negro.raycastTarget = false;
            barraSup = Rect("Barra_Sup", new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f));
            barraInf = Rect("Barra_Inf", Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            barraSup.gameObject.AddComponent<Image>().color = Color.black;
            barraInf.gameObject.AddComponent<Image>().color = Color.black;
            barraSup.sizeDelta = barraInf.sizeDelta = Vector2.zero;
            // El negro va encima de las barras y los textos encima de todo
            negro.transform.SetAsLastSibling();

            textoNivel = Texto("Nivel", 34f, new Vector2(0f, 62f), new Vector2(1500f, 60f));
            textoNivel.characterSpacing = 6f;
            textoNivel.color = new Color(0.78f, 0.93f, 1f);
            textoSaltar = Texto("Saltar", 22f, new Vector2(-40f, 30f), new Vector2(360f, 40f));
            var rs = (RectTransform)textoSaltar.transform;
            rs.anchorMin = rs.anchorMax = rs.pivot = new Vector2(1f, 0f);
            textoSaltar.alignment = TextAlignmentOptions.Right;
            textoSaltar.color = new Color(1f, 1f, 1f, 0f);
        }

        RectTransform Rect(string nombre, Vector2 min, Vector2 max, Vector2? pivote = null)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(lienzo.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivote ?? new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        TMP_Text Texto(string nombre, float tam, Vector2 pos, Vector2 caja)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(lienzo.transform, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (fuente != null) t.font = fuente;
            t.fontSize = tam; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos; rt.sizeDelta = caja;
            return t;
        }

        IEnumerator Fundir(float hasta, float dur)
        {
            float desde = negro.color.a;
            for (float t = 0f; t < 1f; t += Time.deltaTime / dur)
            {
                negro.color = new Color(0f, 0f, 0f, Mathf.Lerp(desde, hasta, t));
                yield return null;
            }
            negro.color = new Color(0f, 0f, 0f, hasta);
        }

        IEnumerator Barras(bool entrar, float dur)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / dur)
            {
                float h = Mathf.SmoothStep(0f, 1f, entrar ? t : 1f - t) * AltoBarras;
                barraSup.sizeDelta = barraInf.sizeDelta = new Vector2(0f, h);
                textoSaltar.color = new Color(1f, 1f, 1f, entrar ? 0.45f * t : 0f);
                yield return null;
            }
            barraSup.sizeDelta = barraInf.sizeDelta = new Vector2(0f, entrar ? AltoBarras : 0f);
            textoSaltar.text = entrar ? "ENTER / ESC  saltar" : "";
        }

        // ======================= Puertas del ascensor del nivel =======================
        /// <summary>
        /// El prefab tiene una sola hoja de puerta: aquí se reemplaza por dos que se recogen hacia los marcos
        /// (el ascensor está pegado a la pared, así que no hay hueco donde esconderlas), y detrás aparece la
        /// cabina oscura con su luz.
        /// </summary>
        class PuertasAscensor
        {
            readonly Transform izq, der;
            readonly Light luz;
            readonly float ancho, alto, grosor, z;

            public PuertasAscensor(Transform ascensor)
            {
                var hoja = ascensor.Find("Puertas");
                if (hoja == null) return;
                ancho = hoja.localScale.x; alto = hoja.localScale.y; grosor = hoja.localScale.z; z = hoja.localPosition.z;
                var mat = hoja.GetComponent<Renderer>().sharedMaterial;
                hoja.GetComponent<Renderer>().enabled = false;
                var junta = ascensor.Find("Junta");
                if (junta != null) junta.gameObject.SetActive(false);
                izq = Cubo("Hoja_Izq", ascensor, mat); der = Cubo("Hoja_Der", ascensor, mat);
                Abrir(0f);

                // Dónde empieza la pared detrás de las puertas (en los niveles queda a ~2 cm): la cabina oscura
                // se dibuja justo delante, si no la pared la taparía
                float zPared = z + grosor / 2f;
                var desde = ascensor.TransformPoint(new Vector3(0f, alto / 2f, -1.5f));
                float dmin = float.MaxValue;
                foreach (var h in Physics.RaycastAll(desde, ascensor.forward, 4f, ~0, QueryTriggerInteraction.Ignore))
                    if (!h.transform.IsChildOf(ascensor) && h.distance < dmin) { dmin = h.distance; zPared = ascensor.InverseTransformPoint(h.point).z; }
                float zFondo = Mathf.Min(z + grosor / 2f + 0.03f, zPared - 0.008f);

                // Cabina oscura detrás y la franja de luz de su techo
                var neon = ascensor.Find("Luz_Cartel")?.GetComponent<Renderer>()?.sharedMaterial;
                var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(fondo.GetComponent<Collider>());
                fondo.name = "Cabina_Fondo"; fondo.transform.SetParent(ascensor, false);
                fondo.transform.localPosition = new Vector3(0f, alto / 2f, zFondo);
                fondo.transform.localScale = new Vector3(ancho, alto, 1f);
                // Negro mate y sin la textura de remaches: debe leerse como un hueco, no como una pared
                var mf = new Material(mat); Pintar(mf, new Color(0.012f, 0.012f, 0.014f), Color.black);
                foreach (var tex in new[] { "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" })
                    if (mf.HasProperty(tex)) mf.SetTexture(tex, null);
                if (mf.HasProperty("_Smoothness")) mf.SetFloat("_Smoothness", 0f);
                if (mf.HasProperty("_Metallic")) mf.SetFloat("_Metallic", 0f);
                mf.DisableKeyword("_NORMALMAP");
                fondo.GetComponent<Renderer>().sharedMaterial = mf;
                if (neon != null)
                {
                    var franja = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Object.Destroy(franja.GetComponent<Collider>());
                    franja.name = "Cabina_Luz"; franja.transform.SetParent(ascensor, false);
                    franja.transform.localPosition = new Vector3(0f, alto - 0.12f, zFondo - 0.004f);
                    franja.transform.localScale = new Vector3(ancho * 0.8f, 0.07f, 1f);
                    var mn = new Material(neon); Pintar(mn, new Color(1f, 0.92f, 0.78f), new Color(1f, 0.9f, 0.75f) * 2.2f);
                    franja.GetComponent<Renderer>().sharedMaterial = mn;
                }
                // La luz de la cabina sale al pasillo a medida que se abren las puertas
                var lgo = new GameObject("Cabina_Luz_Punto");
                lgo.transform.SetParent(ascensor, false);
                lgo.transform.localPosition = new Vector3(0f, alto - 0.3f, -0.45f);
                luz = lgo.AddComponent<Light>();
                luz.type = LightType.Point; luz.range = 4.5f; luz.intensity = 0f; luz.color = new Color(1f, 0.9f, 0.75f);
            }

            Transform Cubo(string nombre, Transform padre, Material mat)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(c.GetComponent<Collider>());
                c.name = nombre; c.transform.SetParent(padre, false);
                c.GetComponent<Renderer>().sharedMaterial = mat;
                return c.transform;
            }

            /// <summary>k = 0 cerrada, 1 abierta: cada hoja se encoge hacia su marco.</summary>
            public void Abrir(float k)
            {
                if (izq == null) return;
                k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
                float mitad = ancho / 2f, w = Mathf.Lerp(mitad, 0.06f, k);
                foreach (var (h, lado) in new[] { (izq, -1f), (der, 1f) })
                {
                    h.localScale = new Vector3(w, alto, grosor);
                    h.localPosition = new Vector3(lado * (mitad - w / 2f), alto / 2f, z);
                }
                // La luz parpadea al arrancar, como un tubo fluorescente viejo
                if (luz != null) luz.intensity = k * 1.8f * (k < 0.6f && Mathf.PerlinNoise(Time.time * 18f, 0f) < 0.35f ? 0.25f : 1f);
            }
        }

        static void Pintar(Material m, Color baseColor, Color emision)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
            if (m.HasProperty("_Color")) m.SetColor("_Color", baseColor);
            if (m.HasProperty("_EmissionColor"))
            {
                m.SetColor("_EmissionColor", emision);
                if (emision.maxColorComponent > 0f) m.EnableKeyword("_EMISSION"); else m.DisableKeyword("_EMISSION");
            }
        }

        // ======================= Cabina del viaje =======================
        /// <summary>
        /// Cabina de montacargas de 2,4 x 2,4 m hecha con primitivas y los materiales del propio ascensor
        /// (metal de las puertas, chasis de los racks, franjas de peligro, panel de terminal). Se arma 300 m
        /// por debajo del nivel, donde no llega ninguna otra luz, y desaparece al cargar la escena siguiente.
        /// </summary>
        class CabinaAscensor
        {
            public Vector3 PosicionJugador => o + new Vector3(0.2f, 0.05f, 0.35f);
            public Vector3 Camara => o + new Vector3(0.98f, 2.38f, -0.98f);
            public Vector3 Mirar => o + new Vector3(-0.25f, 1.15f, 0.45f);

            readonly Vector3 o;
            const float IntensidadLuz = 3.4f;
            readonly Light luz, luzPiso;
            readonly Renderer panelTecho;
            readonly Material matTecho;
            readonly Transform franjaPiso;
            readonly TextMeshPro numero, estado;
            readonly Color emisionTecho = new Color(0.85f, 0.92f, 1f) * 1.6f;

            public CabinaAscensor(Transform ascensor, Vector3 origen, TMP_FontAsset fuenteUI, string piso)
            {
                o = origen;
                var raiz = new GameObject("Cabina_Ascensor").transform;
                raiz.position = o;
                Material M(string hijo) => ascensor.Find(hijo)?.GetComponent<Renderer>()?.sharedMaterial;
                Material metal = M("Puertas"), chasis = M("Dintel") ?? metal, peligro = M("Marco_Izq") ?? metal,
                         terminal = M("Boton") ?? chasis, neon = M("Luz_Cartel") ?? metal;

                void Caja(string n, Vector3 p, Vector3 s, Material m)
                {
                    var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.name = n; c.transform.SetParent(raiz, false);
                    c.transform.localPosition = p; c.transform.localScale = s;
                    c.GetComponent<Renderer>().sharedMaterial = m;
                }
                Caja("Piso", new Vector3(0f, -0.05f, 0f), new Vector3(2.6f, 0.1f, 2.6f), chasis);
                Caja("Techo", new Vector3(0f, 2.75f, 0f), new Vector3(2.6f, 0.1f, 2.6f), chasis);
                Caja("Pared_Fondo", new Vector3(0f, 1.35f, 1.25f), new Vector3(2.6f, 2.8f, 0.1f), metal);
                Caja("Pared_Izq", new Vector3(-1.25f, 1.35f, 0f), new Vector3(0.1f, 2.8f, 2.6f), metal);
                Caja("Pared_Der", new Vector3(1.25f, 1.35f, 0f), new Vector3(0.1f, 2.8f, 2.6f), metal);
                // Puertas cerradas con una junta de 3 cm por donde se ven pasar los pisos
                Caja("Puerta_Izq", new Vector3(-0.6075f, 1.3f, -1.25f), new Vector3(1.185f, 2.6f, 0.08f), metal);
                Caja("Puerta_Der", new Vector3(0.6075f, 1.3f, -1.25f), new Vector3(1.185f, 2.6f, 0.08f), metal);
                Caja("Dintel", new Vector3(0f, 2.66f, -1.25f), new Vector3(2.5f, 0.18f, 0.1f), chasis);
                Caja("Zocalo_Peligro", new Vector3(0f, 0.006f, -1.08f), new Vector3(2.3f, 0.012f, 0.16f), peligro);
                Caja("Pasamanos", new Vector3(0f, 1.0f, 1.16f), new Vector3(2.1f, 0.05f, 0.05f), peligro);
                Caja("Pasamanos_Izq", new Vector3(-1.16f, 1.0f, 0.2f), new Vector3(0.05f, 0.05f, 1.6f), peligro);
                Caja("Panel", new Vector3(-1.18f, 1.35f, -0.25f), new Vector3(0.04f, 0.62f, 0.34f), terminal);

                // Luz del techo: un panel que brilla y una luz puntual que falla de vez en cuando
                var techo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(techo.GetComponent<Collider>());
                techo.name = "Panel_Techo"; techo.transform.SetParent(raiz, false);
                techo.transform.localPosition = new Vector3(0f, 2.69f, 0.15f);
                techo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                techo.transform.localScale = new Vector3(1.3f, 0.55f, 1f);
                matTecho = new Material(neon); Pintar(matTecho, new Color(0.9f, 0.95f, 1f), emisionTecho);
                panelTecho = techo.GetComponent<Renderer>(); panelTecho.sharedMaterial = matTecho;
                var lgo = new GameObject("Luz_Cabina"); lgo.transform.SetParent(raiz, false);
                lgo.transform.localPosition = new Vector3(0f, 2.35f, 0.15f);
                luz = lgo.AddComponent<Light>();
                luz.type = LightType.Point; luz.range = 6f; luz.intensity = IntensidadLuz; luz.color = new Color(0.85f, 0.92f, 1f);

                // La luz de cada piso que pasa por fuera: barre la cabina de abajo hacia arriba (sin sombras,
                // atraviesa las puertas e ilumina la pared del fondo y la cara del técnico)
                var lp = new GameObject("Luz_Piso_Barrido"); lp.transform.SetParent(raiz, false);
                luzPiso = lp.AddComponent<Light>();
                luzPiso.type = LightType.Point; luzPiso.range = 3.2f; luzPiso.intensity = 0f; luzPiso.color = new Color(1f, 0.72f, 0.4f);

                // y vista directamente por la junta de las puertas
                var f = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(f.GetComponent<Collider>());
                f.name = "Luz_Piso"; f.transform.SetParent(raiz, false);
                f.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                f.transform.localScale = new Vector3(0.5f, 0.28f, 1f);
                var mp = new Material(neon); Pintar(mp, new Color(1f, 0.85f, 0.6f), new Color(1f, 0.8f, 0.5f) * 3f);
                f.GetComponent<Renderer>().sharedMaterial = mp;
                franjaPiso = f.transform;

                // Indicador del piso en el panel (mira hacia el interior de la cabina)
                numero = Texto3D(raiz, fuenteUI, new Vector3(-1.155f, 1.47f, -0.25f), 2.4f, new Color(0.35f, 1f, 0.45f));
                estado = Texto3D(raiz, fuenteUI, new Vector3(-1.155f, 1.27f, -0.25f), 0.75f, new Color(1f, 0.6f, 0.15f));
                numero.text = piso; estado.text = "BAJANDO";
            }

            static TextMeshPro Texto3D(Transform padre, TMP_FontAsset fuente, Vector3 pos, float tam, Color col)
            {
                var go = new GameObject("Indicador");
                go.transform.SetParent(padre, false);
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);   // se lee desde el centro de la cabina (+X)
                var t = go.AddComponent<TextMeshPro>();
                if (fuente != null) t.font = fuente;
                t.rectTransform.sizeDelta = new Vector2(0.3f, 0.2f);
                t.fontSize = tam; t.alignment = TextAlignmentOptions.Center; t.color = col;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                return t;
            }

            /// <summary>Un cuadro del viaje: luces de piso que suben por la junta, fallos de la luz e indicador.</summary>
            public void Actualizar(float t, string piso, bool parpadeaNumero, bool frenado)
            {
                // Cada 1,3 s pasa la luz de un piso de abajo hacia arriba (la cabina baja)
                float ciclo = frenado ? 0.5f : Mathf.Repeat(t - 0.8f, 1.3f) / 1.3f;
                franjaPiso.localPosition = new Vector3(0f, Mathf.Lerp(-0.6f, 3.4f, ciclo), -1.33f);
                franjaPiso.gameObject.SetActive(t > 0.8f && !frenado);
                luzPiso.transform.localPosition = new Vector3(0f, Mathf.Lerp(-1.4f, 4.2f, ciclo), -1.75f);
                luzPiso.intensity = t > 0.8f && !frenado ? 5f * Mathf.Sin(ciclo * Mathf.PI) : 0f;
                // La luz falla un instante en dos momentos del viaje y al frenar
                bool falla = (t > 2.15f && t < 2.32f) || (t > 4.3f && t < 4.36f) || (t > 4.42f && t < 4.5f) || (t > 5.1f && t < 5.3f);
                float k = falla ? 0.12f : 1f;
                luz.intensity = IntensidadLuz * k;
                matTecho.SetColor("_EmissionColor", emisionTecho * k);
                numero.text = piso;
                numero.alpha = parpadeaNumero && Mathf.Repeat(t, 0.3f) < 0.15f ? 0.15f : 1f;
                estado.text = frenado ? "" : "BAJANDO";
                estado.alpha = Mathf.Repeat(t, 1f) < 0.6f ? 1f : 0.25f;
            }
        }
    }
}
