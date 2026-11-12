using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Construye la interfaz de los niveles (prefab UI_Nivel):
    ///   Canvas_HUD   (Canvas 2): corazones, refrigeración, recursos, objetivo, avisos, diálogos.
    ///   Canvas_Final (Canvas 3): Game Over / Victoria con botones.
    /// Menú: Nexus → Interfaz → Construir UI de nivel.
    /// </summary>
    public static partial class ConstructorUI
    {
        const string RutaUI = "Assets/Nexus/UI";
        const string RutaPrefab = "Assets/Nexus/Prefabs/UI/UI_Nivel.prefab";

        static readonly Color Fondo = new Color(0.02f, 0.06f, 0.08f, 0.78f);
        static readonly Color Cian = new Color(0.55f, 0.95f, 1f);
        static readonly Color Borde = new Color(0.2f, 0.85f, 1f, 0.55f);
        static readonly Color Rojo = new Color(1f, 0.32f, 0.25f);

        [MenuItem("Nexus/Interfaz/Construir UI de nivel")]
        public static void Construir()
        {
            GenerarIconos();
            var corazon = CargarSprite("Icono_Corazon");
            var corazonVacio = CargarSprite("Icono_Corazon_Vacio");
            var disco = CargarSprite("Icono_Disco");
            var copo = CargarSprite("Icono_Copo");
            var blanco = CargarSprite("Blanco");

            var raiz = new GameObject("UI_Nivel");
            var hud = raiz.AddComponent<HUDNexus>();

            // Sistema de eventos para los botones (compatible con el Input System)
            var es = new GameObject("EventSystem");
            es.transform.SetParent(raiz.transform, false);
            es.AddComponent<EventSystem>();
            ModuloEntradaUI(es);

            // ===================== Canvas 2: HUD =====================
            var canvasHud = NuevoCanvas("Canvas_HUD", raiz.transform, 0);

            // HUD mínimo: solo texto pequeño con sombra, sin recuadros, para no tapar el escenario.
            var gris = new Color(0.62f, 0.72f, 0.76f);
            var tObjetivo = Texto("Texto_Objetivo", canvasHud, "OBJETIVO: ...", 22, Cian, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(30, -24), new Vector2(1000, 32));

            // Salud: oculta durante el juego; aparece unos segundos cuando cambia (golpe o curación)
            var indicador = Rect("Indicador_Salud", canvasHud, new Vector2(0, 1), new Vector2(30, -62), new Vector2(460, 34));
            var grupoSalud = indicador.gameObject.AddComponent<CanvasGroup>();
            grupoSalud.alpha = 0f; grupoSalud.blocksRaycasts = false;
            var corazones = new Image[EstadoPartida.SaludMaxima];
            for (int i = 0; i < corazones.Length; i++)
                corazones[i] = Icono("Corazon_" + i, indicador, corazon, new Vector2(0, 0.5f), new Vector2(i * 34, 0), new Vector2(28, 28));
            var tEstado = Texto("Texto_Estado", indicador, "ESTADO: ESTABLE", 18, new Color(0.4f, 1f, 0.6f), TextAlignmentOptions.Left, new Vector2(0, 0.5f), new Vector2(112, 0), new Vector2(340, 30));

            Texto("Etiqueta_Refri", canvasHud, "REFRIGERACIÓN", 15, gris, TextAlignmentOptions.TopRight, new Vector2(1, 1), new Vector2(-30, -20), new Vector2(300, 22));
            var tTiempo = Texto("Texto_Tiempo", canvasHud, "07:00", 40, Cian, TextAlignmentOptions.TopRight, new Vector2(1, 1), new Vector2(-30, -38), new Vector2(300, 46));
            tTiempo.fontStyle = FontStyles.Bold;
            var tTemp = Texto("Texto_Temperatura", canvasHud, "NÚCLEO 80%", 15, new Color(1f, 0.75f, 0.3f), TextAlignmentOptions.TopRight, new Vector2(1, 1), new Vector2(-30, -84), new Vector2(300, 22));
            var tDiscos = Texto("Texto_Discos", canvasHud, "DISCOS 0/3", 17, Cian, TextAlignmentOptions.TopRight, new Vector2(1, 1), new Vector2(-30, -110), new Vector2(300, 24));
            var tRefri = Texto("Texto_Refrigerante", canvasHud, "CARTUCHOS x0", 17, new Color(0.7f, 0.95f, 1f), TextAlignmentOptions.TopRight, new Vector2(1, 1), new Vector2(-30, -134), new Vector2(300, 24));
            var tHabilidad = Texto("Texto_Habilidad", canvasHud, "Q  ·  HABILIDAD  ·  LISTA", 15, Cian, TextAlignmentOptions.TopRight, new Vector2(1, 1), new Vector2(-30, -160), new Vector2(460, 22));
            Texto("Texto_Ayuda", canvasHud, "TAB / I  ·  INVENTARIO", 14, new Color(0.5f, 0.6f, 0.64f), TextAlignmentOptions.TopRight, new Vector2(1, 1), new Vector2(-30, -184), new Vector2(400, 22));

            var prompt = Rect("Panel_Prompt", canvasHud, new Vector2(0.5f, 0), new Vector2(0, 130), new Vector2(800, 36));
            var tPrompt = Texto("Texto_Prompt", prompt, "[E] Usar terminal", 23, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 36));

            var aviso = Rect("Aviso", canvasHud, new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(1400, 40));
            var grupoAviso = aviso.gameObject.AddComponent<CanvasGroup>();
            grupoAviso.blocksRaycasts = false;
            var tAviso = Texto("Texto_Aviso", aviso, "", 24, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 40));

            // Viñeta roja que late cuando la salud está crítica (el aviso de peligro sin ocupar pantalla)
            var vineta = Icono("Vineta_Danio", canvasHud, CargarSprite("Vineta"), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            vineta.preserveAspect = false;
            vineta.rectTransform.anchorMin = Vector2.zero; vineta.rectTransform.anchorMax = Vector2.one;
            vineta.rectTransform.sizeDelta = Vector2.zero;
            vineta.color = new Color(1f, 1f, 1f, 0f);
            vineta.transform.SetAsFirstSibling();

            // Cuadro de diálogo: el único recuadro, porque el texto largo necesita fondo para leerse
            var dialogo = Rect("Panel_Dialogo", canvasHud, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1400, 230));
            var fondoDialogo = dialogo.gameObject.AddComponent<Image>();
            fondoDialogo.color = new Color(0.01f, 0.02f, 0.03f, 0.88f);
            var linea = Icono("Linea", dialogo, blanco, new Vector2(0.5f, 1), Vector2.zero, new Vector2(1400, 3));
            linea.preserveAspect = false;
            linea.color = new Color(1f, 0.3f, 0.25f, 0.9f);
            var tTitulo = Texto("Titulo", dialogo, "[!] ALERTA DEL SISTEMA", 21, new Color(1f, 0.45f, 0.38f), TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(28, -14), new Vector2(1340, 30));
            tTitulo.fontStyle = FontStyles.Bold;
            var tCuerpo = Texto("Texto", dialogo, "", 27, new Color(0.8f, 1f, 0.88f), TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(28, -52), new Vector2(1340, 140));
            var tPista = Texto("Pista", dialogo, ">> E / ESPACIO / CLIC / TOCA PARA CONTINUAR", 15, gris, TextAlignmentOptions.BottomRight, new Vector2(1, 0), new Vector2(-24, 12), new Vector2(700, 22));
            TMP_Text tPersonaje = null, tInventario = null;
            Image barra = null;

            // ===================== Canvas 3: Pantalla final =====================
            // Pantalla de fallo crítico / sistema restaurado (ver ConstructorUI_Final.cs)
            var canvasFinal = ConstruirPantallaFinal(raiz, blanco);

            // ===================== Referencias del HUDNexus =====================
            var so = new SerializedObject(hud);
            var arr = so.FindProperty("corazones");
            arr.arraySize = corazones.Length;
            for (int i = 0; i < corazones.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = corazones[i];
            void Ref(string campo, Object valor) => so.FindProperty(campo).objectReferenceValue = valor;
            Ref("corazonLleno", corazon); Ref("corazonVacio", corazonVacio); Ref("textoPersonaje", tPersonaje);
            Ref("textoTiempo", tTiempo); Ref("textoTemperatura", tTemp); Ref("barraTemperatura", barra);
            Ref("textoDiscos", tDiscos); Ref("textoRefrigerante", tRefri); Ref("textoInventario", tInventario);
            Ref("textoHabilidad", tHabilidad);
            Ref("textoObjetivo", tObjetivo); Ref("panelPrompt", prompt.gameObject); Ref("textoPrompt", tPrompt);
            Ref("grupoAviso", grupoAviso); Ref("textoAviso", tAviso);
            Ref("panelDialogo", dialogo.gameObject); Ref("tituloDialogo", tTitulo); Ref("textoDialogo", tCuerpo); Ref("pistaDialogo", tPista);
            Ref("canvasFinal", canvasFinal.GetComponent<Canvas>());
            Ref("grupoSalud", grupoSalud); Ref("textoEstadoSalud", tEstado); Ref("vinetaDanio", vineta);
            ConstruirInventario(raiz, corazon, corazonVacio, blanco);
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(RutaPrefab));
            AssetDatabase.Refresh();
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Object.DestroyImmediate(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Nexus] UI de nivel construida.");
        }

        // ---------------- Elementos ----------------
        static RectTransform NuevoCanvas(string nombre, Transform padre, int orden)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = orden;
            var cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;   // 1920x1080 siempre cabe completo (PC, web o móvil)
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        static RectTransform Rect(string nombre, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tam)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = ancla;
            rt.pivot = ancla;
            rt.anchoredPosition = pos;
            rt.sizeDelta = tam;
            return rt;
        }

        static RectTransform Panel(string nombre, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tam)
        {
            var rt = Rect(nombre, padre, ancla, pos, tam);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Fondo;
            img.raycastTarget = false;
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = Borde; o.effectDistance = new Vector2(2, -2);
            return rt;
        }

        static TextMeshProUGUI Texto(string nombre, Transform padre, string texto, float tam, Color color, TextAlignmentOptions alin, Vector2 ancla, Vector2 pos, Vector2 caja)
        {
            var rt = Rect(nombre, padre, ancla, pos, caja);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = texto; t.fontSize = tam; t.color = color; t.alignment = alin;
            t.raycastTarget = false;
            t.fontSharedMaterial = MaterialSombra();
            return t;
        }

        static Material materialSombra;

        /// <summary>Material de texto con sombra suave (underlay): se lee sobre el escenario sin recuadros.</summary>
        static Material MaterialSombra()
        {
            if (materialSombra != null) return materialSombra;
            string ruta = RutaUI + "/TMP_Sombra.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(TMP_Settings.defaultFontAsset.material) { name = "TMP_Sombra" };
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.EnableKeyword("UNDERLAY_ON");
            m.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.9f));
            m.SetFloat("_UnderlayOffsetX", 0.5f);
            m.SetFloat("_UnderlayOffsetY", -0.5f);
            m.SetFloat("_UnderlayDilate", 0.5f);
            m.SetFloat("_UnderlaySoftness", 0.4f);
            EditorUtility.SetDirty(m);
            return materialSombra = m;
        }

        static Image Icono(string nombre, Transform padre, Sprite sprite, Vector2 ancla, Vector2 pos, Vector2 tam)
        {
            var rt = Rect(nombre, padre, ancla, pos, tam);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        static Button Boton(string nombre, Transform padre, string texto, Vector2 pos)
        {
            var rt = Rect(nombre, padre, new Vector2(0.5f, 0.5f), pos, new Vector2(560, 72));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.04f, 0.1f, 0.12f, 0.95f);
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = Borde; o.effectDistance = new Vector2(2, -2);
            var b = rt.gameObject.AddComponent<Button>();
            var colores = b.colors;
            colores.highlightedColor = new Color(0.4f, 0.9f, 1f);
            colores.selectedColor = new Color(0.4f, 0.9f, 1f);
            colores.pressedColor = new Color(0.2f, 0.6f, 0.7f);
            colores.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            b.colors = colores;
            Texto("Texto", rt, texto, 30, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540, 64));
            return b;
        }

        // ---------------- Íconos procedurales ----------------
        static void GenerarIconos()
        {
            Directory.CreateDirectory(RutaUI);
            Icono("Icono_Corazon", 64, (x, y) => Corazon(x, y) ? new Color(0.92f, 0.12f, 0.14f, 1f) : Color.clear);
            Icono("Icono_Corazon_Vacio", 64, (x, y) =>
                Corazon(x, y) ? (Corazon(x * 1.18f, y * 1.18f + 0.05f) ? new Color(0.15f, 0.05f, 0.05f, 0.55f) : new Color(0.55f, 0.2f, 0.2f, 0.9f)) : Color.clear);
            Icono("Icono_Disco", 64, (x, y) =>
            {
                float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
                if (ax < 0.8f && ay < 0.8f && (ax > 0.62f || ay > 0.62f)) return new Color(0.75f, 0.8f, 0.85f, 1f);
                if (ax < 0.62f && ay < 0.62f) return Mathf.Abs(y) < 0.12f ? new Color(0.2f, 0.7f, 1f, 1f) : new Color(0.25f, 0.28f, 0.32f, 1f);
                return Color.clear;
            });
            Icono("Icono_Copo", 64, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                if (r > 0.9f) return Color.clear;
                for (int k = 0; k < 3; k++)
                {
                    float a = k * Mathf.PI / 3f;
                    float d = Mathf.Abs(-Mathf.Sin(a) * x + Mathf.Cos(a) * y);
                    if (d < 0.09f) return new Color(0.75f, 0.95f, 1f, 1f);
                }
                return Color.clear;
            });
            Icono("Blanco", 8, (x, y) => Color.white);
            Icono("Vineta", 256, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x * 0.8f + y * y);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 1.4f, r)) * 0.8f;
                return new Color(0.75f, 0.02f, 0.02f, a);
            });
        }

        static bool Corazon(float x, float y)
        {
            x *= 1.25f; y = y * 1.25f + 0.15f;
            float a = x * x + y * y - 1f;
            return a * a * a - x * x * y * y * y <= 0f;
        }

        static void Icono(string nombre, int tam, System.Func<float, float, Color> f)
        {
            var t = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
            for (int y = 0; y < tam; y++)
                for (int x = 0; x < tam; x++)
                    t.SetPixel(x, y, f((x + 0.5f) / tam * 2f - 1f, (y + 0.5f) / tam * 2f - 1f));
            t.Apply();
            string ruta = $"{RutaUI}/{nombre}.png";
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(ruta);
            var ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }

        static Sprite CargarSprite(string nombre) => AssetDatabase.LoadAssetAtPath<Sprite>($"{RutaUI}/{nombre}.png");

        /// <summary>
        /// Módulo de entrada de la UI compatible con el Input System. Al agregarlo, el módulo ya trae
        /// sus acciones por defecto; volver a llamar AssignDefaultActions() falla ("must be part of an
        /// InputActionAsset"), así que solo se llama si no tiene ninguna.
        /// </summary>
        static void ModuloEntradaUI(GameObject eventSystem)
        {
            var modulo = eventSystem.AddComponent<InputSystemUIInputModule>();
            if (modulo.point == null || modulo.point.action == null) modulo.AssignDefaultActions();
        }
    }
}
