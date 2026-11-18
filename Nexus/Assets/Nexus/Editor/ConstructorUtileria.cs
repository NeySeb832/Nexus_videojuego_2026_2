using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Utilería y ambientación de la empresa abandonada: oficina, cafetería, almacén, zona técnica,
    /// basura y efectos. Todo se modela con primitivas y texturas generadas por código.
    ///
    /// Para que el juego corra en Android y WebGL, las piezas de cada objeto se COMBINAN en una
    /// malla por material (un escritorio con monitor, teclado y torre = 5 llamadas de dibujo en
    /// lugar de 20). Convención: el pivote va en el suelo y el respaldo del objeto toca z = 0;
    /// el objeto se extiende hacia -Z (así se apoya contra una pared mirando hacia la sala).
    ///
    /// Menú: Nexus → Fase 1 → 5. Utilería y ambientación.
    /// </summary>
    public static partial class ConstructorFase1
    {
        const string RutaMeshUtl = "Assets/Nexus/Modelos/Utileria";
        const string CarpetaUtl = "Utileria";

        [MenuItem("Nexus/Fase 1/5. Utilería y ambientación")]
        public static void ConstruirUtileria()
        {
            CrearCarpeta(RutaTex); CrearCarpeta(RutaMat); CrearCarpeta(RutaMeshUtl);
            CrearCarpeta(RutaPre + "/" + CarpetaUtl); CrearCarpeta(RutaPre + "/Efectos");

            TexturasUtileria();
            MaterialesUtileria();
            ConstruirEfectos();   // primero: el gabinete abierto lleva el chispazo

            // Oficina
            PrefabEscritorio("Escritorio_PC", "M_Utl_Pantalla_Error", 0);
            PrefabEscritorio("Escritorio_PC_Codigo", "M_Utl_Pantalla_Codigo", 0);
            PrefabEscritorio("Escritorio_PC_Apagado", "M_Utl_Pantalla_Apagada", 1);
            PrefabEscritorio("Escritorio_Desordenado", null, 2);
            PrefabIslaEscritorios();
            PrefabSillaOficina();
            PrefabArchivador();
            PrefabCasillero("Casillero", false);
            PrefabCasillero("Casillero_Abierto", true);
            PrefabMonitoresSeguridad();
            PrefabPizarra();
            PrefabCartel("Cartel_Nexus", new Color(0.03f, 0.08f, 0.14f), new Color(0.1f, 0.85f, 1f), "NEXUS CORP.", "Conectando el futuro.\n<size=60%>La información es poder.</size>");
            PrefabCartel("Cartel_Seguridad", new Color(0.1f, 0.09f, 0.02f), new Color(1f, 0.75f, 0.05f), "ATENCIÓN", "Unidades SENTINEL en servicio.\n<size=60%>No obstruya su ruta de patrulla.\nNo corra en los pasillos.</size>");
            PrefabCartel("Cartel_Empleado", new Color(0.12f, 0.04f, 0.06f), new Color(1f, 0.35f, 0.3f), "EMPLEADO DEL MES", "<size=70%>SENTINEL-04\n\"Cero incidentes. Cero descansos.\"</size>");
            PrefabPlanta();
            PrefabDispensadorAgua();

            // Cafetería
            PrefabMaquina("Maquina_Bebidas", TipoColeccionable.BebidaEnergetica, 2, new Color(0.04f, 0.32f, 0.12f), "NEXUS FRESH", false);
            PrefabMaquina("Maquina_Snacks", TipoColeccionable.BarritaProteina, 1, new Color(0.45f, 0.12f, 0.03f), "SNACK·BYTE", false);
            PrefabMaquina("Maquina_Rota", TipoColeccionable.BebidaEnergetica, 0, new Color(0.2f, 0.07f, 0.06f), "FUERA DE SERVICIO", true);
            PrefabMesaCafeteria();
            PrefabSillaPlastica();
            PrefabMeson();
            PrefabNevera();

            // Almacén y zona técnica
            PrefabEstanteria();
            for (int i = 0; i < 3; i++) PrefabPilaCajas(i);
            PrefabBobinaCable();
            PrefabGabinete("Gabinete_Electrico", false);
            PrefabGabinete("Gabinete_Electrico_Abierto", true);
            PrefabCarrito();
            PrefabBancoTrabajo();

            // Basura y abandono
            ConstruirBasura();
            AssetDatabase.SaveAssets();
            Debug.Log("[Nexus] Utilería y ambientación construidas.");
        }

        // =====================================================================
        //  TEXTURAS
        // =====================================================================
        static void Linea(Lienzo l, float x0, float y0, float x1, float y1, Color c, float grosor)
        {
            int pasos = Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)) / Mathf.Max(0.5f, grosor * 0.5f));
            for (int i = 0; i <= pasos; i++)
            {
                float t = pasos == 0 ? 0f : i / (float)pasos;
                l.Disco(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), grosor * 0.5f, c);
            }
        }

        /// <summary>Renglones de "texto" (bloques) para pantallas y papeles.</summary>
        static void Renglones(Lienzo l, System.Random rnd, int x0, int x1, int yArriba, int yAbajo, int alto, int paso, Color c, bool sangria)
        {
            for (int y = yArriba; y > yAbajo; y -= paso)
            {
                int x = x0 + (sangria ? rnd.Next(0, 4) * 8 : 0);
                int fin = x1 - rnd.Next(0, (x1 - x0) / 2);
                while (x < fin)
                {
                    int w = rnd.Next(4, 18);
                    l.Rect(x, y, Mathf.Min(x + w, fin), y + alto, c);
                    x += w + rnd.Next(3, 6);
                }
            }
        }

        static void TexturasUtileria()
        {
            // Cartón corrugado con cinta de embalaje y flecha "este lado arriba"
            var carton = new Lienzo(256, 256, C(0.52f, 0.38f, 0.22f));
            for (int x = 0; x < 256; x += 5) carton.Rect(x, 0, x + 1, 256, C(0.47f, 0.34f, 0.2f));
            carton.Rect(0, 114, 256, 142, C(0.7f, 0.6f, 0.42f));
            carton.Rect(30, 40, 34, 80, C(0.12f, 0.1f, 0.08f));
            Linea(carton, 32, 82, 22, 66, C(0.12f, 0.1f, 0.08f), 4); Linea(carton, 32, 82, 42, 66, C(0.12f, 0.1f, 0.08f), 4);
            carton.Rect(170, 30, 230, 62, C(0.12f, 0.1f, 0.08f));
            carton.Rect(174, 34, 226, 58, C(0.52f, 0.38f, 0.22f));
            carton.Ruido(0.07f, 101);
            Guardar(carton, "T_Carton", TextureWrapMode.Repeat, false, 0f);

            // Pantalla azul de error del sistema
            var rnd = new System.Random(102);
            var error = new Lienzo(256, 160, C(0.02f, 0.06f, 0.28f));
            error.Rect(0, 140, 256, 160, C(0.05f, 0.18f, 0.55f));
            Renglones(error, rnd, 14, 240, 128, 60, 3, 9, C(0.6f, 0.7f, 0.9f), false);
            error.Rect(60, 18, 196, 54, C(0.75f, 0.08f, 0.06f));
            error.Rect(64, 22, 192, 50, C(0.12f, 0.02f, 0.02f));
            Renglones(error, rnd, 72, 186, 44, 26, 3, 8, C(1f, 0.6f, 0.55f), false);
            Guardar(error, "T_Pantalla_Error", TextureWrapMode.Clamp, false, 0f);

            // Consola con código verde
            var codigo = new Lienzo(256, 160, C(0.01f, 0.02f, 0.015f));
            Renglones(codigo, rnd, 8, 248, 150, 4, 3, 7, C(0.25f, 1f, 0.45f), true);
            codigo.Rect(8, 6, 16, 12, C(0.4f, 1f, 0.6f));
            Guardar(codigo, "T_Pantalla_Codigo", TextureWrapMode.Clamp, false, 0f);

            // Imagen de cámara de seguridad: pasillo en perspectiva, ruido y REC
            var camara = new Lienzo(256, 160, C(0.14f, 0.16f, 0.15f));
            Color trazo = C(0.42f, 0.48f, 0.45f);
            Linea(camara, 0, 0, 100, 70, trazo, 2); Linea(camara, 256, 0, 156, 70, trazo, 2);
            Linea(camara, 0, 160, 100, 100, trazo, 2); Linea(camara, 256, 160, 156, 100, trazo, 2);
            camara.Rect(100, 70, 156, 100, C(0.08f, 0.09f, 0.09f));
            for (int k = 0; k < 4; k++) Linea(camara, 20 + k * 24, 12 + k * 17, 20 + k * 24, 148 - k * 13, C(0.2f, 0.23f, 0.22f), 3);
            camara.Disco(16, 146, 5f, C(1f, 0.1f, 0.08f));
            camara.Rect(26, 142, 48, 150, C(0.85f, 0.85f, 0.85f));
            camara.Rect(200, 142, 246, 150, C(0.85f, 0.85f, 0.85f));
            camara.Ruido(0.25f, 103);
            Guardar(camara, "T_Pantalla_Camara", TextureWrapMode.Clamp, false, 0f);

            // Estática
            var estatica = new Lienzo(128, 80, C(0.4f, 0.42f, 0.42f));
            estatica.Ruido(0.9f, 104);
            for (int y = 0; y < 80; y += 4) estatica.Rect(0, y, 128, y + 1, C(0.15f, 0.16f, 0.16f));
            Guardar(estatica, "T_Pantalla_Estatica", TextureWrapMode.Clamp, false, 0f);

            // Pizarra con un diagrama y garabatos de marcador
            var pizarra = new Lienzo(512, 320, C(0.9f, 0.91f, 0.9f));
            Color azul = C(0.1f, 0.2f, 0.65f), rojo = C(0.75f, 0.1f, 0.08f), negro = C(0.12f, 0.12f, 0.14f);
            void Caja2(int x0, int y0, int x1, int y1, Color c) { Linea(pizarra, x0, y0, x1, y0, c, 3); Linea(pizarra, x1, y0, x1, y1, c, 3); Linea(pizarra, x1, y1, x0, y1, c, 3); Linea(pizarra, x0, y1, x0, y0, c, 3); }
            Caja2(40, 200, 150, 260, azul); Caja2(200, 200, 310, 260, azul); Caja2(360, 200, 470, 260, rojo);
            Linea(pizarra, 150, 230, 200, 230, azul, 3); Linea(pizarra, 310, 230, 360, 230, rojo, 3);
            Linea(pizarra, 350, 240, 360, 230, rojo, 3); Linea(pizarra, 350, 220, 360, 230, rojo, 3);
            Renglones(pizarra, new System.Random(105), 52, 140, 245, 215, 4, 12, azul, false);
            Renglones(pizarra, new System.Random(106), 212, 300, 245, 215, 4, 12, azul, false);
            Renglones(pizarra, new System.Random(107), 372, 460, 245, 215, 4, 12, rojo, false);
            Renglones(pizarra, new System.Random(108), 40, 330, 160, 40, 5, 22, negro, false);
            Linea(pizarra, 380, 150, 470, 60, rojo, 5); Linea(pizarra, 380, 60, 470, 150, rojo, 5);   // gran X roja
            var r2 = new System.Random(109);
            for (int k = 0; k < 4; k++) { float x = r2.Next(30, 480), y = r2.Next(20, 300); Linea(pizarra, x, y, x + r2.Next(-40, 40), y + r2.Next(-30, 30), C(0.6f, 0.62f, 0.62f), 6); }
            Guardar(pizarra, "T_Pizarra", TextureWrapMode.Clamp, false, 0f);

            // Teclado
            var teclado = new Lienzo(256, 80, C(0.05f, 0.05f, 0.06f));
            for (int fila = 0; fila < 5; fila++)
                for (int x = 6 + (fila % 2) * 6; x < 250; x += 16)
                    teclado.Rect(x, 6 + fila * 15, Mathf.Min(x + 13, 250), 18 + fila * 15, C(0.17f, 0.17f, 0.19f));
            Guardar(teclado, "T_Teclado", TextureWrapMode.Clamp, false, 0f);

            // Vidrio roto: grietas radiales desde el punto del golpe
            var vidrio = new Lienzo(256, 384, C(0.02f, 0.025f, 0.03f));
            var r3 = new System.Random(110);
            Vector2 golpe = new Vector2(150, 230);
            for (int k = 0; k < 14; k++)
            {
                float ang = k * Mathf.PI * 2f / 14f + (float)r3.NextDouble() * 0.3f;
                Vector2 p = golpe;
                for (int s = 0; s < 5; s++)
                {
                    ang += ((float)r3.NextDouble() - 0.5f) * 0.6f;
                    var q = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r3.Next(15, 45);
                    Linea(vidrio, p.x, p.y, q.x, q.y, C(0.55f, 0.6f, 0.62f), 1.5f);
                    p = q;
                }
            }
            for (int r = 18; r < 70; r += 17)
                for (int k = 0; k < 14; k++)
                {
                    float a0 = k * Mathf.PI * 2f / 14f, a1 = (k + 1) * Mathf.PI * 2f / 14f;
                    Linea(vidrio, golpe.x + Mathf.Cos(a0) * r, golpe.y + Mathf.Sin(a0) * r, golpe.x + Mathf.Cos(a1) * r, golpe.y + Mathf.Sin(a1) * r, C(0.4f, 0.45f, 0.48f), 1.2f);
                }
            Guardar(vidrio, "T_Vidrio_Roto", TextureWrapMode.Clamp, false, 0f);

            // Texturas con transparencia: manchas, anillo de señal y partícula
            TexturaAlfa("T_Mancha", 256, (x, y) =>
            {
                float n = Mathf.PerlinNoise(x * 0.025f + 3f, y * 0.025f + 7f) * 0.6f + Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.4f;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(128, 128)) / 128f;
                return Mathf.Clamp01((n - d * 0.75f) * 3f);
            });
            TexturaAlfa("T_Anillo", 256, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(128, 128)) / 128f;
                return Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) * 14f) + Mathf.Clamp01(1f - Mathf.Abs(d - 0.6f) * 30f) * 0.4f;
            });
            TexturaAlfa("T_Particula", 64, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32)) / 32f;
                return Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
            });
        }

        static void TexturaAlfa(string nombre, int tam, System.Func<int, int, float> alfa)
        {
            var px = new Color[tam * tam];
            for (int y = 0; y < tam; y++)
                for (int x = 0; x < tam; x++) px[y * tam + x] = new Color(1f, 1f, 1f, alfa(x, y));
            string ruta = $"{RutaTex}/{nombre}.png";
            EscribirPng(px, tam, tam, ruta);
            AssetDatabase.ImportAsset(ruta);
            var ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
            ti.textureType = TextureImporterType.Default;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
        }

        // =====================================================================
        //  MATERIALES
        // =====================================================================
        static void MaterialesUtileria()
        {
            Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{RutaTex}/{n}.png");
            Lit("M_Utl_Laminado", null, null, C(0.6f, 0.58f, 0.54f), 0f, 0.35f);
            Lit("M_Utl_MetalGris", T("T_Metal_Cepillado"), null, C(0.42f, 0.44f, 0.47f), 0.3f, 0.45f);
            Lit("M_Utl_MetalOscuro", null, null, C(0.1f, 0.11f, 0.12f), 0.3f, 0.4f);
            Lit("M_Utl_Tela", null, null, C(0.07f, 0.09f, 0.15f), 0f, 0.12f);
            Lit("M_Utl_PlasticoGris", null, null, C(0.3f, 0.31f, 0.33f), 0f, 0.5f);
            Lit("M_Utl_Blanco", null, null, C(0.72f, 0.73f, 0.72f), 0f, 0.45f);
            Lit("M_Utl_Rojo", null, null, C(0.5f, 0.06f, 0.05f), 0.2f, 0.5f);
            Lit("M_Utl_Azul", T("T_Metal_Cepillado"), null, C(0.16f, 0.24f, 0.36f), 0.25f, 0.45f);
            Lit("M_Utl_Naranja", null, null, C(0.85f, 0.32f, 0.04f), 0f, 0.4f);
            Lit("M_Utl_Carton", T("T_Carton"), null, Color.white, 0f, 0.15f);
            Lit("M_Utl_Maceta", null, null, C(0.4f, 0.19f, 0.11f), 0f, 0.3f);
            Lit("M_Utl_Seco", null, null, C(0.22f, 0.16f, 0.09f), 0f, 0.2f);
            Lit("M_Utl_Agua", null, null, C(0.2f, 0.45f, 0.75f), 0f, 0.85f);
            Lit("M_Utl_Bolsa", null, null, C(0.025f, 0.025f, 0.03f), 0f, 0.75f);
            Lit("M_Utl_Panel_Techo", null, null, C(0.5f, 0.5f, 0.47f), 0f, 0.2f);
            Lit("M_Utl_Pizarra", T("T_Pizarra"), null, Color.white, 0f, 0.6f);
            Lit("M_Utl_Teclado", T("T_Teclado"), null, Color.white, 0f, 0.4f);
            Lit("M_Utl_Pantalla_Apagada", null, null, C(0.015f, 0.015f, 0.02f), 0.1f, 0.92f);
            Emision(Lit("M_Utl_Vidrio_Roto", T("T_Vidrio_Roto"), null, Color.white, 0.1f, 0.9f), T("T_Vidrio_Roto"), 0.25f);
            foreach (var n in new[] { "Error", "Codigo", "Camara", "Estatica" })
                Emision(Lit("M_Utl_Pantalla_" + n, T("T_Pantalla_" + n), null, C(0.1f, 0.1f, 0.1f), 0f, 0.85f), T("T_Pantalla_" + n), 1.25f);
            Led("M_Utl_Luz_Fria", false, new Color(0.75f, 0.92f, 1f) * 2.6f, 1f, false, 0f);
            Led("M_Utl_LED_Rojo", false, new Color(1f, 0.08f, 0.05f) * 5f, 1f, true, 0f);
            Led("M_Utl_LED_Verde", false, new Color(0.1f, 1f, 0.3f) * 4f, 1f, true, 0f);

            Transparente(Lit("M_Utl_Vidrio", null, null, new Color(0.35f, 0.45f, 0.5f, 0.22f), 0.1f, 0.95f));
            Transparente(Lit("M_Utl_Mancha", T("T_Mancha"), null, new Color(0.04f, 0.035f, 0.03f, 0.75f), 0f, 0.2f));
            Transparente(Lit("M_Utl_Charco", T("T_Mancha"), null, new Color(0.02f, 0.03f, 0.035f, 0.85f), 0f, 0.97f));

            Particulas("M_Particula_Aditiva", T("T_Particula"), Color.white);
            Particulas("M_Anillo_Senal", T("T_Anillo"), new Color(0.2f, 0.9f, 1f, 0.9f));
        }

        static void Transparente(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(m);
        }

        /// <summary>Material aditivo para partículas y el anillo del señuelo (URP Particles/Unlit).</summary>
        static Material Particulas(string nombre, Texture2D tex, Color color)
        {
            var m = ObtenerMaterial(nombre, Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);   // aditivo
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        // =====================================================================
        //  HERRAMIENTAS DE MODELADO
        // =====================================================================
        static Transform Visual(GameObject raiz)
        {
            var v = new GameObject("Visual").transform;
            v.SetParent(raiz.transform, false);
            return v;
        }

        static GameObject Caja(string n, Transform p, Vector3 pos, Vector3 tam, string mat, Vector3? rot = null) => Pieza(PrimitiveType.Cube, n, p, pos, tam, mat, rot);
        /// <summary>Cilindro de radio r y alto h (el cilindro de Unity mide 2 de alto con escala 1).</summary>
        static GameObject Cil(string n, Transform p, Vector3 pos, float r, float h, string mat, Vector3? rot = null) => Pieza(PrimitiveType.Cylinder, n, p, pos, new Vector3(r * 2f, h / 2f, r * 2f), mat, rot);
        static GameObject Plano(string n, Transform p, Vector3 pos, Vector2 tam, string mat, Vector3? rot = null) => Pieza(PrimitiveType.Quad, n, p, pos, new Vector3(tam.x, tam.y, 1f), mat, rot);
        static GameObject Esfera(string n, Transform p, Vector3 pos, Vector3 tam, string mat) => Pieza(PrimitiveType.Sphere, n, p, pos, tam, mat);

        static Transform Pivote(string n, Transform p, Vector3 pos, Vector3 rot)
        {
            var t = new GameObject(n).transform;
            t.SetParent(p, false);
            t.localPosition = pos; t.localRotation = Quaternion.Euler(rot);
            return t;
        }

        static TextMeshPro Rotulo(string n, Transform p, Vector3 pos, Vector2 tam, string texto, float tamFuente, Color color, Vector3? rot = null)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(rot ?? Vector3.zero);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = tam;
            tmp.fontSize = tamFuente; tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color; tmp.text = texto;
            tmp.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return tmp;
        }

        /// <summary>
        /// Combina todas las piezas que cuelgan de "origen" en una malla por material, como hijos de
        /// "destino". Las mallas se guardan como assets en Modelos/Utileria.
        /// </summary>
        static void Combinar(Transform origen, Transform destino, string clave, bool sombras)
        {
            var grupos = new Dictionary<Material, List<CombineInstance>>();
            Matrix4x4 aLocal = destino.worldToLocalMatrix;
            foreach (var mf in origen.GetComponentsInChildren<MeshFilter>())
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mf.sharedMesh == null) continue;
                if (!grupos.TryGetValue(mr.sharedMaterial, out var lista)) grupos[mr.sharedMaterial] = lista = new List<CombineInstance>();
                lista.Add(new CombineInstance { mesh = mf.sharedMesh, transform = aLocal * mf.transform.localToWorldMatrix });
            }
            Object.DestroyImmediate(origen.gameObject);
            int i = 0;
            foreach (var kv in grupos)
            {
                string ruta = $"{RutaMeshUtl}/{clave}_{i++}.asset";
                var m = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
                if (m == null) { m = new Mesh(); AssetDatabase.CreateAsset(m, ruta); }
                m.Clear();
                m.name = System.IO.Path.GetFileNameWithoutExtension(ruta);
                m.indexFormat = IndexFormat.UInt32;
                m.CombineMeshes(kv.Value.ToArray(), true, true);
                m.RecalculateBounds();
                EditorUtility.SetDirty(m);
                var go = new GameObject("Malla_" + kv.Key.name.Replace("M_", ""));
                go.transform.SetParent(destino, false);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = kv.Key;
                bool transparente = kv.Key.renderQueue >= (int)RenderQueue.Transparent;
                r.shadowCastingMode = sombras && !transparente ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
        }

        /// <summary>
        /// Cierra un objeto de utilería: combina su "Visual", le pone colisión (caja que lo envuelve,
        /// o la caja dada) y lo guarda como prefab estático.
        /// </summary>
        static void Terminar(GameObject raiz, bool colision = true, bool sombras = true, Bounds? caja = null, bool estatico = true)
        {
            var visual = raiz.transform.Find("Visual");
            if (visual != null) Combinar(visual, raiz.transform, raiz.name, sombras);
            if (colision)
            {
                Bounds b;
                if (caja.HasValue) b = caja.Value;
                else
                {
                    b = new Bounds();
                    bool primero = true;
                    foreach (var mf in raiz.GetComponentsInChildren<MeshFilter>())
                    {
                        if (mf.GetComponentInParent<TextMeshPro>() != null) continue;
                        var mb = mf.sharedMesh.bounds;
                        var centro = raiz.transform.InverseTransformPoint(mf.transform.TransformPoint(mb.center));
                        var lb = new Bounds(centro, Vector3.Scale(mb.size, mf.transform.lossyScale));
                        if (primero) { b = lb; primero = false; } else b.Encapsulate(lb);
                    }
                }
                var bc = raiz.AddComponent<BoxCollider>();
                bc.center = b.center; bc.size = b.size;
            }
            // Estático (se agrupa en lotes al compilar), salvo textos, efectos y las piezas "Dinamico"
            // que el juego oculta o mueve
            if (estatico)
                foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    bool dinamico = t.GetComponentInParent<TextMeshPro>() != null || t.GetComponentInParent<ParticleSystem>() != null;
                    for (var p = t; p != null && !dinamico; p = p.parent) dinamico = p.name == "Dinamico";
                    if (!dinamico) MarcarEstatico(t.gameObject);
                }
            Guardar(raiz, CarpetaUtl);
        }

        // =====================================================================
        //  OFICINA
        // =====================================================================
        /// <summary>Escritorio con computador. estado: 0 = en uso, 1 = monitor apagado y caído, 2 = sin monitor y lleno de cajas.</summary>
        static void PrefabEscritorio(string nombre, string pantalla, int estado)
        {
            var raiz = new GameObject(nombre);
            var v = Visual(raiz);
            ArmarEscritorio(v, Vector3.zero, 0f, pantalla, estado, nombre.GetHashCode());
            Terminar(raiz);
        }

        static void ArmarEscritorio(Transform v, Vector3 o, float giro, string pantalla, int estado, int semilla)
        {
            var e = Pivote("Escritorio", v, o, new Vector3(0f, giro, 0f));
            var rnd = new System.Random(semilla);
            Caja("Tablero", e, new Vector3(0f, 0.74f, -0.38f), new Vector3(1.4f, 0.04f, 0.7f), "M_Utl_Laminado");
            Caja("Lado_I", e, new Vector3(-0.67f, 0.36f, -0.38f), new Vector3(0.04f, 0.72f, 0.66f), "M_Utl_MetalGris");
            Caja("Lado_D", e, new Vector3(0.67f, 0.36f, -0.38f), new Vector3(0.04f, 0.72f, 0.66f), "M_Utl_MetalGris");
            Caja("Faldon", e, new Vector3(0f, 0.46f, -0.08f), new Vector3(1.3f, 0.48f, 0.02f), "M_Utl_MetalGris");
            Caja("Torre", e, new Vector3(0.42f, 0.23f, -0.36f), new Vector3(0.2f, 0.44f, 0.46f), "M_Plastico_Negro");
            Caja("Torre_Luz", e, new Vector3(0.42f, 0.4f, -0.592f), new Vector3(0.012f, 0.012f, 0.004f), estado == 0 ? "M_Utl_LED_Verde" : "M_Plastico_Negro");
            // Papeles y taza
            for (int i = 0; i < 3; i++)
                Plano("Papel", e, new Vector3(-0.45f + (float)rnd.NextDouble() * 0.25f, 0.762f + i * 0.001f, -0.5f + (float)rnd.NextDouble() * 0.2f), new Vector2(0.21f, 0.297f), "M_Papel", new Vector3(90f, rnd.Next(-40, 40), 0f));
            Cil("Taza", e, new Vector3(0.52f, 0.81f, -0.45f), 0.04f, 0.1f, "M_Utl_Blanco");

            if (estado == 0 || estado == 1)
            {
                Caja("Teclado", e, new Vector3(0f, 0.768f, -0.55f), new Vector3(0.44f, 0.018f, 0.14f), "M_Utl_Teclado", new Vector3(0f, estado == 1 ? 14f : 0f, 0f));
                Caja("Raton", e, new Vector3(0.3f, 0.768f, -0.56f), new Vector3(0.06f, 0.02f, 0.1f), "M_Plastico_Negro");
                var mon = estado == 0
                    ? Pivote("Monitor", e, new Vector3(0f, 0.76f, -0.24f), new Vector3(0f, 0f, 0f))
                    : Pivote("Monitor", e, new Vector3(-0.1f, 0.76f, -0.3f), new Vector3(-82f, 25f, 0f));   // caído sobre la mesa
                Caja("Base", mon, new Vector3(0f, 0.008f, 0f), new Vector3(0.22f, 0.016f, 0.16f), "M_Plastico_Negro");
                Caja("Cuello", mon, new Vector3(0f, 0.12f, 0.02f), new Vector3(0.04f, 0.22f, 0.03f), "M_Plastico_Negro");
                Caja("Carcasa", mon, new Vector3(0f, 0.26f, 0f), new Vector3(0.56f, 0.34f, 0.035f), "M_Plastico_Negro");
                Plano("Pantalla", mon, new Vector3(0f, 0.26f, -0.0185f), new Vector2(0.52f, 0.3f), pantalla ?? "M_Utl_Pantalla_Apagada");
            }
            else
            {
                // Mudanza abandonada: cajas sobre el escritorio
                Caja("Caja_A", e, new Vector3(-0.25f, 0.92f, -0.35f), new Vector3(0.45f, 0.32f, 0.36f), "M_Utl_Carton", new Vector3(0f, 8f, 0f));
                Caja("Caja_B", e, new Vector3(0.3f, 0.86f, -0.4f), new Vector3(0.34f, 0.2f, 0.3f), "M_Utl_Carton", new Vector3(0f, -12f, 0f));
                Caja("Monitor_Viejo", e, new Vector3(-0.2f, 1.15f, -0.35f), new Vector3(0.4f, 0.14f, 0.3f), "M_Plastico_Negro", new Vector3(0f, 10f, 0f));
            }
        }

        static void ArmarSilla(Transform v, Vector3 o, float giro, Vector3 inclinacion)
        {
            var s = Pivote("Silla", v, o, new Vector3(inclinacion.x, giro, inclinacion.z));
            Caja("Asiento", s, new Vector3(0f, 0.47f, 0f), new Vector3(0.48f, 0.07f, 0.46f), "M_Utl_Tela");
            Caja("Respaldo", s, new Vector3(0f, 0.82f, -0.23f), new Vector3(0.46f, 0.55f, 0.06f), "M_Utl_Tela", new Vector3(-8f, 0f, 0f));
            Caja("Brazo_I", s, new Vector3(-0.26f, 0.62f, 0f), new Vector3(0.04f, 0.04f, 0.3f), "M_Utl_MetalOscuro");
            Caja("Brazo_D", s, new Vector3(0.26f, 0.62f, 0f), new Vector3(0.04f, 0.04f, 0.3f), "M_Utl_MetalOscuro");
            Cil("Columna", s, new Vector3(0f, 0.26f, 0f), 0.025f, 0.36f, "M_Utl_MetalOscuro");
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f;
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Caja("Pata_" + i, s, dir * 0.15f + Vector3.up * 0.07f, new Vector3(0.04f, 0.03f, 0.3f), "M_Utl_MetalOscuro", new Vector3(0f, a, 0f));
                Caja("Rueda_" + i, s, dir * 0.29f + Vector3.up * 0.03f, new Vector3(0.04f, 0.05f, 0.05f), "M_Plastico_Negro");
            }
        }

        static void PrefabSillaOficina()
        {
            var raiz = new GameObject("Silla_Oficina");
            ArmarSilla(Visual(raiz), Vector3.zero, 0f, Vector3.zero);
            Terminar(raiz);
        }

        /// <summary>Isla de dos escritorios enfrentados con sus sillas (para el centro de una oficina).</summary>
        static void PrefabIslaEscritorios()
        {
            var raiz = new GameObject("Isla_Escritorios");
            var v = Visual(raiz);
            ArmarEscritorio(v, new Vector3(0f, 0f, 0.38f + 0.02f), 0f, "M_Utl_Pantalla_Codigo", 0, 11);
            ArmarEscritorio(v, new Vector3(0f, 0f, -0.38f - 0.02f), 180f, "M_Utl_Pantalla_Apagada", 1, 12);
            Caja("Divisor", v, new Vector3(0f, 1.0f, 0f), new Vector3(1.42f, 0.5f, 0.04f), "M_Utl_Azul");
            ArmarSilla(v, new Vector3(0.1f, 0f, -0.75f), 15f, Vector3.zero);
            ArmarSilla(v, new Vector3(-0.3f, 0f, 1.1f), 160f, new Vector3(0f, 0f, 90f));   // volcada
            Terminar(raiz, true, true, new Bounds(new Vector3(0f, 0.5f, 0f), new Vector3(1.45f, 1f, 1.5f)));
        }

        static void PrefabArchivador()
        {
            var raiz = new GameObject("Archivador");
            var v = Visual(raiz);
            Caja("Cuerpo", v, new Vector3(0f, 0.66f, -0.31f), new Vector3(0.47f, 1.32f, 0.6f), "M_Utl_MetalGris");
            for (int i = 0; i < 4; i++)
            {
                float y = 0.17f + i * 0.32f;
                float z = i == 1 ? -0.86f : -0.615f;   // el segundo cajón quedó abierto
                Caja("Frente_" + i, v, new Vector3(0f, y, z), new Vector3(0.43f, 0.29f, 0.02f), "M_Utl_Azul");
                Caja("Manija_" + i, v, new Vector3(0f, y + 0.06f, z - 0.02f), new Vector3(0.13f, 0.025f, 0.02f), "M_Disco_Metal");
                Caja("Etiqueta_" + i, v, new Vector3(0f, y - 0.05f, z - 0.011f), new Vector3(0.08f, 0.04f, 0.002f), "M_Papel");
                if (i == 1)
                {
                    Caja("Cajon_I", v, new Vector3(-0.2f, y, -0.73f), new Vector3(0.02f, 0.24f, 0.26f), "M_Utl_Azul");
                    Caja("Cajon_D", v, new Vector3(0.2f, y, -0.73f), new Vector3(0.02f, 0.24f, 0.26f), "M_Utl_Azul");
                    for (int k = 0; k < 6; k++)
                        Caja("Carpeta_" + k, v, new Vector3(0f, y + 0.06f, -0.65f - k * 0.035f), new Vector3(0.36f, 0.22f, 0.008f), k % 2 == 0 ? "M_Papel" : "M_Utl_Naranja", new Vector3(k * 4f - 10f, 0f, 0f));
                }
            }
            Terminar(raiz, true, true, new Bounds(new Vector3(0f, 0.66f, -0.31f), new Vector3(0.47f, 1.32f, 0.62f)));
        }

        static void PrefabCasillero(string nombre, bool abierto)
        {
            var raiz = new GameObject(nombre);
            var v = Visual(raiz);
            Caja("Cuerpo", v, new Vector3(0f, 0.95f, -0.25f), new Vector3(0.82f, 1.9f, 0.5f), "M_Utl_Azul");
            Caja("Interior", v, new Vector3(0f, 0.97f, -0.47f), new Vector3(0.78f, 1.82f, 0.04f), "M_Utl_MetalOscuro");
            for (int k = 0; k < 2; k++)
            {
                float x = k == 0 ? -0.2f : 0.2f;
                bool estaAbierta = abierto && k == 1;
                Transform puerta = estaAbierta
                    ? Pivote("Bisagra", v, new Vector3(0.4f, 0f, -0.505f), new Vector3(0f, -105f, 0f))
                    : Pivote("Bisagra", v, new Vector3(x, 0f, -0.505f), Vector3.zero);
                float px = estaAbierta ? -0.19f : 0f;
                Caja("Puerta", puerta, new Vector3(px, 0.97f, 0f), new Vector3(0.38f, 1.82f, 0.02f), "M_Utl_Azul");
                for (int r = 0; r < 4; r++)
                    Caja("Rejilla", puerta, new Vector3(px, 1.65f - r * 0.05f, -0.011f), new Vector3(0.22f, 0.012f, 0.004f), "M_Utl_MetalOscuro");
                Caja("Manija", puerta, new Vector3(px + (k == 0 ? 0.14f : -0.14f), 1.0f, -0.02f), new Vector3(0.025f, 0.12f, 0.025f), "M_Disco_Metal");
            }
            if (abierto)
            {
                Caja("Repisa", v, new Vector3(0.2f, 1.55f, -0.27f), new Vector3(0.36f, 0.02f, 0.44f), "M_Utl_MetalGris");
                Caja("Caja_Personal", v, new Vector3(0.2f, 1.65f, -0.3f), new Vector3(0.28f, 0.18f, 0.3f), "M_Utl_Carton");
                Caja("Chaqueta", v, new Vector3(0.2f, 1.1f, -0.3f), new Vector3(0.32f, 0.6f, 0.12f), "M_Utl_Tela", new Vector3(0f, 0f, 4f));
                Cil("Lata", v, new Vector3(0.12f, 0.12f, -0.3f), 0.033f, 0.12f, "M_Lata_Energetica");
            }
            Terminar(raiz, true, true, new Bounds(new Vector3(0f, 0.95f, -0.25f), new Vector3(0.82f, 1.9f, 0.5f)));
        }

        /// <summary>Consola con los monitores de las cámaras de seguridad (Oficina de Seguridad).</summary>
        static void PrefabMonitoresSeguridad()
        {
            var raiz = new GameObject("Monitores_Seguridad");
            var v = Visual(raiz);
            Caja("Consola", v, new Vector3(0f, 0.4f, -0.4f), new Vector3(1.9f, 0.8f, 0.75f), "M_Utl_MetalOscuro");
            Caja("Tablero", v, new Vector3(0f, 0.82f, -0.42f), new Vector3(1.95f, 0.04f, 0.8f), "M_Utl_Laminado");
            Caja("Bastidor", v, new Vector3(0f, 1.45f, -0.08f), new Vector3(1.9f, 1.2f, 0.1f), "M_Utl_MetalOscuro");
            string[] pantallas = { "M_Utl_Pantalla_Camara", "M_Utl_Pantalla_Estatica", "M_Utl_Pantalla_Camara", "M_Utl_Pantalla_Camara", "M_Utl_Pantalla_Apagada", "M_Utl_Pantalla_Estatica" };
            for (int f = 0; f < 2; f++)
                for (int c = 0; c < 3; c++)
                {
                    var pos = new Vector3(-0.62f + c * 0.62f, 1.18f + f * 0.52f, -0.14f);
                    Caja("Marco", v, pos, new Vector3(0.58f, 0.46f, 0.04f), "M_Plastico_Negro");
                    Plano("Pantalla", v, pos + new Vector3(0f, 0f, -0.021f), new Vector2(0.53f, 0.41f), pantallas[f * 3 + c]);
                }
            Caja("Teclado", v, new Vector3(-0.3f, 0.85f, -0.55f), new Vector3(0.44f, 0.018f, 0.14f), "M_Utl_Teclado");
            Caja("Joystick", v, new Vector3(0.25f, 0.88f, -0.55f), new Vector3(0.12f, 0.06f, 0.12f), "M_Plastico_Negro");
            Cil("Palanca", v, new Vector3(0.25f, 0.95f, -0.55f), 0.012f, 0.1f, "M_Utl_Rojo");
            for (int i = 0; i < 6; i++) Caja("Boton", v, new Vector3(0.45f + (i % 3) * 0.06f, 0.845f, -0.5f - (i / 3) * 0.06f), new Vector3(0.03f, 0.01f, 0.03f), i == 2 ? "M_Utl_LED_Rojo" : "M_Utl_LED_Verde");
            Cil("Taza", v, new Vector3(0.75f, 0.89f, -0.5f), 0.04f, 0.1f, "M_Utl_Blanco");
            var raizT = raiz.transform;
            Rotulo("Rotulo", raizT, new Vector3(0f, 2.12f, -0.14f), new Vector2(1.8f, 0.14f), "CCTV  ·  SECTOR B", 0.9f, new Color(0.5f, 0.9f, 1f));
            Terminar(raiz, true, true, new Bounds(new Vector3(0f, 0.6f, -0.42f), new Vector3(1.95f, 1.2f, 0.8f)));
        }

        static void PrefabPizarra()
        {
            var raiz = new GameObject("Pizarra");
            var v = Visual(raiz);
            Caja("Marco", v, new Vector3(0f, 1.55f, -0.015f), new Vector3(1.62f, 1.02f, 0.03f), "M_Utl_PlasticoGris");
            Plano("Tablero", v, new Vector3(0f, 1.55f, -0.031f), new Vector2(1.54f, 0.94f), "M_Utl_Pizarra");
            Caja("Bandeja", v, new Vector3(0f, 1.03f, -0.06f), new Vector3(0.9f, 0.025f, 0.07f), "M_Utl_PlasticoGris");
            Caja("Marcador", v, new Vector3(0.2f, 1.05f, -0.06f), new Vector3(0.12f, 0.018f, 0.018f), "M_Utl_Rojo");
            Terminar(raiz, false, false);
        }

        static void PrefabCartel(string nombre, Color fondo, Color acento, string titulo, string cuerpo)
        {
            var raiz = new GameObject(nombre);
            var v = Visual(raiz);
            var mFondo = Lit("M_Utl_" + nombre, null, null, fondo, 0f, 0.3f);
            var mAcento = Led("M_Utl_" + nombre + "_Acento", false, acento * 1.2f, 1f, false, 0f);
            Plano("Fondo", v, new Vector3(0f, 1.65f, -0.012f), new Vector2(0.72f, 0.98f), mFondo.name);
            Caja("Franja", v, new Vector3(0f, 2.06f, -0.014f), new Vector3(0.72f, 0.06f, 0.004f), mAcento.name);
            Cil("Logo_Anillo", v, new Vector3(0f, 1.82f, -0.016f), 0.1f, 0.004f, mAcento.name, new Vector3(90f, 0f, 0f));
            Cil("Logo_Centro", v, new Vector3(0f, 1.82f, -0.02f), 0.07f, 0.004f, mFondo.name, new Vector3(90f, 0f, 0f));
            Rotulo("Titulo", raiz.transform, new Vector3(0f, 1.62f, -0.022f), new Vector2(0.66f, 0.14f), titulo, 0.75f, acento).fontStyle = FontStyles.Bold;
            Rotulo("Texto", raiz.transform, new Vector3(0f, 1.38f, -0.022f), new Vector2(0.64f, 0.34f), cuerpo, 0.42f, new Color(0.85f, 0.88f, 0.9f));
            Terminar(raiz, false, false);
        }

        static void PrefabPlanta()
        {
            var raiz = new GameObject("Planta_Muerta");
            var v = Visual(raiz);
            var rnd = new System.Random(201);
            Cil("Maceta", v, new Vector3(0f, 0.2f, -0.3f), 0.17f, 0.4f, "M_Utl_Maceta");
            Cil("Tierra", v, new Vector3(0f, 0.395f, -0.3f), 0.155f, 0.01f, "M_Utl_Seco");
            for (int i = 0; i < 7; i++)
            {
                float a = i * 51f, inc = 10f + (float)rnd.NextDouble() * 25f;
                float largo = 0.4f + (float)rnd.NextDouble() * 0.5f;
                var dir = Quaternion.Euler(inc, a, 0f) * Vector3.up;
                Caja("Tallo_" + i, v, new Vector3(0f, 0.4f, -0.3f) + dir * largo * 0.5f, new Vector3(0.018f, largo, 0.018f), "M_Utl_Seco", new Vector3(inc, a, 0f));
            }
            for (int i = 0; i < 6; i++)
                Caja("Hoja_" + i, v, new Vector3((float)rnd.NextDouble() * 0.8f - 0.4f, 0.004f, -0.1f - (float)rnd.NextDouble() * 0.5f), new Vector3(0.07f, 0.004f, 0.04f), "M_Utl_Seco", new Vector3(0f, rnd.Next(0, 180), 0f));
            Terminar(raiz, true, true, new Bounds(new Vector3(0f, 0.3f, -0.3f), new Vector3(0.36f, 0.6f, 0.36f)));
        }

        static void PrefabDispensadorAgua()
        {
            var raiz = new GameObject("Dispensador_Agua");
            var v = Visual(raiz);
            Caja("Cuerpo", v, new Vector3(0f, 0.5f, -0.22f), new Vector3(0.32f, 1.0f, 0.32f), "M_Utl_Blanco");
            Cil("Botellon", v, new Vector3(0f, 1.2f, -0.22f), 0.13f, 0.4f, "M_Utl_Agua");
            Cil("Cuello", v, new Vector3(0f, 1.0f, -0.22f), 0.04f, 0.04f, "M_Utl_Agua");
            Caja("Grifo_F", v, new Vector3(-0.06f, 0.78f, -0.39f), new Vector3(0.04f, 0.05f, 0.04f), "M_Utl_Azul");
            Caja("Grifo_C", v, new Vector3(0.06f, 0.78f, -0.39f), new Vector3(0.04f, 0.05f, 0.04f), "M_Utl_Rojo");
            Caja("Bandeja", v, new Vector3(0f, 0.6f, -0.4f), new Vector3(0.24f, 0.02f, 0.08f), "M_Utl_PlasticoGris");
            Terminar(raiz);
        }
    }
}
