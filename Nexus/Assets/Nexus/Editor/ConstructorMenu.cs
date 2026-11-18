using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Construye la escena 00_Menu: pasillo 3D del centro de datos, plataformas con los dos técnicos,
    /// un androide SENTINEL inmóvil al fondo y el Canvas 1 (título, selector, briefing y créditos).
    /// Menú: Nexus → Interfaz → Construir menú principal.
    /// </summary>
    public static partial class ConstructorUI
    {
        const string Pre = "Assets/Nexus/Prefabs/";

        [MenuItem("Nexus/Interfaz/Construir menú principal")]
        public static void ConstruirMenu()
        {
            var escena = EditorSceneManager.OpenScene("Assets/Nexus/Escenas/00_Menu.unity", OpenSceneMode.Single);
            foreach (var go in escena.GetRootGameObjects()) Object.DestroyImmediate(go);
            // Se carga DESPUÉS de abrir la escena: OpenScene(Single) descarga los assets sin uso
            // y dejaría la referencia apuntando a un objeto destruido.
            var datos = CrearDatosCreditos();

            // ==================== Escenario 3D ====================
            var esc = new GameObject("Escenario").transform;
            GameObject Poner(string prefab, Vector3 pos, float rotY, Transform padre)
            {
                var g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Pre + prefab + ".prefab"), padre);
                g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
                return g;
            }
            for (int i = -2; i <= 1; i++)
                for (int j = 0; j < 8; j++)
                    Poner("Entorno/Modulo_Piso_3x3", new Vector3(i * 3 + 1.5f, 0, j * 3 + 1.5f), 0, esc);
            for (int j = 0; j < 8; j++)
            {
                Poner("Entorno/Modulo_Pared_3x3", new Vector3(-6, 0, j * 3 + 1.5f), 90, esc);
                Poner("Entorno/Modulo_Pared_3x3", new Vector3(6, 0, j * 3 + 1.5f), 90, esc);
                if (j >= 1)
                {
                    Poner("Entorno/Fila_Racks", new Vector3(-4.5f, 0, j * 3 + 1.5f), 90, esc);
                    Poner("Entorno/Fila_Racks", new Vector3(4.5f, 0, j * 3 + 1.5f), 90, esc);
                }
            }
            for (int i = -2; i <= 1; i++) Poner("Entorno/Modulo_Pared_3x3", new Vector3(i * 3 + 1.5f, 0, 24), 0, esc);

            Light Luz(string n, LightType tipo, Vector3 pos, Vector3 rot, Color c, float inten, float rango, bool sombras)
            {
                var l = new GameObject(n).AddComponent<Light>();
                l.transform.SetParent(esc, false);
                l.transform.SetPositionAndRotation(pos, Quaternion.Euler(rot));
                l.type = tipo; l.color = c; l.intensity = inten; l.range = rango;
                l.shadows = sombras ? LightShadows.Soft : LightShadows.None;
                if (tipo == LightType.Spot) { l.spotAngle = 48f; l.innerSpotAngle = 25f; }
                return l;
            }
            Luz("Luz_Emergencia_1", LightType.Point, new Vector3(0, 2.7f, 12), Vector3.zero, new Color(1f, 0.15f, 0.1f), 9f, 10f, false);
            Luz("Luz_Emergencia_2", LightType.Point, new Vector3(0, 2.7f, 20), Vector3.zero, new Color(1f, 0.15f, 0.1f), 8f, 10f, false);
            Luz("Luz_Relleno", LightType.Point, new Vector3(0, 2.8f, 3.5f), Vector3.zero, new Color(0.55f, 0.7f, 1f), 3f, 9f, false);

            // Plataformas y técnicos
            var personajes = new Transform[2];
            var focos = new Light[2];
            var anillos = new Renderer[2];
            string[] modelos = { "Neyder", "Deisy" };
            var controlador = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Nexus/Animaciones/AC_Tecnico.controller");
            var matMetal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Nexus/Materiales/Entorno/M_Rack_Cuerpo.mat");
            var matNeon = AssetDatabase.LoadAssetAtPath<Material>("Assets/Nexus/Materiales/Entorno/M_Neon_Cian.mat");
            for (int k = 0; k < 2; k++)
            {
                float x = k == 0 ? -1.2f : 1.2f;
                var plat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                plat.name = "Plataforma_" + modelos[k];
                plat.transform.SetParent(esc, false);
                plat.transform.position = new Vector3(x, 0.08f, 6f);
                plat.transform.localScale = new Vector3(1.2f, 0.08f, 1.2f);
                plat.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
                var anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                anillo.name = "Anillo_Luz";
                Object.DestroyImmediate(anillo.GetComponent<Collider>());
                anillo.transform.SetParent(esc, false);
                anillo.transform.position = new Vector3(x, 0.02f, 6f);
                anillo.transform.localScale = new Vector3(1.32f, 0.02f, 1.32f);
                anillo.GetComponent<MeshRenderer>().sharedMaterial = matNeon;
                anillos[k] = anillo.GetComponent<MeshRenderer>();

                var m = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Nexus/Modelos/Personajes/{modelos[k]}.fbx"), esc);
                m.name = modelos[k];
                m.transform.SetPositionAndRotation(new Vector3(x, 0.16f, 6f), Quaternion.Euler(0, 180, 0));
                var a = m.GetComponent<Animator>();
                a.applyRootMotion = false; a.runtimeAnimatorController = controlador;
                personajes[k] = m.transform;
                focos[k] = Luz("Foco_" + modelos[k], LightType.Spot, new Vector3(x, 3.4f, 5.2f), new Vector3(70, 0, 0), new Color(0.8f, 0.9f, 1f), 20f, 7f, true);
            }

            // Androide inmóvil al fondo del pasillo (solo decorativo: sin IA)
            var andro = Poner("Enemigos/Androide_Sentinel", new Vector3(0, 0, 19.5f), 180, esc);
            PrefabUtility.UnpackPrefabInstance(andro, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(andro.GetComponent<AndroideIA>());
            Object.DestroyImmediate(andro.GetComponent<NavMeshAgent>());
            Object.DestroyImmediate(andro.GetComponent<Rigidbody>());
            Object.DestroyImmediate(andro.GetComponentInChildren<ConoVision>().gameObject);
            andro.GetComponentInChildren<Animator>().runtimeAnimatorController = controlador;

            // Ambiente
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.11f, 0.15f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = 0.045f;
            RenderSettings.fogColor = new Color(0.01f, 0.012f, 0.018f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            var vol = new GameObject("Volumen_Global").AddComponent<Volume>();
            vol.isGlobal = true; vol.sharedProfile = ConstructorNiveles.PerfilPostProceso();

            // Cámara y sus dos planos
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();
            Transform Ancla(string n, Vector3 pos, Vector3 mirar)
            {
                var t = new GameObject(n).transform;
                t.position = pos; t.LookAt(mirar);
                return t;
            }
            var anclaTitulo = Ancla("Plano_Titulo", new Vector3(1.4f, 1.55f, 0.6f), new Vector3(-3.2f, 1.2f, 13f));
            var anclaSelector = Ancla("Plano_Selector", new Vector3(0f, 1.42f, 2.75f), new Vector3(0f, 1.12f, 6f));

            // ==================== Canvas 1: menú ====================
            var raiz = new GameObject("Menu");
            var es = new GameObject("EventSystem");
            es.transform.SetParent(raiz.transform, false);
            es.AddComponent<EventSystem>();
            ModuloEntradaUI(es);
            var canvas = NuevoCanvas("Canvas_Menu", raiz.transform, 0);
            var blanco = CargarSprite("Blanco");
            var marco = CargarSprite("Marco");
            var gris = new Color(0.6f, 0.68f, 0.72f);

            RectTransform Panel(string n)
            {
                var rt = Rect(n, canvas, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
                return rt;
            }
            Image Velo(Transform padre, float a)
            {
                var img = Icono("Velo", padre, blanco, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                img.preserveAspect = false; img.raycastTarget = true;
                img.rectTransform.anchorMin = Vector2.zero; img.rectTransform.anchorMax = Vector2.one; img.rectTransform.sizeDelta = Vector2.zero;
                img.color = new Color(0.01f, 0.02f, 0.03f, a);
                return img;
            }
            Button Opcion(string n, Transform padre, string texto, Vector2 ancla, Vector2 pos, float ancho, TextAlignmentOptions alin, float tam = 30)
            {
                var rt = Rect(n, padre, ancla, pos, new Vector2(ancho, 58));
                var img = rt.gameObject.AddComponent<Image>(); img.sprite = blanco; img.color = Color.white;
                var b = rt.gameObject.AddComponent<Button>();
                var cb = b.colors;
                cb.normalColor = new Color(0.4f, 0.9f, 1f, 0f);
                cb.highlightedColor = new Color(0.4f, 0.9f, 1f, 0.18f);
                cb.selectedColor = new Color(0.4f, 0.9f, 1f, 0.22f);
                cb.pressedColor = new Color(0.4f, 0.9f, 1f, 0.4f);
                cb.disabledColor = new Color(1, 1, 1, 0f);
                b.colors = cb;
                var t = Texto("Texto", rt, texto, tam, Color.white, alin, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ancho - 40, 54));
                t.fontStyle = FontStyles.Bold;
                return b;
            }

            // ---- Título ----
            var pTitulo = Panel("Panel_Titulo");
            TextMeshProUGUI CapaLogo(string n, Color c)
            {
                var t = Texto(n, pTitulo, "NEXUS", 190, c, TextAlignmentOptions.Left, new Vector2(0, 0.5f), new Vector2(110, 250), new Vector2(1100, 220));
                t.fontStyle = FontStyles.Bold; t.characterSpacing = 38;
                return t;
            }
            var logoC = CapaLogo("Logo_Cian", new Color(0.1f, 0.9f, 1f, 0.6f));
            var logoR = CapaLogo("Logo_Rojo", new Color(1f, 0.1f, 0.15f, 0.65f));
            var logo = CapaLogo("Logo", new Color(0.92f, 0.97f, 1f));
            Texto("Subtitulo", pTitulo, "PROTOCOLO DE EMERGENCIA  //  CENTRO DE DATOS NEXUS CORP.", 22, Cian, TextAlignmentOptions.Left, new Vector2(0, 0.5f), new Vector2(118, 132), new Vector2(1100, 30));
            var linea = Icono("Linea", pTitulo, blanco, new Vector2(0, 0.5f), new Vector2(118, 108), new Vector2(560, 2));
            linea.preserveAspect = false; linea.color = new Color(0.4f, 0.9f, 1f, 0.5f);
            var bNueva = Opcion("Boton_Nueva", pTitulo, "NUEVA PARTIDA", new Vector2(0, 0.5f), new Vector2(100, 30), 520, TextAlignmentOptions.Left);
            var bCont = Opcion("Boton_Continuar", pTitulo, "CONTINUAR", new Vector2(0, 0.5f), new Vector2(100, -40), 520, TextAlignmentOptions.Left);
            var bCred = Opcion("Boton_Creditos", pTitulo, "CRÉDITOS", new Vector2(0, 0.5f), new Vector2(100, -110), 520, TextAlignmentOptions.Left);
            var bSalir = Opcion("Boton_Salir", pTitulo, "SALIR", new Vector2(0, 0.5f), new Vector2(100, -180), 520, TextAlignmentOptions.Left);
            var tAmb = Texto("Ambiente", pTitulo, "ALERTA  ·  TEMPERATURA DEL NÚCLEO 78%  ·  ANDROIDES SENTINEL ACTIVOS", 16, Rojo, TextAlignmentOptions.Right, new Vector2(1, 0), new Vector2(-60, 40), new Vector2(1000, 24));
            Texto("Pie", pTitulo, "UNIVERSIDAD CENTRAL  ·  MODELADO 3D Y VIDEO JUEGOS  ·  2026-2", 14, gris, TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(60, 40), new Vector2(800, 22));
            var mLogo = Icono("Marco_Logo", pTitulo, marco, new Vector2(1, 1), new Vector2(-60, -40), new Vector2(240, 120));
            mLogo.type = Image.Type.Sliced; mLogo.preserveAspect = false; mLogo.color = new Color(0.4f, 0.9f, 1f, 0.25f);
            var logoInst = Icono("Logo_Institucion", mLogo.rectTransform, null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 100));
            var tSinLogo = Texto("Sin_Logo", mLogo.rectTransform, "[ LOGO INSTITUCIONAL ]", 15, gris, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 100));

            // ---- Selector ----
            var pSel = Panel("Panel_Selector");
            Texto("Etiqueta", pSel, "SELECCIÓN DE TÉCNICO", 20, Cian, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(70, -50), new Vector2(800, 28));
            Texto("Pregunta", pSel, "¿Quién entra al centro de datos?", 30, Color.white, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(70, -84), new Vector2(900, 40));
            var tNom = Texto("Nombre", pSel, "NEYDER", 72, Color.white, TextAlignmentOptions.Left, new Vector2(0, 0.5f), new Vector2(80, 110), new Vector2(560, 90));
            tNom.fontStyle = FontStyles.Bold; tNom.characterSpacing = 10;
            var tRol = Texto("Rol", pSel, "MODELO A", 22, Cian, TextAlignmentOptions.Left, new Vector2(0, 0.5f), new Vector2(84, 50), new Vector2(560, 30));
            var lSel = Icono("Linea", pSel, blanco, new Vector2(0, 0.5f), new Vector2(84, 26), new Vector2(380, 2));
            lSel.preserveAspect = false; lSel.color = new Color(0.4f, 0.9f, 1f, 0.5f);
            var tFicha = Texto("Ficha", pSel, "", 19, gris, TextAlignmentOptions.TopLeft, new Vector2(0, 0.5f), new Vector2(84, -60), new Vector2(560, 130));
            tFicha.lineSpacing = 18;
            var bIzq = Opcion("Boton_Izquierda", pSel, "<", new Vector2(0.5f, 0), new Vector2(-330, 100), 110, TextAlignmentOptions.Center, 40);
            var bConf = Opcion("Boton_Confirmar", pSel, "[ CONFIRMAR ]", new Vector2(0.5f, 0), new Vector2(0, 100), 460, TextAlignmentOptions.Center);
            var bDer = Opcion("Boton_Derecha", pSel, ">", new Vector2(0.5f, 0), new Vector2(330, 100), 110, TextAlignmentOptions.Center, 40);
            var bVolSel = Opcion("Boton_Volver", pSel, "< VOLVER", new Vector2(0, 0), new Vector2(60, 50), 260, TextAlignmentOptions.Left, 22);
            Texto("Ayuda", pSel, "A / D  ·  FLECHAS  ·  CAMBIAR TÉCNICO", 14, gris, TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(600, 22));

            // ---- Briefing ----
            var pBrief = Panel("Panel_Briefing");
            Velo(pBrief, 0.9f);
            var tTB = Texto("Titulo", pBrief, "INFORME DE MISIÓN", 44, Cian, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(90, -60), new Vector2(1100, 56));
            tTB.fontStyle = FontStyles.Bold; tTB.characterSpacing = 8;
            Texto("Sub", pBrief, "NEXUS CORP.  //  CENTRO DE DATOS 7  //  02:13  //  CLASIFICACIÓN: CRÍTICA", 16, gris, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(92, -122), new Vector2(1100, 24));
            var tBrief = Texto("Texto", pBrief, TextoBriefing(), 24, new Color(0.85f, 0.92f, 0.95f), TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(90, -170), new Vector2(1080, 760));
            tBrief.lineSpacing = 8;
            var mRet = Icono("Marco_Retrato", pBrief, marco, new Vector2(1, 1), new Vector2(-110, -160), new Vector2(320, 320));
            mRet.type = Image.Type.Sliced; mRet.preserveAspect = false; mRet.color = new Color(0.4f, 0.9f, 1f, 0.6f);
            var ret = Icono("Retrato", mRet.rectTransform, null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(306, 306));
            var tAsig = Texto("Asignado", pBrief, "", 20, gris, TextAlignmentOptions.Top, new Vector2(1, 1), new Vector2(-110, -500), new Vector2(320, 140));
            var bIni = Opcion("Boton_Iniciar", pBrief, "[ INICIAR MISIÓN ]", new Vector2(1, 0), new Vector2(-90, 60), 460, TextAlignmentOptions.Center);
            var bVolBr = Opcion("Boton_Volver", pBrief, "< VOLVER", new Vector2(0, 0), new Vector2(80, 60), 260, TextAlignmentOptions.Left, 22);

            // ---- Créditos ----
            var pCred = Panel("Panel_Creditos");
            Velo(pCred, 0.93f);
            var tTC = Texto("Titulo", pCred, "CRÉDITOS", 40, Cian, TextAlignmentOptions.Top, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(800, 52));
            tTC.fontStyle = FontStyles.Bold; tTC.characterSpacing = 12;
            var tCred = Texto("Texto", pCred, "", 26, Color.white, TextAlignmentOptions.Top, new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(1300, 720));
            tCred.lineSpacing = 6;
            var logos = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                logos[i] = Icono("Logo_" + i, pCred, null, new Vector2(0.5f, 0), new Vector2((i - 1) * 220, 150), new Vector2(190, 110));
                logos[i].gameObject.SetActive(false);   // se muestran solo si hay logos en los datos de créditos
            }
            var bVolCr = Opcion("Boton_Volver", pCred, "[ VOLVER ]", new Vector2(0.5f, 0), new Vector2(0, 50), 320, TextAlignmentOptions.Center);

            // ---- Transición ----
            var tr = Rect("Transicion", canvas, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
            var trImg = tr.gameObject.AddComponent<Image>(); trImg.sprite = blanco; trImg.color = Color.black;
            var trGrupo = tr.gameObject.AddComponent<CanvasGroup>();

            // ==================== Referencias ====================
            var menu = raiz.AddComponent<MenuPrincipal>();
            var so = new SerializedObject(menu);
            void Ref(string c, Object v) => so.FindProperty(c).objectReferenceValue = v;
            void Arr<T>(string c, T[] vs) where T : Object
            {
                var p = so.FindProperty(c); p.arraySize = vs.Length;
                for (int i = 0; i < vs.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = vs[i];
            }
            Ref("camara", cam); Ref("anclaTitulo", anclaTitulo); Ref("anclaSelector", anclaSelector);
            Ref("panelTitulo", pTitulo.gameObject); Ref("panelSelector", pSel.gameObject); Ref("panelBriefing", pBrief.gameObject); Ref("panelCreditos", pCred.gameObject);
            Ref("transicion", trGrupo);
            Ref("logo", logo); Ref("logoR", logoR); Ref("logoC", logoC); Ref("textoAmbiente", tAmb);
            Ref("botonNueva", bNueva); Ref("botonContinuar", bCont); Ref("botonCreditos", bCred); Ref("botonSalir", bSalir);
            Ref("logoInstitucion", logoInst); Ref("textoSinLogo", tSinLogo);
            Arr("personajes", personajes); Arr("focos", focos); Arr("anillos", anillos);
            Ref("nombreSeleccion", tNom); Ref("rolSeleccion", tRol); Ref("fichaSeleccion", tFicha);
            Ref("botonIzquierda", bIzq); Ref("botonDerecha", bDer); Ref("botonConfirmar", bConf); Ref("botonVolverSelector", bVolSel);
            Ref("textoBriefing", tBrief); Ref("textoAsignado", tAsig); Ref("retratoBriefing", ret);
            Arr("retratos", new[] { AssetDatabase.LoadAssetAtPath<Sprite>(RutaUI + "/Iconos/Retrato_Neyder.png"), AssetDatabase.LoadAssetAtPath<Sprite>(RutaUI + "/Iconos/Retrato_Deisy.png") });
            Ref("botonIniciar", bIni); Ref("botonVolverBriefing", bVolBr);
            Ref("datos", datos); Ref("textoCreditos", tCred); Arr("logosCreditos", logos); Ref("botonVolverCreditos", bVolCr);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[Nexus] Menú principal construido.");
        }

        static string TextoBriefing() =>
            "<color=#8ef3ff>02:13</color> — La actualización de seguridad <b>CERBERUS 7.1</b> se corrompió. El sistema de " +
            "refrigeración del centro de datos quedó bloqueado y las puertas principales están selladas.\n\n" +
            "Eres el único técnico dentro del complejo. Si la temperatura del núcleo llega al <color=#ff5a50>100%</color>, " +
            "los servidores se incendiarán.\n\n" +
            "<color=#8ef3ff><b>OBJETIVO</b></color>   Recupera los <b>3 DISCOS DUROS MAESTROS</b>, uno en cada sector, para forzar " +
            "el reinicio manual antes del colapso térmico. Los <b>cartuchos de refrigerante</b> te darán tiempo extra.\n\n" +
            "<color=#ff5a50><b>PELIGRO</b></color>   Los Androides de Seguridad <b>SENTINEL</b> tratan a cualquier humano como una " +
            "anomalía. Evita su luz roja, escóndete detrás de los racks y no corras cerca de ellos: te oirán.\n\n" +
            "<color=#8ef3ff><b>CONTROLES</b></color>   WASD / stick: moverse  ·  SHIFT: correr  ·  E: interactuar  ·  TAB: inventario y mapa\n" +
            "<size=85%><color=#9fb3bb>En el celular se usan el joystick y los botones en pantalla.</color></size>";

        static CreditosNexus CrearDatosCreditos()
        {
            const string ruta = "Assets/Nexus/Datos/Creditos.asset";
            var datos = AssetDatabase.LoadAssetAtPath<CreditosNexus>(ruta);
            if (datos != null) return datos;   // no se sobrescriben datos que el equipo ya haya llenado
            // La carpeta debe crearse con el AssetDatabase (si solo existe en disco, CreateAsset falla)
            if (!AssetDatabase.IsValidFolder("Assets/Nexus/Datos")) AssetDatabase.CreateFolder("Assets/Nexus", "Datos");
            datos = ScriptableObject.CreateInstance<CreditosNexus>();
            AssetDatabase.CreateAsset(datos, ruta);
            AssetDatabase.SaveAssets();
            return datos;
        }
    }
}
