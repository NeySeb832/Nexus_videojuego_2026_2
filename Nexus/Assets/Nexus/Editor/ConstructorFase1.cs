using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Herramienta de editor de la Fase 1 (Modelado): genera las texturas procedurales,
    /// crea los materiales URP y arma los prefabs del entorno a partir de primitivas de Unity.
    /// Menú: Nexus → Fase 1.
    /// </summary>
    public static partial class ConstructorFase1
    {
        const string RutaTex = "Assets/Nexus/Texturas/Entorno";
        const string RutaMat = "Assets/Nexus/Materiales/Entorno";
        const string RutaPre = "Assets/Nexus/Prefabs";
        const string RutaMesh = "Assets/Nexus/Modelos/Entorno";

        // ---- Diseño del frente del rack (píxeles de la textura de 256 x 1024) ----
        const int FrenteTexAncho = 256, FrenteTexAlto = 1024;
        const int Cuchillas = 10, CuchillaAlto = 92, CuchillaPaso = 96, CuchillaInicio = 44;
        static readonly int[] LedPixelX = { 186, 202 };
        const int LedDesdeArriba = 16;

        // ---- Medidas reales del rack (metros) ----
        const float RackAncho = 0.6f, RackAlto = 2.0f, RackFondo = 1.0f;
        const float FrenteAncho = 0.56f, FrenteAlto = 1.92f, FrenteBase = 0.04f;

        [MenuItem("Nexus/Fase 1/Construir todo")]
        public static void ConstruirTodo()
        {
            GenerarTexturas();
            CrearMateriales();
            ConstruirPrefabs();
            ConstruirObjetos();
            Debug.Log("[Nexus] Fase 1 construida: texturas, materiales y prefabs.");
        }

        // =====================================================================
        //  1. TEXTURAS PROCEDURALES
        // =====================================================================

        /// <summary>Lienzo simple: color (sRGB) + mapa de alturas para derivar el normal map.</summary>
        class Lienzo
        {
            public readonly int Ancho, Alto;
            public readonly Color[] Color;
            public readonly float[] Altura;

            public Lienzo(int ancho, int alto, Color fondo, float altura = 0.5f)
            {
                Ancho = ancho; Alto = alto;
                Color = new Color[ancho * alto];
                Altura = new float[ancho * alto];
                for (int i = 0; i < Color.Length; i++) { Color[i] = fondo; Altura[i] = altura; }
            }

            public void Rect(int x0, int y0, int x1, int y1, Color c, float h = -1f)
            {
                x0 = Mathf.Clamp(x0, 0, Ancho); x1 = Mathf.Clamp(x1, 0, Ancho);
                y0 = Mathf.Clamp(y0, 0, Alto); y1 = Mathf.Clamp(y1, 0, Alto);
                for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        Color[y * Ancho + x] = c;
                        if (h >= 0f) Altura[y * Ancho + x] = h;
                    }
            }

            public void Disco(float cx, float cy, float r, Color c, float h = -1f)
            {
                int x0 = Mathf.FloorToInt(cx - r - 1), x1 = Mathf.CeilToInt(cx + r + 1);
                int y0 = Mathf.FloorToInt(cy - r - 1), y1 = Mathf.CeilToInt(cy + r + 1);
                for (int y = Mathf.Max(0, y0); y < Mathf.Min(Alto, y1); y++)
                    for (int x = Mathf.Max(0, x0); x < Mathf.Min(Ancho, x1); x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        float k = Mathf.Clamp01(r - d + 0.5f); // borde suavizado
                        if (k <= 0f) continue;
                        int i = y * Ancho + x;
                        Color[i] = UnityEngine.Color.Lerp(Color[i], c, k);
                        if (h >= 0f) Altura[i] = Mathf.Lerp(Altura[i], h, k);
                    }
            }

            /// <summary>Variación aleatoria de brillo por píxel (polvo, imperfecciones).</summary>
            public void Ruido(float amplitud, int semilla)
            {
                var rnd = new System.Random(semilla);
                for (int i = 0; i < Color.Length; i++)
                {
                    float n = 1f + ((float)rnd.NextDouble() * 2f - 1f) * amplitud;
                    Color[i] = new Color(Color[i].r * n, Color[i].g * n, Color[i].b * n, 1f);
                }
            }

            /// <summary>Vetas horizontales de metal cepillado.</summary>
            public void Cepillado(float amplitud, int semilla)
            {
                var rnd = new System.Random(semilla);
                float fila = 0f;
                for (int y = 0; y < Alto; y++)
                {
                    fila = Mathf.Lerp(fila, (float)rnd.NextDouble() * 2f - 1f, 0.5f);
                    for (int x = 0; x < Ancho; x++)
                    {
                        float n = 1f + (fila * 0.7f + ((float)rnd.NextDouble() * 2f - 1f) * 0.3f) * amplitud;
                        int i = y * Ancho + x;
                        Color[i] = new Color(Color[i].r * n, Color[i].g * n, Color[i].b * n, 1f);
                    }
                }
            }
        }

        static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

        [MenuItem("Nexus/Fase 1/1. Generar texturas")]
        public static void GenerarTexturas()
        {
            CrearCarpeta(RutaTex);
            TexturaFrenteRack();
            TexturaPiso();
            TexturaPared();
            TexturaPeligro();
            TexturaMetal();
            TexturaTarjeta("T_Tarjeta_Roja", C(0.72f, 0.07f, 0.06f));
            TexturaTarjeta("T_Tarjeta_Azul", C(0.08f, 0.28f, 0.82f));
            AssetDatabase.Refresh();
        }

        static void TexturaFrenteRack()
        {
            var l = new Lienzo(FrenteTexAncho, FrenteTexAlto, C(0.10f, 0.11f, 0.13f), 0.3f);
            Color riel = C(0.22f, 0.23f, 0.25f), oscuro = C(0.03f, 0.03f, 0.035f);

            // Rieles laterales con agujeros de montaje
            l.Rect(0, 0, 14, FrenteTexAlto, riel, 0.6f);
            l.Rect(242, 0, 256, FrenteTexAlto, riel, 0.6f);
            for (int y = 8; y < FrenteTexAlto - 8; y += 16)
            {
                l.Rect(5, y, 10, y + 6, oscuro, 0.1f);
                l.Rect(246, y, 251, y + 6, oscuro, 0.1f);
            }

            // Rejilla de ventilación inferior
            l.Rect(16, 4, 240, 40, C(0.16f, 0.17f, 0.19f), 0.5f);
            for (int y = 8; y < 36; y += 6) l.Rect(22, y, 234, y + 3, oscuro, 0.1f);

            // Cuchillas de servidor
            for (int i = 0; i < Cuchillas; i++)
            {
                int y0 = CuchillaInicio + i * CuchillaPaso, y1 = y0 + CuchillaAlto;
                l.Rect(16, y0, 240, y1, C(0.17f, 0.18f, 0.20f), 0.6f);
                l.Rect(16, y1 - 2, 240, y1, C(0.28f, 0.29f, 0.31f), 0.65f);   // brillo del borde
                l.Rect(16, y0, 240, y0 + 2, C(0.08f, 0.08f, 0.09f), 0.55f);   // sombra inferior

                // Malla perforada (patrón hexagonal de agujeros)
                l.Rect(22, y0 + 7, 172, y1 - 15, C(0.12f, 0.12f, 0.14f), 0.55f);
                for (int yy = y0 + 10; yy < y1 - 16; yy += 5)
                {
                    int desfase = ((yy / 5) % 2) * 3;
                    for (int xx = 25 + desfase; xx < 170; xx += 6) l.Disco(xx, yy, 1.6f, oscuro, 0.2f);
                }

                // Etiqueta con código de barras
                l.Rect(22, y1 - 12, 76, y1 - 5, C(0.62f, 0.63f, 0.60f), 0.62f);
                var rnd = new System.Random(i * 31 + 7);
                for (int k = 0; k < 16; k++)
                    if (rnd.NextDouble() > 0.45) l.Rect(25 + k * 3, y1 - 11, 26 + k * 3, y1 - 6, C(0.1f, 0.1f, 0.1f));

                // Huecos de los LEDs (los LEDs reales son geometría encima)
                foreach (int lx in LedPixelX)
                    l.Rect(lx - 5, y1 - LedDesdeArriba - 4, lx + 5, y1 - LedDesdeArriba + 4, C(0.04f, 0.04f, 0.045f), 0.45f);

                // Botón de encendido y asa
                l.Disco(194, y0 + 20, 3f, C(0.30f, 0.30f, 0.32f), 0.7f);
                l.Rect(222, y0 + 10, 232, y1 - 10, C(0.34f, 0.35f, 0.37f), 0.85f);
                l.Rect(222, y0 + 10, 224, y1 - 10, C(0.45f, 0.46f, 0.48f), 0.85f);
            }
            l.Rect(16, 1004, 240, 1020, C(0.14f, 0.15f, 0.17f), 0.5f);
            l.Ruido(0.05f, 11);

            Guardar(l, "T_Rack_Frente", TextureWrapMode.Clamp, true, 6f);
        }

        static void TexturaPiso()
        {
            // 512 px = 1,2 m: cuatro baldosas de suelo técnico de 60 cm, una de ellas perforada
            var l = new Lienzo(512, 512, C(0.30f, 0.31f, 0.33f), 0.6f);
            for (int ty = 0; ty < 2; ty++)
                for (int tx = 0; tx < 2; tx++)
                {
                    int x0 = tx * 256, y0 = ty * 256;
                    l.Rect(x0, y0, x0 + 256, y0 + 3, C(0.08f, 0.08f, 0.09f), 0f);
                    l.Rect(x0, y0, x0 + 3, y0 + 256, C(0.08f, 0.08f, 0.09f), 0f);
                    l.Rect(x0 + 3, y0 + 252, x0 + 256, y0 + 256, C(0.38f, 0.39f, 0.41f), 0.65f);
                    foreach (var (cx, cy) in new[] { (14, 14), (242, 14), (14, 242), (242, 242) })
                        l.Disco(x0 + cx, y0 + cy, 3.5f, C(0.18f, 0.18f, 0.2f), 0.4f);
                    if (tx == 1 && ty == 0)
                        for (int yy = y0 + 30; yy < y0 + 228; yy += 12)
                            for (int xx = x0 + 30; xx < x0 + 228; xx += 12)
                                l.Disco(xx, yy, 3.2f, C(0.05f, 0.05f, 0.06f), 0.1f);
                }
            l.Ruido(0.06f, 21);
            Guardar(l, "T_Piso", TextureWrapMode.Repeat, true, 4f);
        }

        static void TexturaPared()
        {
            // 512 px = 1,5 m: paneles metálicos con juntas, banda horizontal y zócalo
            var l = new Lienzo(512, 512, C(0.20f, 0.21f, 0.23f), 0.6f);
            l.Rect(0, 0, 512, 60, C(0.12f, 0.12f, 0.13f), 0.5f);
            l.Rect(0, 300, 512, 340, C(0.14f, 0.15f, 0.16f), 0.45f);
            l.Rect(0, 318, 512, 322, C(0.32f, 0.33f, 0.35f), 0.55f);
            foreach (int x in new[] { 0, 256 })
            {
                l.Rect(x, 0, x + 4, 512, C(0.07f, 0.07f, 0.08f), 0f);
                for (int y = 20; y < 512; y += 40) { l.Disco(x + 12, y, 3f, C(0.32f, 0.33f, 0.35f), 0.8f); l.Disco(x + 244, y, 3f, C(0.32f, 0.33f, 0.35f), 0.8f); }
            }
            l.Ruido(0.05f, 31);
            Guardar(l, "T_Pared", TextureWrapMode.Repeat, true, 4f);
        }

        static void TexturaPeligro()
        {
            var l = new Lienzo(256, 256, C(0.06f, 0.06f, 0.06f), 0.5f);
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                    if (((x + y) / 32) % 2 == 0) l.Color[y * 256 + x] = C(0.85f, 0.62f, 0.05f);
            l.Ruido(0.08f, 41);
            Guardar(l, "T_Peligro", TextureWrapMode.Repeat, false, 0f);
        }

        static void TexturaMetal()
        {
            var l = new Lienzo(256, 256, C(0.55f, 0.56f, 0.58f), 0.5f);
            l.Cepillado(0.12f, 51);
            Guardar(l, "T_Metal_Cepillado", TextureWrapMode.Repeat, false, 0f);
        }

        static void TexturaTarjeta(string nombre, Color colorBase)
        {
            var l = new Lienzo(256, 160, colorBase, 0.5f);
            l.Rect(0, 104, 256, 122, C(0.92f, 0.92f, 0.9f));           // banda blanca
            l.Rect(26, 34, 70, 70, C(0.85f, 0.68f, 0.28f));            // chip
            for (int k = 0; k < 4; k++) l.Rect(26, 40 + k * 8, 70, 41 + k * 8, C(0.55f, 0.42f, 0.15f));
            l.Disco(204, 56, 26f, C(0.95f, 0.95f, 0.95f));             // logotipo: anillo
            l.Disco(204, 56, 18f, colorBase);
            l.Disco(204, 56, 7f, C(0.95f, 0.95f, 0.95f));
            for (int k = 0; k < 6; k++) l.Rect(100, 30 + k * 7, 160 - k * 6, 33 + k * 7, C(0.95f, 0.95f, 0.95f) * 0.85f);
            l.Ruido(0.03f, 61);
            Guardar(l, nombre, TextureWrapMode.Clamp, false, 0f);
        }

        static void Guardar(Lienzo l, string nombre, TextureWrapMode envoltura, bool conNormal, float fuerza)
        {
            EscribirPng(l.Color, l.Ancho, l.Alto, $"{RutaTex}/{nombre}.png");
            if (conNormal)
            {
                var n = new Color[l.Color.Length];
                for (int y = 0; y < l.Alto; y++)
                    for (int x = 0; x < l.Ancho; x++)
                    {
                        float hx = l.Altura[y * l.Ancho + Mathf.Min(x + 1, l.Ancho - 1)] - l.Altura[y * l.Ancho + Mathf.Max(x - 1, 0)];
                        float hy = l.Altura[Mathf.Min(y + 1, l.Alto - 1) * l.Ancho + x] - l.Altura[Mathf.Max(y - 1, 0) * l.Ancho + x];
                        var v = new Vector3(-hx * fuerza, -hy * fuerza, 1f).normalized;
                        n[y * l.Ancho + x] = new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f, 1f);
                    }
                EscribirPng(n, l.Ancho, l.Alto, $"{RutaTex}/{nombre}_N.png");
            }
            AssetDatabase.Refresh();
            ConfigurarImportacion($"{RutaTex}/{nombre}.png", false, envoltura);
            if (conNormal) ConfigurarImportacion($"{RutaTex}/{nombre}_N.png", true, envoltura);
        }

        static void EscribirPng(Color[] pixeles, int ancho, int alto, string ruta)
        {
            var t = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            t.SetPixels(pixeles);
            t.Apply();
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }

        static void ConfigurarImportacion(string ruta, bool esNormal, TextureWrapMode envoltura)
        {
            AssetDatabase.ImportAsset(ruta);
            var ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
            ti.textureType = esNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !esNormal;
            ti.wrapMode = envoltura;
            ti.anisoLevel = 4;
            ti.mipmapEnabled = true;
            ti.maxTextureSize = 1024;
            ti.SaveAndReimport();
        }

        // =====================================================================
        //  2. MATERIALES
        // =====================================================================

        [MenuItem("Nexus/Fase 1/2. Crear materiales")]
        public static void CrearMateriales()
        {
            CrearCarpeta(RutaMat);
            Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{RutaTex}/{n}.png");

            // Metálico bajo: sin cielo que reflejar, un metal puro se vería negro en salas oscuras
            Lit("M_Rack_Frente", T("T_Rack_Frente"), T("T_Rack_Frente_N"), C(1.3f, 1.3f, 1.35f), 0.2f, 0.45f);
            Lit("M_Rack_Cuerpo", T("T_Metal_Cepillado"), null, C(0.32f, 0.33f, 0.36f), 0.3f, 0.45f);
            Lit("M_Piso", T("T_Piso"), T("T_Piso_N"), Color.white, 0.1f, 0.4f);
            Lit("M_Pared", T("T_Pared"), T("T_Pared_N"), C(1.2f, 1.2f, 1.25f), 0.15f, 0.3f);
            Lit("M_Peligro", T("T_Peligro"), null, Color.white, 0.1f, 0.3f);
            Lit("M_Puerta", T("T_Pared"), T("T_Pared_N"), C(0.75f, 0.78f, 0.82f), 0.3f, 0.5f);
            Lit("M_Terminal", null, null, C(0.2f, 0.21f, 0.23f), 0.2f, 0.4f);
            Lit("M_Disco_Metal", T("T_Metal_Cepillado"), null, C(0.85f, 0.87f, 0.9f), 0.95f, 0.75f);

            var roja = Lit("M_Tarjeta_Roja", T("T_Tarjeta_Roja"), null, Color.white, 0f, 0.8f);
            Emision(roja, T("T_Tarjeta_Roja"), 1.6f);
            var azul = Lit("M_Tarjeta_Azul", T("T_Tarjeta_Azul"), null, Color.white, 0f, 0.8f);
            Emision(azul, T("T_Tarjeta_Azul"), 1.6f);

            Led("M_LED_Rack", true, Color.white, 3.5f, true, 0f);
            Led("M_LED_Lector", false, new Color(1f, 0.1f, 0.05f) * 4f, 1f, false, 0f);
            Led("M_Disco_Neon", false, new Color(0.1f, 0.5f, 1f) * 9f, 1f, false, 1f);
            Led("M_Pantalla", false, new Color(0.02f, 0.12f, 0.06f), 1f, false, 0f);
            AssetDatabase.SaveAssets();
        }

        static Material Lit(string nombre, Texture2D baseMap, Texture2D normal, Color color, float metalico, float suavidad)
        {
            var m = ObtenerMaterial(nombre, Shader.Find("Universal Render Pipeline/Lit"));
            m.SetTexture("_BaseMap", baseMap);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_WorkflowMode", 1f);
            m.SetFloat("_Metallic", metalico);
            m.SetFloat("_Smoothness", suavidad);
            m.SetTexture("_BumpMap", normal);
            if (normal != null) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Emision(Material m, Texture2D mapa, float intensidad)
        {
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", mapa);
            m.SetColor("_EmissionColor", Color.white * intensidad);
            // Con "None" URP considera la emisión como negra y apaga _EMISSION al validar el material
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
        }

        static Material Led(string nombre, bool colorVertice, Color color, float intensidad, bool parpadeo, float pulso)
        {
            var m = ObtenerMaterial(nombre, Shader.Find("Nexus/LED Parpadeo"));
            m.SetColor("_Color", color);
            m.SetFloat("_UsarColorVertice", colorVertice ? 1f : 0f);
            m.SetFloat("_Intensidad", intensidad);
            m.SetFloat("_Parpadeo", parpadeo ? 1f : 0f);
            m.SetFloat("_Pulso", pulso);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material ObtenerMaterial(string nombre, Shader shader)
        {
            string ruta = $"{RutaMat}/{nombre}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, ruta);
            }
            else m.shader = shader;
            return m;
        }

        // =====================================================================
        //  3. PREFABS
        // =====================================================================

        static Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>($"{RutaMat}/{n}.mat");

        [MenuItem("Nexus/Fase 1/3. Construir prefabs")]
        public static void ConstruirPrefabs()
        {
            CrearCarpeta(RutaPre + "/Entorno");
            CrearCarpeta(RutaPre + "/Coleccionables");
            CrearCarpeta(RutaPre + "/Interactivos");
            CrearCarpeta(RutaMesh);

            PrefabRack();
            PrefabDisco();
            PrefabTarjeta("Tarjeta_Acceso_Roja", TipoColeccionable.TarjetaRoja, "Tarjeta de acceso roja", "M_Tarjeta_Roja", new Color(1f, 0.2f, 0.15f));
            PrefabTarjeta("Tarjeta_Acceso_Azul", TipoColeccionable.TarjetaAzul, "Tarjeta de acceso azul", "M_Tarjeta_Azul", new Color(0.2f, 0.5f, 1f));
            PrefabPuerta("Puerta_Seguridad_Roja", LlaveRequerida.Roja);
            PrefabPuerta("Puerta_Seguridad_Azul", LlaveRequerida.Azul);
            PrefabPuerta("Puerta_Seguridad_Libre", LlaveRequerida.Ninguna);
            PrefabTerminal();
            PrefabModulo("Modulo_Piso_3x3", new Vector3(3f, 0.1f, 3f), new Vector3(0f, -0.05f, 0f), "M_Piso", new Vector2(2.5f, 2.5f));
            PrefabModulo("Modulo_Pared_3x3", new Vector3(3f, 3f, 0.2f), new Vector3(0f, 1.5f, 0f), "M_Pared", new Vector2(2f, 2f));
            AssetDatabase.SaveAssets();
        }

        /// <summary>Crea una primitiva con material, sin collider propio.</summary>
        static GameObject Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, string material, Vector3? rot = null)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(rot ?? Vector3.zero);
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = M(material);
            return go;
        }

        static void Guardar(GameObject raiz, string carpeta)
        {
            PrefabUtility.SaveAsPrefabAsset(raiz, $"{RutaPre}/{carpeta}/{raiz.name}.prefab");
            Object.DestroyImmediate(raiz);
        }

        static void MarcarEstatico(GameObject go, bool combinar = true)
        {
            var flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic |
                        StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
            if (combinar) flags |= StaticEditorFlags.BatchingStatic;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
        }

        // ---------- Rack de servidores ----------
        static void PrefabRack()
        {
            var raiz = new GameObject("Rack_Servidor");
            var col = raiz.AddComponent<BoxCollider>();
            col.size = new Vector3(RackAncho, RackAlto, RackFondo);
            col.center = new Vector3(0f, RackAlto / 2f, 0f);

            Pieza(PrimitiveType.Cube, "Cuerpo", raiz.transform, new Vector3(0f, RackAlto / 2f, 0f), new Vector3(RackAncho, RackAlto, RackFondo), "M_Rack_Cuerpo");
            float yFrente = FrenteBase + FrenteAlto / 2f;
            Pieza(PrimitiveType.Quad, "Frente", raiz.transform, new Vector3(0f, yFrente, -RackFondo / 2f - 0.0015f), new Vector3(FrenteAncho, FrenteAlto, 1f), "M_Rack_Frente");
            Pieza(PrimitiveType.Quad, "Trasera", raiz.transform, new Vector3(0f, yFrente, RackFondo / 2f + 0.0015f), new Vector3(FrenteAncho, FrenteAlto, 1f), "M_Rack_Frente", new Vector3(0f, 180f, 0f));

            var leds = new GameObject("LEDs");
            leds.transform.SetParent(raiz.transform, false);
            leds.AddComponent<MeshFilter>().sharedMesh = MeshLedsRack();
            var mr = leds.AddComponent<MeshRenderer>();
            mr.sharedMaterial = M("M_LED_Rack");
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            foreach (Transform t in raiz.transform) MarcarEstatico(t.gameObject, t.name != "LEDs");
            MarcarEstatico(raiz);
            Guardar(raiz, "Entorno");
        }

        /// <summary>
        /// Todos los LEDs del rack en un solo mesh (un único draw call). Cada LED es una cajita
        /// cuyo color de vértice guarda su color (RGB) y su desfase de parpadeo (A).
        /// </summary>
        static Mesh MeshLedsRack()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var c = new List<Color>(); var tri = new List<int>();
            var rnd = new System.Random(1234);
            Color[] paleta = { new Color(0.1f, 1f, 0.25f), new Color(0.15f, 0.45f, 1f), new Color(1f, 0.55f, 0.05f), new Color(1f, 0.08f, 0.05f) };

            foreach (int lado in new[] { -1, 1 })           // -1 = frente (-Z), 1 = trasera (+Z)
                for (int i = 0; i < Cuchillas; i++)
                    foreach (int px in LedPixelX)
                    {
                        int pyCentro = CuchillaInicio + i * CuchillaPaso + CuchillaAlto - LedDesdeArriba;
                        float xLocal = -FrenteAncho / 2f + (px + 0.5f) / FrenteTexAncho * FrenteAncho;
                        if (lado == 1) xLocal = -xLocal;     // la cara trasera está girada 180°
                        float yLocal = FrenteBase + (pyCentro + 0.5f) / FrenteTexAlto * FrenteAlto;
                        double r = rnd.NextDouble();
                        Color col = r < 0.6 ? paleta[0] : r < 0.85 ? paleta[1] : r < 0.97 ? paleta[2] : paleta[3];
                        col.a = (float)rnd.NextDouble();
                        var centro = new Vector3(xLocal, yLocal, lado * (RackFondo / 2f + 0.004f));
                        AgregarCaja(v, n, c, tri, centro, new Vector3(0.016f, 0.01f, 0.008f), col);
                    }

            string ruta = $"{RutaMesh}/Mesh_Rack_LEDs.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (mesh == null) { mesh = new Mesh { name = "Mesh_Rack_LEDs" }; AssetDatabase.CreateAsset(mesh, ruta); }
            mesh.Clear();
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetColors(c); mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static void AgregarCaja(List<Vector3> v, List<Vector3> n, List<Color> c, List<int> tri, Vector3 centro, Vector3 tam, Color col)
        {
            Vector3 h = tam * 0.5f;
            Vector3[] normales = { Vector3.forward, Vector3.back, Vector3.up, Vector3.down, Vector3.right, Vector3.left };
            foreach (var nor in normales)
            {
                Vector3 a = Vector3.Cross(nor, Mathf.Abs(nor.y) > 0.5f ? Vector3.forward : Vector3.up);
                Vector3 b = Vector3.Cross(nor, a);
                int baseIdx = v.Count;
                foreach (var (sa, sb) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
                {
                    v.Add(centro + Vector3.Scale(nor, h) + Vector3.Scale(a * sa, h) + Vector3.Scale(b * sb, h));
                    n.Add(nor); c.Add(col);
                }
                // Orden horario visto desde fuera (convención de Unity para caras frontales)
                tri.AddRange(new[] { baseIdx, baseIdx + 1, baseIdx + 2, baseIdx, baseIdx + 2, baseIdx + 3 });
            }
        }

        // ---------- Disco Duro Maestro ----------
        static void PrefabDisco()
        {
            var raiz = new GameObject("Disco_Duro_Maestro");
            var col = raiz.AddComponent<SphereCollider>();
            col.isTrigger = true; col.radius = 0.6f;
            var cole = raiz.AddComponent<Coleccionable>();
            Configurar(cole, TipoColeccionable.DiscoDuro, "Disco Duro Maestro");

            var visual = new GameObject("Visual");
            visual.transform.SetParent(raiz.transform, false);
            visual.AddComponent<FlotarGirar>();
            Pieza(PrimitiveType.Cube, "Cuerpo", visual.transform, Vector3.zero, Vector3.one * 0.26f, "M_Disco_Metal");
            Pieza(PrimitiveType.Cube, "Anillo_Neon", visual.transform, Vector3.zero, new Vector3(0.272f, 0.045f, 0.272f), "M_Disco_Neon");
            Pieza(PrimitiveType.Cube, "Tapa_Neon", visual.transform, new Vector3(0f, 0.131f, 0f), new Vector3(0.16f, 0.006f, 0.16f), "M_Disco_Neon");
            Pieza(PrimitiveType.Cube, "Base_Neon", visual.transform, new Vector3(0f, -0.131f, 0f), new Vector3(0.16f, 0.006f, 0.16f), "M_Disco_Neon");
            foreach (var r in visual.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;

            var luz = new GameObject("Luz_Azul").AddComponent<Light>();
            luz.transform.SetParent(raiz.transform, false);
            luz.type = LightType.Point;
            luz.color = new Color(0.25f, 0.55f, 1f);
            luz.range = 5f; luz.intensity = 7f;
            luz.shadows = LightShadows.None;
            Guardar(raiz, "Coleccionables");
        }

        // ---------- Tarjetas de acceso ----------
        static void PrefabTarjeta(string nombre, TipoColeccionable tipo, string visible, string material, Color colorLuz)
        {
            var raiz = new GameObject(nombre);
            var col = raiz.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = Vector3.one * 0.8f;
            Configurar(raiz.AddComponent<Coleccionable>(), tipo, visible);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(raiz.transform, false);
            visual.AddComponent<FlotarGirar>();
            var tarjeta = Pieza(PrimitiveType.Cube, "Tarjeta", visual.transform, Vector3.zero, new Vector3(0.22f, 0.138f, 0.008f), material);
            tarjeta.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(raiz.transform, false);
            luz.type = LightType.Point; luz.color = colorLuz;
            luz.range = 1.6f; luz.intensity = 1.5f; luz.shadows = LightShadows.None;
            Guardar(raiz, "Coleccionables");
        }

        static void Configurar(Coleccionable c, TipoColeccionable tipo, string visible)
        {
            var so = new SerializedObject(c);
            so.FindProperty("tipo").enumValueIndex = (int)tipo;
            so.FindProperty("nombreVisible").stringValue = visible;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Puerta de seguridad ----------
        static void PrefabPuerta(string nombre, LlaveRequerida llave)
        {
            var raiz = new GameObject(nombre);
            var zona = raiz.AddComponent<BoxCollider>();
            zona.isTrigger = true; zona.size = new Vector3(2.4f, 2.6f, 3.2f); zona.center = new Vector3(0f, 1.3f, 0f);
            var puerta = raiz.AddComponent<PuertaSeguridad>();

            Pieza(PrimitiveType.Cube, "Marco_Izq", raiz.transform, new Vector3(-1.125f, 1.45f, 0f), new Vector3(0.25f, 2.9f, 0.45f), "M_Peligro");
            Pieza(PrimitiveType.Cube, "Marco_Der", raiz.transform, new Vector3(1.125f, 1.45f, 0f), new Vector3(0.25f, 2.9f, 0.45f), "M_Peligro");
            Pieza(PrimitiveType.Cube, "Dintel", raiz.transform, new Vector3(0f, 2.75f, 0f), new Vector3(2.0f, 0.3f, 0.45f), "M_Peligro");
            var izq = Pieza(PrimitiveType.Cube, "Hoja_Izq", raiz.transform, new Vector3(-0.5f, 1.3f, 0f), new Vector3(1.0f, 2.6f, 0.1f), "M_Puerta");
            var der = Pieza(PrimitiveType.Cube, "Hoja_Der", raiz.transform, new Vector3(0.5f, 1.3f, 0f), new Vector3(1.0f, 2.6f, 0.1f), "M_Puerta");
            izq.AddComponent<BoxCollider>(); der.AddComponent<BoxCollider>();
            Pieza(PrimitiveType.Cube, "Lector", raiz.transform, new Vector3(1.125f, 1.3f, -0.25f), new Vector3(0.16f, 0.24f, 0.05f), "M_Terminal");
            var luz = Pieza(PrimitiveType.Cube, "Luz_Lector", raiz.transform, new Vector3(1.125f, 1.36f, -0.278f), new Vector3(0.1f, 0.03f, 0.01f), "M_LED_Lector");

            var bloqueo = new GameObject("Bloqueo_Navegacion").AddComponent<NavMeshObstacle>();
            bloqueo.transform.SetParent(raiz.transform, false);
            bloqueo.shape = NavMeshObstacleShape.Box;
            bloqueo.size = new Vector3(2.0f, 2.6f, 0.3f); bloqueo.center = new Vector3(0f, 1.3f, 0f);
            bloqueo.carving = true;

            var audio = raiz.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.spatialBlend = 1f;

            var so = new SerializedObject(puerta);
            so.FindProperty("llave").enumValueIndex = (int)llave;
            so.FindProperty("hojaIzquierda").objectReferenceValue = izq.transform;
            so.FindProperty("hojaDerecha").objectReferenceValue = der.transform;
            so.FindProperty("luzLector").objectReferenceValue = luz.GetComponent<MeshRenderer>();
            so.FindProperty("bloqueoNavegacion").objectReferenceValue = bloqueo;
            so.FindProperty("audioPuerta").objectReferenceValue = audio;
            so.ApplyModifiedPropertiesWithoutUndo();
            Guardar(raiz, "Interactivos");
        }

        // ---------- Terminal ----------
        static void PrefabTerminal()
        {
            var raiz = new GameObject("Terminal_Nexus");
            var zona = raiz.AddComponent<BoxCollider>();
            zona.isTrigger = true; zona.size = new Vector3(1.6f, 2f, 1.6f); zona.center = new Vector3(0f, 1f, -0.9f);
            var terminal = raiz.AddComponent<TerminalNexus>();

            var consola = Pieza(PrimitiveType.Cube, "Consola", raiz.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 1.0f, 0.55f), "M_Terminal");
            consola.AddComponent<BoxCollider>();
            Pieza(PrimitiveType.Cube, "Teclado", raiz.transform, new Vector3(0f, 1.02f, -0.12f), new Vector3(0.7f, 0.04f, 0.25f), "M_Rack_Cuerpo", new Vector3(10f, 0f, 0f));

            var pivote = new GameObject("Monitor");
            pivote.transform.SetParent(raiz.transform, false);
            pivote.transform.localPosition = new Vector3(0f, 1.38f, 0.12f);
            pivote.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            Pieza(PrimitiveType.Cube, "Carcasa", pivote.transform, Vector3.zero, new Vector3(0.8f, 0.52f, 0.06f), "M_Terminal");
            Pieza(PrimitiveType.Quad, "Pantalla", pivote.transform, new Vector3(0f, 0f, -0.031f), new Vector3(0.74f, 0.46f, 1f), "M_Pantalla");

            var texto = new GameObject("Texto_Pantalla");
            texto.transform.SetParent(pivote.transform, false);
            texto.transform.localPosition = new Vector3(0f, 0f, -0.033f);
            var tmp = texto.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = new Vector2(0.68f, 0.40f);
            tmp.enableAutoSizing = true; tmp.fontSizeMin = 0.1f; tmp.fontSizeMax = 0.5f;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = new Color(0.35f, 1f, 0.55f);
            tmp.text = "NEXUS OS v3.2\n> ...";

            var so = new SerializedObject(terminal);
            so.FindProperty("textoPantalla").objectReferenceValue = tmp;
            so.ApplyModifiedPropertiesWithoutUndo();
            Guardar(raiz, "Interactivos");
        }

        // ---------- Módulos de construcción (suelo y pared de 3 m) ----------
        static void PrefabModulo(string nombre, Vector3 tam, Vector3 centro, string material, Vector2 repeticion)
        {
            // Material propio con la repetición de textura adecuada al tamaño del módulo
            var baseMat = M(material);
            var mat = ObtenerMaterial(material + "_Modulo", baseMat.shader);
            mat.CopyPropertiesFromMaterial(baseMat);
            mat.SetTextureScale("_BaseMap", repeticion);
            EditorUtility.SetDirty(mat);

            var raiz = GameObject.CreatePrimitive(PrimitiveType.Cube);
            raiz.name = nombre;
            Object.DestroyImmediate(raiz.GetComponent<Collider>());
            var hijo = raiz; // el cubo se escala en un hijo para que la raíz quede a escala 1
            var contenedor = new GameObject(nombre);
            hijo.name = "Malla";
            hijo.transform.SetParent(contenedor.transform, false);
            hijo.transform.localPosition = centro;
            hijo.transform.localScale = tam;
            hijo.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var col = contenedor.AddComponent<BoxCollider>();
            col.size = tam; col.center = centro;
            MarcarEstatico(contenedor); MarcarEstatico(hijo);
            Guardar(contenedor, "Entorno");
        }

        static void CrearCarpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta)) return;
            string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
            CrearCarpeta(padre);
            AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
        }
    }
}
