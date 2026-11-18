using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Pantalla de inventario (terminal portátil del técnico): retrato, signos vitales, objetos,
    /// archivo de notas y mapa. Los íconos de los objetos y los retratos se renderizan desde los
    /// modelos 3D del juego.
    /// </summary>
    public static partial class ConstructorUI
    {
        static readonly string[] PrefabIcono =
        {
            "Coleccionables/Disco_Duro_Maestro", "Coleccionables/Tarjeta_Acceso_Roja", "Coleccionables/Tarjeta_Acceso_Azul",
            "Coleccionables/Cartucho_Refrigerante", "Coleccionables/Fusible", "Coleccionables/Memoria_USB",
            "Coleccionables/Bebida_Energetica", "Coleccionables/Barrita_Proteina",
            "Coleccionables/Cuchillo", "Coleccionables/Pistola", "Coleccionables/Escopeta",
            "Coleccionables/Balas_Pistola", "Coleccionables/Cartuchos_Escopeta"
        };   // mismo orden que el enum TipoColeccionable

        static void ConstruirInventario(GameObject raiz, Sprite corazon, Sprite corazonVacio, Sprite blanco)
        {
            var iconos = RenderizarIconos();
            var retratos = RenderizarRetratos();
            GenerarRecursosInventario();
            var marco = CargarSprite("Marco");
            var gris = new Color(0.55f, 0.65f, 0.7f);

            var canvas = NuevoCanvas("Canvas_Inventario", raiz.transform, 5);
            var fondo = Icono("Fondo", canvas, blanco, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            fondo.preserveAspect = false;
            fondo.rectTransform.anchorMin = Vector2.zero; fondo.rectTransform.anchorMax = Vector2.one; fondo.rectTransform.sizeDelta = Vector2.zero;
            fondo.color = new Color(0.01f, 0.025f, 0.035f, 0.97f);
            fondo.raycastTarget = true;

            Texto("Cabecera", canvas, "NEXUS OS  //  TERMINAL PORTÁTIL DEL TÉCNICO", 22, Cian, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(60, -34), new Vector2(900, 36));
            var linea = Icono("Linea", canvas, blanco, new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(1800, 2));
            linea.preserveAspect = false; linea.color = new Color(0.4f, 0.9f, 1f, 0.35f);

            Button Pestana(string nombre, string texto, float x, float anchoBtn)
            {
                var rt = Rect(nombre, canvas, new Vector2(1, 1), new Vector2(x, -30), new Vector2(anchoBtn, 44));
                var img = rt.gameObject.AddComponent<Image>(); img.color = new Color(0, 0, 0, 0.01f);
                var b = rt.gameObject.AddComponent<Button>();
                var col = b.colors; col.highlightedColor = new Color(0.4f, 0.9f, 1f, 0.25f); col.selectedColor = col.highlightedColor; b.colors = col;
                Texto("Texto", rt, texto, 22, gris, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(anchoBtn, 44));
                return b;
            }
            var tabObjetos = Pestana("Tab_Objetos", "OBJETOS", -600, 160);
            var tabArchivo = Pestana("Tab_Archivo", "ARCHIVO", -430, 160);
            var tabMapa = Pestana("Tab_Mapa", "MAPA", -270, 140);
            var cerrar = Pestana("Boton_Cerrar", "CERRAR [TAB]", -60, 200);
            Texto("Ayuda_Pestanas", canvas, "Q / R  CAMBIAR PESTAÑA", 13, new Color(0.45f, 0.55f, 0.6f), TextAlignmentOptions.Right, new Vector2(1, 1), new Vector2(-60, -66), new Vector2(500, 18));

            // ---------- Columna izquierda: el técnico ----------
            var marcoRetrato = Icono("Marco_Retrato", canvas, marco, new Vector2(0, 1), new Vector2(60, -110), new Vector2(230, 230));
            marcoRetrato.type = Image.Type.Sliced; marcoRetrato.preserveAspect = false; marcoRetrato.color = new Color(0.4f, 0.9f, 1f, 0.6f);
            var retrato = Icono("Retrato", marcoRetrato.rectTransform, retratos[0], new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(218, 218));
            var tNombre = Texto("Nombre", canvas, "NEYDER", 30, Color.white, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(310, -120), new Vector2(240, 120));
            tNombre.fontStyle = FontStyles.Bold;

            Texto("Etiqueta_Vitales", canvas, "SIGNOS VITALES", 15, gris, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(60, -360), new Vector2(440, 22));
            var marcoPulso = Icono("Monitor", canvas, marco, new Vector2(0, 1), new Vector2(60, -386), new Vector2(460, 130));
            marcoPulso.type = Image.Type.Sliced; marcoPulso.preserveAspect = false; marcoPulso.color = new Color(0.4f, 0.9f, 1f, 0.45f);
            var fondoMonitor = Icono("Fondo_Monitor", marcoPulso.rectTransform, blanco, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(450, 120));
            fondoMonitor.preserveAspect = false; fondoMonitor.color = new Color(0.01f, 0.06f, 0.05f, 1f);
            var rtPulso = Rect("Pulso", marcoPulso.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(450, 120));
            var pulso = rtPulso.gameObject.AddComponent<RawImage>();
            pulso.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaUI + "/Pulso.png");
            pulso.uvRect = new Rect(0, 0, 2, 1);
            pulso.raycastTarget = false;

            var tEstado = Texto("Estado", canvas, "ESTADO: ESTABLE", 28, new Color(0.4f, 1f, 0.6f), TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(60, -530), new Vector2(460, 36));
            tEstado.fontStyle = FontStyles.Bold;
            var corazones = new Image[EstadoPartida.SaludMaxima];
            for (int i = 0; i < corazones.Length; i++)
                corazones[i] = Icono("Corazon_" + i, canvas, corazon, new Vector2(0, 1), new Vector2(60 + i * 46, -576), new Vector2(38, 38));
            Texto("Etiqueta_Oportunidades", canvas, "OPORTUNIDADES DE EVASIÓN", 15, gris, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(206, -586), new Vector2(300, 22));
            var tMision = Texto("Mision", canvas, "", 21, new Color(0.85f, 0.9f, 0.92f), TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(60, -650), new Vector2(460, 200));

            // ---------- Página OBJETOS ----------
            var pagObjetos = Rect("Pagina_Objetos", canvas, new Vector2(0, 1), Vector2.zero, Vector2.zero); Estirar(pagObjetos, 580, 50, 50, 110);
            var marcoVista = Icono("Marco_Vista", pagObjetos, marco, new Vector2(0, 1), Vector2.zero, new Vector2(500, 500));
            marcoVista.type = Image.Type.Sliced; marcoVista.preserveAspect = false; marcoVista.color = new Color(0.4f, 0.9f, 1f, 0.45f);
            var vista = Icono("Vista_Previa", marcoVista.rectTransform, iconos[0], new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(430, 430));
            var tNombreObj = Texto("Nombre_Objeto", pagObjetos, "", 28, Cian, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, -520), new Vector2(520, 40));
            tNombreObj.fontStyle = FontStyles.Bold;
            var tDesc = Texto("Descripcion", pagObjetos, "", 22, new Color(0.85f, 0.88f, 0.9f), TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, -566), new Vector2(520, 180));
            var tAccion = Texto("Accion", pagObjetos, "", 18, new Color(1f, 0.78f, 0.3f), TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, -760), new Vector2(520, 30));

            // 12 ranuras en 3 columnas (con las armas y la munición caben hasta 12 objetos distintos)
            var ranuras = new RanuraInventario[12];
            for (int i = 0; i < ranuras.Length; i++)
            {
                int col = i % 3, fila = i / 3;
                var rt = Rect("Ranura_" + i, pagObjetos, new Vector2(1, 1), new Vector2(-(2 - col) * 235, -fila * 170), new Vector2(222, 150));
                var img = rt.gameObject.AddComponent<Image>();
                img.color = new Color(0.04f, 0.08f, 0.1f, 0.95f);
                var b = rt.gameObject.AddComponent<Button>();
                var colores = b.colors;
                colores.normalColor = new Color(0.55f, 0.65f, 0.7f);
                colores.highlightedColor = new Color(0.8f, 1f, 1f);
                colores.selectedColor = new Color(1.3f, 1.7f, 1.8f);
                colores.colorMultiplier = 1.5f;
                b.colors = colores;
                var m = Icono("Marco", rt, marco, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(222, 150));
                m.type = Image.Type.Sliced; m.preserveAspect = false; m.color = new Color(0.4f, 0.9f, 1f, 0.35f);
                var ic = Icono("Icono", rt, iconos[0], new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(124, 124));
                var tx = Texto("Cantidad", rt, "", 24, Color.white, TextAlignmentOptions.BottomRight, new Vector2(1, 0), new Vector2(-12, 8), new Vector2(110, 34));
                var r = rt.gameObject.AddComponent<RanuraInventario>();
                r.icono = ic; r.texto = tx; r.indice = i;
                ranuras[i] = r;
            }

            // ---------- Página ARCHIVO ----------
            var pagArchivo = Rect("Pagina_Archivo", canvas, new Vector2(0, 1), Vector2.zero, Vector2.zero); Estirar(pagArchivo, 580, 50, 50, 110);
            var lista = Rect("Lista_Notas", pagArchivo, new Vector2(0, 1), Vector2.zero, new Vector2(460, 860));
            var vlg = lista.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10; vlg.childForceExpandHeight = false; vlg.childControlHeight = false; vlg.childControlWidth = true;
            var plantilla = Rect("Plantilla_Nota", lista, new Vector2(0, 1), Vector2.zero, new Vector2(460, 56));
            var imgN = plantilla.gameObject.AddComponent<Image>(); imgN.color = new Color(0.04f, 0.08f, 0.1f, 0.95f);
            var bN = plantilla.gameObject.AddComponent<Button>();
            var cN = bN.colors; cN.selectedColor = new Color(0.3f, 0.7f, 0.8f); cN.highlightedColor = new Color(0.2f, 0.5f, 0.6f); bN.colors = cN;
            Texto("Texto", plantilla, "NOTA", 19, Color.white, TextAlignmentOptions.Left, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(430, 50));
            var tTituloNota = Texto("Titulo_Nota", pagArchivo, "", 26, Cian, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(520, 0), new Vector2(760, 40));
            AnchoHastaDerecha(tTituloNota.rectTransform, 520, 40);
            tTituloNota.fontStyle = FontStyles.Bold;
            var tNota = Texto("Texto_Nota", pagArchivo, "", 24, new Color(0.88f, 0.9f, 0.86f), TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(520, -60), new Vector2(760, 800));
            AnchoHastaDerecha(tNota.rectTransform, 520, 800);

            // ---------- Página MAPA ----------
            var pagMapa = Rect("Pagina_Mapa", canvas, new Vector2(0, 1), Vector2.zero, Vector2.zero); Estirar(pagMapa, 580, 50, 50, 110);
            var rtMapa = Rect("Mapa", pagMapa, new Vector2(0.5f, 0.5f), new Vector2(-140, 0), new Vector2(700, 860));
            var mapa = rtMapa.gameObject.AddComponent<RawImage>(); mapa.raycastTarget = false;
            var marcador = Icono("Marcador_Jugador", rtMapa, CargarSprite("Flecha"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28));
            marcador.color = new Color(1f, 0.25f, 0.2f);
            var etiqueta = Texto("Plantilla_Etiqueta", rtMapa, "SALA", 14, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 22));
            Texto("Leyenda", pagMapa,
                "<mark=#ff5040>   </mark>  PUERTA ROJA\n<mark=#4090ff>   </mark>  PUERTA AZUL\n<mark=#ffa020>   </mark>  SIN ENERGÍA\n<mark=#a0b4c0>   </mark>  PUERTA\n" +
                "<mark=#50ff80>   </mark>  TERMINAL\n<mark=#50d8ff>   </mark>  RESPALDO (USB)\n<mark=#ffb030>   </mark>  PANEL ELÉCTRICO\n<mark=#60b0ff>   </mark>  DISCO MAESTRO\n<mark=#ff4030>   </mark>  TU POSICIÓN",
                17, new Color(0.8f, 0.85f, 0.88f), TextAlignmentOptions.TopLeft, new Vector2(1, 1), new Vector2(0, -20), new Vector2(250, 400));

            // ---------- Componente ----------
            var pi = raiz.AddComponent<PantallaInventario>();
            var so = new SerializedObject(pi);
            void Ref(string campo, Object v) => so.FindProperty(campo).objectReferenceValue = v;
            void Arr<T>(string campo, T[] valores) where T : Object
            {
                var p = so.FindProperty(campo); p.arraySize = valores.Length;
                for (int i = 0; i < valores.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
            }
            Ref("raizCanvas", canvas.gameObject); Ref("canvasHud", raiz.transform.Find("Canvas_HUD").gameObject);
            Ref("paginaObjetos", pagObjetos.gameObject); Ref("paginaArchivo", pagArchivo.gameObject); Ref("paginaMapa", pagMapa.gameObject);
            Ref("tabObjetos", tabObjetos); Ref("tabArchivo", tabArchivo); Ref("tabMapa", tabMapa); Ref("botonCerrar", cerrar);
            Ref("retrato", retrato); Arr("retratos", retratos); Ref("textoNombre", tNombre); Ref("pulso", pulso); Ref("textoEstado", tEstado);
            Arr("corazones", corazones); Ref("corazonLleno", corazon); Ref("corazonVacio", corazonVacio); Ref("textoMision", tMision);
            Arr("ranuras", ranuras); Arr("iconos", iconos); Ref("vistaPrevia", vista);
            Ref("textoNombreObjeto", tNombreObj); Ref("textoDescripcion", tDesc); Ref("textoAccion", tAccion);
            Ref("listaNotas", lista); Ref("plantillaNota", bN); Ref("tituloNota", tTituloNota); Ref("textoNota", tNota);
            Ref("imagenMapa", mapa); Ref("marcadorJugador", marcador.rectTransform); Ref("plantillaEtiqueta", etiqueta);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Estirar(RectTransform rt, float izq, float abajo, float der, float arriba)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0, 1);
            rt.offsetMin = new Vector2(izq, abajo); rt.offsetMax = new Vector2(-der, -arriba);
        }

        static void AnchoHastaDerecha(RectTransform rt, float izq, float alto)
        {
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(-izq, alto);
            rt.anchoredPosition = new Vector2(izq, rt.anchoredPosition.y);
        }

        // ---------------- Recursos gráficos ----------------
        static void GenerarRecursosInventario()
        {
            // Marco de 9 cortes (solo el borde) para paneles y casillas
            Icono("Marco", 64, (x, y) => (Mathf.Abs(x) > 0.9f || Mathf.Abs(y) > 0.9f) ? Color.white : Color.clear);
            var tiM = (TextureImporter)AssetImporter.GetAtPath(RutaUI + "/Marco.png");
            tiM.spriteBorder = new Vector4(5, 5, 5, 5);
            tiM.SaveAndReimport();
            // Flecha del jugador en el mapa
            Icono("Flecha", 32, (x, y) => (y < 0.8f && y > -0.7f && Mathf.Abs(x) < (0.8f - y) * 0.55f) ? Color.white : Color.clear);

            // Línea de pulso (electrocardiograma) que se repite al desplazarla
            const int w = 512, h = 128;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pix = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pix[y * w + x] = (x % 32 == 0 || y % 32 == 0) ? new Color(1f, 1f, 1f, 0.08f) : Color.clear;
            float Latido(float u)
            {
                u = Mathf.Repeat(u, 0.5f) * 2f;   // dos latidos por textura
                float v = 0f;
                v += 0.07f * Mathf.Exp(-Mathf.Pow((u - 0.12f) / 0.03f, 2));
                v -= 0.08f * Mathf.Exp(-Mathf.Pow((u - 0.20f) / 0.012f, 2));
                v += 0.42f * Mathf.Exp(-Mathf.Pow((u - 0.23f) / 0.012f, 2));
                v -= 0.15f * Mathf.Exp(-Mathf.Pow((u - 0.26f) / 0.012f, 2));
                v += 0.11f * Mathf.Exp(-Mathf.Pow((u - 0.42f) / 0.04f, 2));
                return 0.4f + v;
            }
            int prev = (int)(Latido(0f) * h);
            for (int x = 0; x < w; x++)
            {
                int y = Mathf.Clamp((int)(Latido(x / (float)w) * h), 2, h - 3);
                for (int yy = Mathf.Min(prev, y) - 1; yy <= Mathf.Max(prev, y) + 1; yy++)
                    for (int dx = 0; dx < 2; dx++)
                        if (yy >= 0 && yy < h && x + dx < w) pix[yy * w + x + dx] = Color.white;
                prev = y;
            }
            t.SetPixels(pix); t.Apply();
            string ruta = RutaUI + "/Pulso.png";
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(ruta);
            var ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
            ti.textureType = TextureImporterType.Default; ti.wrapMode = TextureWrapMode.Repeat;
            ti.mipmapEnabled = false; ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }

        // ---------------- Renders de íconos y retratos ----------------
        /// <summary>Escena temporal con luz de estudio para fotografiar modelos 3D en un PNG.</summary>
        static Sprite Fotografiar(GameObject modelo, Vector3 posCamara, Vector3 mirar, float fov, Color fondo, string nombre, int tam)
        {
            var go = new GameObject("Camara_Foto");
            go.transform.position = posCamara;
            go.transform.LookAt(mirar);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.01f; cam.farClipPlane = 20f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = fondo;
            var datos = cam.GetUniversalAdditionalCameraData();
            datos.renderPostProcessing = false;
            var rt = new RenderTexture(tam, tam, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            var anterior = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, tam, tam), 0, 0);
            tex.Apply();
            RenderTexture.active = anterior;
            cam.targetTexture = null;
            Object.DestroyImmediate(go);
            rt.Release(); Object.DestroyImmediate(rt);

            string carpeta = RutaUI + "/Iconos";
            Directory.CreateDirectory(carpeta);
            string ruta = $"{carpeta}/{nombre}.png";
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(ruta);
            var ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
            ti.textureType = TextureImporterType.Sprite; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }

        static UnityEngine.SceneManagement.Scene AbrirEstudio(out Light clave)
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(escena);
            clave = new GameObject("Luz_Clave").AddComponent<Light>();
            clave.type = LightType.Directional; clave.intensity = 1.6f;
            clave.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
            var relleno = new GameObject("Luz_Relleno").AddComponent<Light>();
            relleno.type = LightType.Directional; relleno.intensity = 0.6f; relleno.color = new Color(0.6f, 0.8f, 1f);
            relleno.transform.rotation = Quaternion.Euler(20f, -140f, 0f);
            return escena;
        }

        static Sprite[] RenderizarIconos()
        {
            var escena = AbrirEstudio(out _);
            var lista = new Sprite[PrefabIcono.Length];
            for (int i = 0; i < PrefabIcono.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Nexus/Prefabs/{PrefabIcono[i]}.prefab");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, escena);
                go.transform.position = new Vector3(0f, -500f, 0f);   // lejos de cualquier nivel abierto
                foreach (var l in go.GetComponentsInChildren<Light>()) l.enabled = false;
                var visual = go.transform.Find("Visual");
                if (visual != null) visual.localRotation = Quaternion.Euler(-15f, 35f, 0f) * visual.localRotation;
                var b = new Bounds(go.transform.position, Vector3.zero);
                foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                float d = b.extents.magnitude / Mathf.Tan(14f * Mathf.Deg2Rad) * 1.05f;
                lista[i] = Fotografiar(go, b.center + new Vector3(0f, 0.15f, -1f).normalized * d, b.center, 28f, new Color(0, 0, 0, 0), "Icono_" + (TipoColeccionable)i, 256);
                Object.DestroyImmediate(go);
            }
            EditorSceneManager.CloseScene(escena, true);
            return lista;
        }

        static Sprite[] RenderizarRetratos()
        {
            var escena = AbrirEstudio(out var clave);
            clave.transform.rotation = Quaternion.Euler(20f, 20f, 0f);   // luz frontal: el modelo en pose T mira hacia -Z
            var lista = new Sprite[2];
            string[] modelos = { "Neyder", "Deisy" };
            for (int i = 0; i < modelos.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Nexus/Modelos/Personajes/{modelos[i]}.fbx");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, escena);
                go.transform.position = new Vector3(0f, -500f, 0f);
                var anim = go.GetComponent<Animator>();
                Transform cabeza = anim != null ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
                if (cabeza == null) foreach (var t in go.GetComponentsInChildren<Transform>()) if (t.name == "Head") cabeza = t;
                Vector3 c = cabeza.position + new Vector3(0f, 0.07f, 0f);
                lista[i] = Fotografiar(go, c + new Vector3(0f, 0.03f, -0.75f), c, 22f, new Color(0.03f, 0.09f, 0.11f, 1f), "Retrato_" + modelos[i], 512);
                Object.DestroyImmediate(go);
            }
            EditorSceneManager.CloseScene(escena, true);
            return lista;
        }
    }
}
