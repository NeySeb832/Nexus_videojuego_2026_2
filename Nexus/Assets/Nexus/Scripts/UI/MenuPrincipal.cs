using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nexus
{
    /// <summary>
    /// Menú principal (Canvas 1): título, selector de personaje, briefing de la misión y créditos.
    /// La escena de fondo es un pasillo del centro de datos; la cámara se desplaza suavemente entre
    /// el plano del título y el de los dos técnicos sobre sus plataformas.
    /// </summary>
    public class MenuPrincipal : MonoBehaviour
    {
        enum Pantalla { Titulo, Selector, Briefing, Creditos }

        [Header("Cámara")]
        [SerializeField] Camera camara;
        [SerializeField] Transform anclaTitulo, anclaSelector;

        [Header("Paneles")]
        [SerializeField] GameObject panelTitulo, panelSelector, panelBriefing, panelCreditos;
        [SerializeField] CanvasGroup transicion;

        [Header("Título")]
        [SerializeField] TMP_Text logo, logoR, logoC, textoAmbiente;
        [SerializeField] Button botonNueva, botonContinuar, botonCreditos, botonSalir;
        [SerializeField] Image logoInstitucion;
        [SerializeField] TMP_Text textoSinLogo;

        [Header("Selector")]
        [SerializeField] Transform[] personajes;
        [SerializeField] Light[] focos;
        [SerializeField] Renderer[] anillos;
        [SerializeField] TMP_Text nombreSeleccion, rolSeleccion, fichaSeleccion;
        [SerializeField] Button botonIzquierda, botonDerecha, botonConfirmar, botonVolverSelector;

        [Header("Briefing")]
        [SerializeField] TMP_Text textoBriefing, textoAsignado;
        [SerializeField] Image retratoBriefing;
        [SerializeField] Sprite[] retratos;
        [SerializeField] Button botonIniciar, botonVolverBriefing;

        [Header("Créditos")]
        [SerializeField] CreditosNexus datos;
        [SerializeField] TMP_Text textoCreditos;
        [SerializeField] Image[] logosCreditos;
        [SerializeField] Button botonVolverCreditos;

        static readonly string[] Nombres = { "NEYDER", "DEISY" };
        static readonly string[] Roles = { "MODELO A  ·  TÉCNICO DE REDES", "MODELO B  ·  ANALISTA DE SISTEMAS" };
        static readonly string[] Fichas =
        {
            "ID  NX-0417\nTURNO  NOCTURNO\nESPECIALIDAD  INFRAESTRUCTURA DE RED\nACCESO  NIVEL 1\n\n<color=#3fe6ff>HABILIDAD [Q]  ·  SEÑUELO DE RED</color>\n<size=85%>Lanza un repetidor que emite una señal falsa:\nlos androides cercanos van a revisarlo.</size>",
            "ID  NX-0522\nTURNO  NOCTURNO\nESPECIALIDAD  ANÁLISIS DE SISTEMAS\nACCESO  NIVEL 1\n\n<color=#3fe6ff>HABILIDAD [Q]  ·  KILLSWITCH.exe</color>\n<size=85%>Apaga 10 segundos al androide más cercano\nque tengas a la vista.</size>"
        };
        static readonly int IdColor = Shader.PropertyToID("_Color");

        Pantalla actual;
        int seleccion;
        float proximoGlitch, finGlitch;
        bool cargando;
        Coroutine rutinaBriefing;
        MaterialPropertyBlock bloque;

        void Start()
        {
            Time.timeScale = 1f;
            bloque = new MaterialPropertyBlock();
            botonNueva.onClick.AddListener(() => Mostrar(Pantalla.Selector));
            botonContinuar.onClick.AddListener(Continuar);
            botonCreditos.onClick.AddListener(() => Mostrar(Pantalla.Creditos));
            botonSalir.onClick.AddListener(Application.Quit);
            botonIzquierda.onClick.AddListener(() => Seleccionar(seleccion - 1));
            botonDerecha.onClick.AddListener(() => Seleccionar(seleccion + 1));
            botonConfirmar.onClick.AddListener(Confirmar);
            botonVolverSelector.onClick.AddListener(() => Mostrar(Pantalla.Titulo));
            botonIniciar.onClick.AddListener(() => StartCoroutine(Cargar("01_Mantenimiento")));
            botonVolverBriefing.onClick.AddListener(() => Mostrar(Pantalla.Selector));
            botonVolverCreditos.onClick.AddListener(() => Mostrar(Pantalla.Titulo));

            foreach (var b in new[] { botonNueva, botonContinuar, botonCreditos, botonSalir, botonIzquierda, botonDerecha,
                          botonConfirmar, botonVolverSelector, botonIniciar, botonVolverBriefing, botonVolverCreditos })
                b.onClick.AddListener(() => GestorAudio.Sfx("ui_clic"));

            botonContinuar.gameObject.SetActive(EstadoPartida.HayGuardado);
            botonSalir.gameObject.SetActive(Application.platform != RuntimePlatform.WebGLPlayer);   // en el navegador no se "sale"
            // Reacomoda las opciones visibles para que no queden huecos
            float y = ((RectTransform)botonNueva.transform).anchoredPosition.y;
            foreach (var b in new[] { botonNueva, botonContinuar, botonCreditos, botonSalir })
            {
                if (!b.gameObject.activeSelf) continue;
                var rt = (RectTransform)b.transform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
                y -= 70f;
            }
            PrepararLogoInstitucion();
            PrepararCreditos();

            camara.transform.SetPositionAndRotation(anclaTitulo.position, anclaTitulo.rotation);
            Seleccionar(EstadoPartida.PersonajeElegido);
            Mostrar(Pantalla.Titulo);
            StartCoroutine(Fundido(1f, 0f, 1.2f));
        }

        // ======================= Navegación =======================
        void Mostrar(Pantalla p)
        {
            actual = p;
            panelTitulo.SetActive(p == Pantalla.Titulo);
            panelSelector.SetActive(p == Pantalla.Selector);
            panelBriefing.SetActive(p == Pantalla.Briefing);
            panelCreditos.SetActive(p == Pantalla.Creditos);
            var primero = p == Pantalla.Titulo ? botonNueva : p == Pantalla.Selector ? botonConfirmar
                        : p == Pantalla.Briefing ? botonIniciar : botonVolverCreditos;
            EventSystem.current?.SetSelectedGameObject(primero.gameObject);
            if (p == Pantalla.Briefing)
            {
                if (rutinaBriefing != null) StopCoroutine(rutinaBriefing);
                rutinaBriefing = StartCoroutine(EscribirBriefing());
            }
        }

        void Update()
        {
            // Cámara: se desliza hacia el plano de la pantalla actual con un leve balanceo
            var ancla = actual == Pantalla.Selector || actual == Pantalla.Briefing ? anclaSelector : anclaTitulo;
            Vector3 destino = ancla.position + new Vector3(Mathf.Sin(Time.time * 0.3f) * 0.08f, Mathf.Sin(Time.time * 0.45f) * 0.03f, 0f);
            if (actual == Pantalla.Selector) destino += Vector3.right * (seleccion == 0 ? -0.35f : 0.35f);
            camara.transform.position = Vector3.Lerp(camara.transform.position, destino, Time.deltaTime * 2.2f);
            camara.transform.rotation = Quaternion.Slerp(camara.transform.rotation, ancla.rotation, Time.deltaTime * 2.2f);

            AnimarLogo();
            if (textoAmbiente != null) textoAmbiente.alpha = Mathf.Repeat(Time.time, 1.4f) < 1f ? 1f : 0.3f;

            var k = Keyboard.current;
            var g = Gamepad.current;
            if (actual == Pantalla.Selector)
            {
                if ((k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame)) || (g != null && g.dpad.left.wasPressedThisFrame)) Seleccionar(seleccion - 1);
                if ((k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame)) || (g != null && g.dpad.right.wasPressedThisFrame)) Seleccionar(seleccion + 1);
            }
            bool atras = (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame);
            if (atras && actual != Pantalla.Titulo) Mostrar(actual == Pantalla.Briefing ? Pantalla.Selector : Pantalla.Titulo);

            // El técnico elegido gira despacio para lucirse; el otro mira a la cámara
            for (int i = 0; i < personajes.Length; i++)
            {
                float objetivo = i == seleccion && actual == Pantalla.Selector ? 180f + Mathf.Sin(Time.time * 0.8f) * 25f : 180f;
                personajes[i].rotation = Quaternion.Slerp(personajes[i].rotation, Quaternion.Euler(0f, objetivo, 0f), Time.deltaTime * 3f);
            }
        }

        // ======================= Selector =======================
        void Seleccionar(int i)
        {
            seleccion = (i + personajes.Length) % personajes.Length;
            nombreSeleccion.text = Nombres[seleccion];
            rolSeleccion.text = Roles[seleccion];
            fichaSeleccion.text = Fichas[seleccion];
            for (int k = 0; k < personajes.Length; k++)
            {
                bool elegido = k == seleccion;
                if (focos[k] != null) focos[k].intensity = elegido ? 22f : 3f;
                anillos[k].GetPropertyBlock(bloque);
                bloque.SetColor(IdColor, elegido ? new Color(0.1f, 0.9f, 1f) * 6f : new Color(0.1f, 0.4f, 0.5f) * 0.6f);
                anillos[k].SetPropertyBlock(bloque);
            }
        }

        void Confirmar()
        {
            EstadoPartida.NuevaPartida(seleccion);
            Mostrar(Pantalla.Briefing);
        }

        // ======================= Briefing =======================
        IEnumerator EscribirBriefing()
        {
            retratoBriefing.sprite = retratos[seleccion];
            textoAsignado.text = $"TÉCNICO ASIGNADO\n<size=150%><b>{Nombres[seleccion]}</b></size>\n{Roles[seleccion]}";
            textoBriefing.maxVisibleCharacters = 0;
            textoBriefing.ForceMeshUpdate();
            int total = textoBriefing.textInfo.characterCount;
            yield return new WaitForSeconds(0.25f);
            for (int v = 0; v <= total; v += 2)
            {
                textoBriefing.maxVisibleCharacters = v;
                var k = Keyboard.current;
                var m = Mouse.current;
                if ((k != null && k.spaceKey.wasPressedThisFrame) || (m != null && m.leftButton.wasPressedThisFrame && v > 20)) v = total;
                yield return new WaitForSeconds(0.008f);
            }
            textoBriefing.maxVisibleCharacters = total;
        }

        // ======================= Créditos =======================
        void PrepararCreditos()
        {
            if (datos == null)
            {
                textoCreditos.text = "Falta el archivo de datos de créditos (Assets/Nexus/Datos/Creditos.asset).";
                foreach (var l in logosCreditos) l.gameObject.SetActive(false);
                return;
            }
            var sb = new System.Text.StringBuilder();
            sb.Append("<size=70%><color=#8ef3ff>TRABAJO FINAL</color></size>\n");
            sb.Append($"<size=130%><b>{datos.materia.ToUpperInvariant()}</b></size>\n");
            sb.Append($"{datos.institucion}\n{datos.programa}\n");
            sb.Append($"Docente: {datos.docente}\n{datos.periodo}\n\n");
            sb.Append("<size=70%><color=#8ef3ff>DESARROLLADO POR</color></size>\n");
            foreach (var p in datos.integrantes)
                sb.Append($"<b>{p.nombreCompleto}</b>\n<size=75%><color=#9fb3bb>{p.rol}</color></size>\n");
            sb.Append("\n<size=70%><color=#8ef3ff>HERRAMIENTAS Y RECURSOS</color></size>\n");
            sb.Append($"<size=80%>{datos.recursos}</size>");
            textoCreditos.text = sb.ToString();
            for (int i = 0; i < logosCreditos.Length; i++)
            {
                bool hay = datos.logos != null && i < datos.logos.Length && datos.logos[i] != null;
                logosCreditos[i].gameObject.SetActive(hay);
                if (hay) logosCreditos[i].sprite = datos.logos[i];
            }
        }

        void PrepararLogoInstitucion()
        {
            bool hay = datos != null && datos.logos != null && datos.logos.Length > 0 && datos.logos[0] != null;
            logoInstitucion.gameObject.SetActive(hay);
            textoSinLogo.gameObject.SetActive(!hay);
            if (hay) logoInstitucion.sprite = datos.logos[0];
        }

        // ======================= Continuar / cargar =======================
        void Continuar()
        {
            string escena = EstadoPartida.Cargar();
            if (escena != null) StartCoroutine(Cargar(escena));
        }

        IEnumerator Cargar(string escena)
        {
            if (cargando) yield break;
            cargando = true;
            yield return Fundido(0f, 1f, 0.8f);
            SceneManager.LoadScene(escena);
        }

        IEnumerator Fundido(float desde, float hasta, float duracion)
        {
            transicion.blocksRaycasts = true;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duracion)
            {
                transicion.alpha = Mathf.Lerp(desde, hasta, t);
                yield return null;
            }
            transicion.alpha = hasta;
            transicion.blocksRaycasts = hasta > 0.5f;
        }

        // ======================= Logo glitch =======================
        void AnimarLogo()
        {
            float t = Time.unscaledTime;
            if (t >= proximoGlitch)
            {
                proximoGlitch = t + Random.Range(1.5f, 4f);
                finGlitch = t + Random.Range(0.08f, 0.22f);
            }
            bool glitch = t < finGlitch;
            float amp = glitch ? 16f : 3f;
            Vector2 baseP = logo.rectTransform.anchoredPosition;
            logoR.rectTransform.anchoredPosition = baseP + new Vector2(-amp * 0.6f + (Mathf.PerlinNoise(t * 8f, 1f) - 0.5f) * amp, glitch ? Random.Range(-4f, 4f) : 0f);
            logoC.rectTransform.anchoredPosition = baseP + new Vector2(amp * 0.6f + (Mathf.PerlinNoise(t * 8f, 5f) - 0.5f) * amp, glitch ? Random.Range(-4f, 4f) : 0f);
            logo.alpha = glitch && Random.value < 0.3f ? 0.6f : 1f;
        }
    }
}
