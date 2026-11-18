using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Segunda parte del modelado: objetos de inventario (fusible, USB, Speedmax, Chocorramo,
    /// refrigerante), objetos interactivos (nota, panel de fusibles, terminal de respaldo,
    /// ascensor) y la fila de racks combinada para construir laberintos.
    /// Menú: Nexus → Fase 1 → 4. Objetos del inventario y del nivel.
    /// </summary>
    public static partial class ConstructorFase1
    {
        [MenuItem("Nexus/Fase 1/4. Objetos del inventario y del nivel")]
        public static void ConstruirObjetos()
        {
            CrearCarpeta(RutaTex); CrearCarpeta(RutaMat); CrearCarpeta(RutaMesh);
            CrearCarpeta(RutaPre + "/Coleccionables"); CrearCarpeta(RutaPre + "/Interactivos"); CrearCarpeta(RutaPre + "/Entorno");

            TexturasObjetos();
            MaterialesObjetos();

            PrefabItem("Fusible", TipoColeccionable.Fusible, "Fusible de repuesto", ArmarFusible);
            PrefabItem("Memoria_USB", TipoColeccionable.USB, "Memoria USB de respaldo", ArmarUSB);
            PrefabItem("Speedmax", TipoColeccionable.Speedmax, "Speedmax (recupera 1 corazón)", ArmarLata);
            PrefabItem("Chocorramo", TipoColeccionable.Chocorramo, "Chocorramo (recupera toda la salud)", ArmarPonque);
            PrefabItem("Cartucho_Refrigerante", TipoColeccionable.Refrigerante, "Cartucho de refrigerante", ArmarRefrigerante);
            PrefabNota();
            PrefabPanelFusibles();
            PrefabTerminalRespaldo();
            PrefabAscensor();
            PrefabFilaRacks();
            AssetDatabase.SaveAssets();
        }

        // ---------------- Texturas ----------------
        static void TexturasObjetos()
        {
            // Lata de bebida energética (diseño propio, sin logotipos reales)
            var lata = new Lienzo(256, 256, C(0.04f, 0.16f, 0.07f));
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                {
                    float d = Mathf.Abs(((x * 0.5f + y) % 96f) - 48f);
                    if (d < 10f) lata.Color[y * 256 + x] = C(0.55f, 1f, 0.12f);
                    if (y < 18 || y > 238) lata.Color[y * 256 + x] = C(0.72f, 0.74f, 0.76f);
                }
            lata.Ruido(0.04f, 71);
            Guardar(lata, "T_Lata_Energetica", TextureWrapMode.Repeat, false, 0f);

            // Envoltura del ponqué
            var ponque = new Lienzo(256, 128, C(0.95f, 0.45f, 0.05f));
            ponque.Rect(0, 48, 256, 80, C(0.36f, 0.17f, 0.06f));
            for (int x = 0; x < 256; x++)
            {
                int y = 64 + (int)(Mathf.Sin(x * 0.12f) * 7f);
                ponque.Rect(x, y - 1, x + 1, y + 2, C(0.98f, 0.92f, 0.8f));
            }
            for (int k = 0; k < 6; k++) ponque.Disco(20 + k * 44, 104, 6f, C(1f, 0.85f, 0.3f));
            ponque.Ruido(0.03f, 72);
            Guardar(ponque, "T_Envoltura_Ponque", TextureWrapMode.Clamp, false, 0f);

            // Hoja de papel con "texto"
            var papel = new Lienzo(128, 180, C(0.9f, 0.88f, 0.8f));
            papel.Rect(10, 160, 118, 163, C(0.75f, 0.15f, 0.12f));
            var rnd = new System.Random(73);
            for (int y = 148; y > 18; y -= 9)
            {
                int x = 12;
                while (x < 112)
                {
                    int w = rnd.Next(6, 22);
                    papel.Rect(x, y, Mathf.Min(x + w, 116), y + 2, C(0.3f, 0.3f, 0.32f));
                    x += w + rnd.Next(3, 6);
                }
            }
            papel.Ruido(0.03f, 74);
            Guardar(papel, "T_Papel", TextureWrapMode.Clamp, false, 0f);

            // Cartucho de refrigerante
            var refri = new Lienzo(256, 256, C(0.82f, 0.94f, 1f));
            refri.Rect(0, 96, 256, 160, C(0.08f, 0.45f, 0.9f));
            for (int k = 0; k < 8; k++) refri.Disco(16 + k * 32, 128, 7f, C(0.85f, 0.97f, 1f));
            refri.Ruido(0.05f, 75);
            Guardar(refri, "T_Refrigerante", TextureWrapMode.Repeat, false, 0f);
        }

        static void MaterialesObjetos()
        {
            Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{RutaTex}/{n}.png");
            Emision(Lit("M_Lata_Energetica", T("T_Lata_Energetica"), null, Color.white, 0.6f, 0.7f), T("T_Lata_Energetica"), 0.45f);
            Emision(Lit("M_Ponque", T("T_Envoltura_Ponque"), null, Color.white, 0f, 0.55f), T("T_Envoltura_Ponque"), 0.45f);
            Emision(Lit("M_Papel", T("T_Papel"), null, Color.white, 0f, 0.2f), T("T_Papel"), 0.3f);
            Emision(Lit("M_Refrigerante", T("T_Refrigerante"), null, Color.white, 0.3f, 0.8f), T("T_Refrigerante"), 0.5f);
            Lit("M_Plastico_Negro", null, null, C(0.06f, 0.06f, 0.07f), 0f, 0.6f);
            Lit("M_Madera", null, null, C(0.35f, 0.22f, 0.12f), 0f, 0.3f);
            Led("M_Neon_Ambar", false, new Color(1f, 0.5f, 0.05f) * 6f, 1f, false, 1f);
            Led("M_Neon_Verde", false, new Color(0.1f, 1f, 0.3f) * 5f, 1f, false, 0f);
            Led("M_Neon_Cian", false, new Color(0.1f, 0.9f, 1f) * 6f, 1f, false, 1f);
            Led("M_Pantalla_Respaldo", false, new Color(0.02f, 0.06f, 0.16f), 1f, false, 0f);
        }

        // ---------------- Objetos de inventario ----------------
        delegate void Armador(Transform visual);

        static void PrefabItem(string nombre, TipoColeccionable tipo, string visible, Armador armar)
        {
            var raiz = new GameObject(nombre);
            var col = raiz.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = Vector3.one * 0.9f;
            var c = raiz.AddComponent<Coleccionable>();
            Configurar(c, tipo, visible);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(raiz.transform, false);
            visual.AddComponent<FlotarGirar>();
            armar(visual.transform);
            foreach (var r in visual.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            Guardar(raiz, "Coleccionables");
        }

        static void ArmarFusible(Transform v)
        {
            v.localScale = Vector3.one * 1.6f;
            Pieza(PrimitiveType.Cylinder, "Cuerpo", v, Vector3.zero, new Vector3(0.065f, 0.07f, 0.065f), "M_Neon_Ambar");
            Pieza(PrimitiveType.Cylinder, "Tapa_A", v, new Vector3(0f, 0.08f, 0f), new Vector3(0.075f, 0.02f, 0.075f), "M_Disco_Metal");
            Pieza(PrimitiveType.Cylinder, "Tapa_B", v, new Vector3(0f, -0.08f, 0f), new Vector3(0.075f, 0.02f, 0.075f), "M_Disco_Metal");
            v.localRotation = Quaternion.Euler(0f, 0f, 70f);
        }

        static void ArmarUSB(Transform v)
        {
            v.localScale = Vector3.one * 2.2f;
            Pieza(PrimitiveType.Cube, "Cuerpo", v, Vector3.zero, new Vector3(0.05f, 0.02f, 0.11f), "M_Plastico_Negro");
            Pieza(PrimitiveType.Cube, "Conector", v, new Vector3(0f, 0f, 0.07f), new Vector3(0.034f, 0.012f, 0.035f), "M_Disco_Metal");
            Pieza(PrimitiveType.Cube, "Luz", v, new Vector3(0f, 0.0105f, -0.01f), new Vector3(0.03f, 0.002f, 0.06f), "M_Neon_Cian");
        }

        static void ArmarLata(Transform v)
        {
            v.localScale = Vector3.one * 2.2f;
            Pieza(PrimitiveType.Cylinder, "Lata", v, Vector3.zero, new Vector3(0.066f, 0.06f, 0.066f), "M_Lata_Energetica");
            Pieza(PrimitiveType.Cylinder, "Tapa", v, new Vector3(0f, 0.061f, 0f), new Vector3(0.058f, 0.003f, 0.058f), "M_Disco_Metal");
        }

        static void ArmarPonque(Transform v)
        {
            v.localScale = Vector3.one * 1.8f;
            Pieza(PrimitiveType.Cube, "Ponque", v, Vector3.zero, new Vector3(0.17f, 0.05f, 0.065f), "M_Ponque");
        }

        static void ArmarRefrigerante(Transform v)
        {
            v.localScale = Vector3.one * 1.6f;
            Pieza(PrimitiveType.Cylinder, "Cilindro", v, Vector3.zero, new Vector3(0.09f, 0.11f, 0.09f), "M_Refrigerante");
            Pieza(PrimitiveType.Cylinder, "Banda", v, new Vector3(0f, 0.04f, 0f), new Vector3(0.094f, 0.008f, 0.094f), "M_Neon_Cian");
            Pieza(PrimitiveType.Cylinder, "Valvula", v, new Vector3(0f, 0.125f, 0f), new Vector3(0.03f, 0.02f, 0.03f), "M_Disco_Metal");
        }

        // ---------------- Interactivos ----------------
        /// <summary>Mesa con una nota (la nota se lee con el botón de interactuar).</summary>
        static void PrefabNota()
        {
            var raiz = new GameObject("Nota_Mesa");
            var zona = raiz.AddComponent<BoxCollider>();
            zona.isTrigger = true; zona.size = new Vector3(2f, 2f, 2f); zona.center = new Vector3(0f, 1f, 0f);
            raiz.AddComponent<NotaLore>();
            var mesa = Pieza(PrimitiveType.Cube, "Tablero", raiz.transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.9f, 0.05f, 0.6f), "M_Rack_Cuerpo");
            var solido = new GameObject("Colision");
            solido.transform.SetParent(raiz.transform, false);
            var sc = solido.AddComponent<BoxCollider>(); sc.size = new Vector3(0.9f, 0.78f, 0.6f); sc.center = new Vector3(0f, 0.39f, 0f);
            foreach (var (x, z) in new[] { (-0.4f, -0.25f), (0.4f, -0.25f), (-0.4f, 0.25f), (0.4f, 0.25f) })
                Pieza(PrimitiveType.Cube, "Pata", raiz.transform, new Vector3(x, 0.375f, z), new Vector3(0.05f, 0.75f, 0.05f), "M_Rack_Cuerpo");
            Pieza(PrimitiveType.Cube, "Portapapeles", raiz.transform, new Vector3(0.05f, 0.78f, 0f), new Vector3(0.26f, 0.008f, 0.35f), "M_Madera", new Vector3(0f, 12f, 0f));
            Pieza(PrimitiveType.Quad, "Papel", raiz.transform, new Vector3(0.05f, 0.786f, -0.01f), new Vector3(0.21f, 0.297f, 1f), "M_Papel", new Vector3(90f, 12f, 0f));
            Guardar(raiz, "Interactivos");
        }

        /// <summary>Panel de fusibles de pared. Su frente mira a -Z (se apoya en la pared por +Z).</summary>
        static void PrefabPanelFusibles()
        {
            var raiz = new GameObject("Panel_Fusibles");
            var zona = raiz.AddComponent<BoxCollider>();
            zona.isTrigger = true; zona.size = new Vector3(1.8f, 2f, 1.8f); zona.center = new Vector3(0f, 1f, -0.9f);
            var panel = raiz.AddComponent<PanelFusibles>();
            Pieza(PrimitiveType.Cube, "Caja", raiz.transform, new Vector3(0f, 1.35f, -0.09f), new Vector3(0.8f, 1.0f, 0.18f), "M_Terminal");
            Pieza(PrimitiveType.Cube, "Tapa", raiz.transform, new Vector3(0f, 1.35f, -0.185f), new Vector3(0.72f, 0.92f, 0.012f), "M_Puerta");
            Pieza(PrimitiveType.Cube, "Franja_Peligro", raiz.transform, new Vector3(0f, 1.74f, -0.193f), new Vector3(0.72f, 0.08f, 0.006f), "M_Peligro");
            for (int i = 0; i < 3; i++)
                Pieza(PrimitiveType.Cylinder, "Fusible_" + i, raiz.transform, new Vector3(-0.2f + i * 0.2f, 1.3f, -0.21f), new Vector3(0.05f, 0.08f, 0.05f), i == 2 ? "M_Plastico_Negro" : "M_Neon_Ambar", new Vector3(90f, 0f, 0f));
            var luz = Pieza(PrimitiveType.Cube, "Luz_Estado", raiz.transform, new Vector3(0.28f, 1.05f, -0.2f), new Vector3(0.07f, 0.04f, 0.01f), "M_LED_Lector");
            var so = new SerializedObject(panel);
            so.FindProperty("luzEstado").objectReferenceValue = luz.GetComponent<MeshRenderer>();
            so.FindProperty("textoAccion").stringValue = "Revisar panel de fusibles";
            so.ApplyModifiedPropertiesWithoutUndo();
            Guardar(raiz, "Interactivos");
        }

        /// <summary>Terminal de respaldo (guardar partida con USB). Frente hacia -Z.</summary>
        static void PrefabTerminalRespaldo()
        {
            var raiz = new GameObject("Terminal_Respaldo");
            var zona = raiz.AddComponent<BoxCollider>();
            zona.isTrigger = true; zona.size = new Vector3(1.6f, 2f, 1.6f); zona.center = new Vector3(0f, 1f, -0.9f);
            var t = raiz.AddComponent<TerminalGuardado>();
            var so = new SerializedObject(t);
            so.FindProperty("textoAccion").stringValue = "Terminal de respaldo (guardar partida)";
            so.ApplyModifiedPropertiesWithoutUndo();
            var consola = Pieza(PrimitiveType.Cube, "Consola", raiz.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 1.1f, 0.45f), "M_Terminal");
            consola.AddComponent<BoxCollider>();
            Pieza(PrimitiveType.Cube, "Ranura_USB", raiz.transform, new Vector3(0.2f, 0.95f, -0.226f), new Vector3(0.08f, 0.025f, 0.005f), "M_Neon_Cian");
            var pivote = new GameObject("Monitor");
            pivote.transform.SetParent(raiz.transform, false);
            pivote.transform.localPosition = new Vector3(0f, 1.45f, 0.05f);
            pivote.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            Pieza(PrimitiveType.Cube, "Carcasa", pivote.transform, Vector3.zero, new Vector3(0.62f, 0.44f, 0.06f), "M_Terminal");
            Pieza(PrimitiveType.Quad, "Pantalla", pivote.transform, new Vector3(0f, 0f, -0.031f), new Vector3(0.56f, 0.38f, 1f), "M_Pantalla_Respaldo");
            var texto = new GameObject("Texto_Pantalla");
            texto.transform.SetParent(pivote.transform, false);
            texto.transform.localPosition = new Vector3(0f, 0f, -0.033f);
            var tmp = texto.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = new Vector2(0.52f, 0.34f);
            tmp.enableAutoSizing = true; tmp.fontSizeMin = 0.1f; tmp.fontSizeMax = 0.45f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.45f, 0.8f, 1f);
            tmp.text = "RESPALDO DE SISTEMA\n<size=70%>Inserte memoria USB</size>";
            Guardar(raiz, "Interactivos");
        }

        /// <summary>Ascensor (salida del nivel). Se apoya en la pared por +Z; frente hacia -Z.</summary>
        static void PrefabAscensor()
        {
            var raiz = new GameObject("Ascensor_Salida");
            var zona = raiz.AddComponent<BoxCollider>();
            zona.isTrigger = true; zona.size = new Vector3(2.8f, 2.5f, 2.2f); zona.center = new Vector3(0f, 1.25f, -1.2f);
            raiz.AddComponent<SalidaNivel>();
            Pieza(PrimitiveType.Cube, "Marco_Izq", raiz.transform, new Vector3(-1.25f, 1.45f, -0.05f), new Vector3(0.3f, 2.9f, 0.3f), "M_Peligro");
            Pieza(PrimitiveType.Cube, "Marco_Der", raiz.transform, new Vector3(1.25f, 1.45f, -0.05f), new Vector3(0.3f, 2.9f, 0.3f), "M_Peligro");
            Pieza(PrimitiveType.Cube, "Dintel", raiz.transform, new Vector3(0f, 2.75f, -0.05f), new Vector3(2.2f, 0.3f, 0.3f), "M_Rack_Cuerpo");
            var hojas = Pieza(PrimitiveType.Cube, "Puertas", raiz.transform, new Vector3(0f, 1.3f, 0.02f), new Vector3(2.2f, 2.6f, 0.08f), "M_Puerta");
            hojas.AddComponent<BoxCollider>();
            Pieza(PrimitiveType.Cube, "Junta", raiz.transform, new Vector3(0f, 1.3f, -0.025f), new Vector3(0.02f, 2.6f, 0.01f), "M_Plastico_Negro");
            Pieza(PrimitiveType.Cube, "Luz_Cartel", raiz.transform, new Vector3(0f, 2.62f, -0.21f), new Vector3(1.6f, 0.04f, 0.01f), "M_Neon_Verde");
            var cartel = new GameObject("Cartel");
            cartel.transform.SetParent(raiz.transform, false);
            cartel.transform.localPosition = new Vector3(0f, 2.76f, -0.21f);
            var tmp = cartel.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = new Vector2(2f, 0.25f);
            tmp.fontSize = 1.6f; tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.4f, 1f, 0.5f);
            tmp.text = "ASCENSOR";
            Pieza(PrimitiveType.Cube, "Boton", raiz.transform, new Vector3(1.6f, 1.2f, -0.05f), new Vector3(0.14f, 0.24f, 0.06f), "M_Terminal");
            Pieza(PrimitiveType.Cube, "Boton_Luz", raiz.transform, new Vector3(1.6f, 1.25f, -0.085f), new Vector3(0.06f, 0.06f, 0.01f), "M_Neon_Verde");
            Guardar(raiz, "Interactivos");
        }

        // ---------------- Fila de racks combinada ----------------
        /// <summary>
        /// Dos filas de 5 racks espalda con espalda, ocupando una celda de 3 x 3 m.
        /// Todo se combina en 3 meshes (cuerpos, frentes y LEDs) = 3 llamadas de dibujo por celda,
        /// en lugar de 40: imprescindible para los laberintos en Android y WebGL.
        /// </summary>
        static void PrefabFilaRacks()
        {
            var cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var cuerpos = new List<CombineInstance>();
            var frentes = new List<CombineInstance>();
            var v = new List<Vector3>(); var n = new List<Vector3>(); var c = new List<Color>(); var tri = new List<int>();
            var rnd = new System.Random(4321);
            Color[] paleta = { new Color(0.1f, 1f, 0.25f), new Color(0.15f, 0.45f, 1f), new Color(1f, 0.55f, 0.05f), new Color(1f, 0.08f, 0.05f) };

            foreach (int lado in new[] { -1, 1 })
                for (int i = 0; i < 5; i++)
                {
                    float x = -1.2f + i * 0.6f, z = lado * 0.75f;
                    var mCuerpo = Matrix4x4.TRS(new Vector3(x, RackAlto / 2f, z), Quaternion.identity, new Vector3(RackAncho, RackAlto, RackFondo));
                    cuerpos.Add(new CombineInstance { mesh = cubo, transform = mCuerpo });
                    // El frente mira hacia fuera de la fila
                    var rot = lado < 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
                    var mFrente = Matrix4x4.TRS(new Vector3(x, FrenteBase + FrenteAlto / 2f, z + lado * (RackFondo / 2f + 0.0015f)), rot, new Vector3(FrenteAncho, FrenteAlto, 1f));
                    frentes.Add(new CombineInstance { mesh = quad, transform = mFrente });

                    for (int b = 0; b < Cuchillas; b++)
                        foreach (int px in LedPixelX)
                        {
                            int py = CuchillaInicio + b * CuchillaPaso + CuchillaAlto - LedDesdeArriba;
                            float lx = -FrenteAncho / 2f + (px + 0.5f) / FrenteTexAncho * FrenteAncho;
                            if (lado > 0) lx = -lx;
                            float ly = FrenteBase + (py + 0.5f) / FrenteTexAlto * FrenteAlto;
                            double r = rnd.NextDouble();
                            Color col = r < 0.6 ? paleta[0] : r < 0.85 ? paleta[1] : r < 0.97 ? paleta[2] : paleta[3];
                            col.a = (float)rnd.NextDouble();
                            AgregarCaja(v, n, c, tri, new Vector3(x + lx, ly, z + lado * (RackFondo / 2f + 0.004f)), new Vector3(0.016f, 0.01f, 0.008f), col);
                        }
                }

            Mesh Combinar(string nombre, List<CombineInstance> partes)
            {
                string ruta = $"{RutaMesh}/{nombre}.asset";
                var m = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
                if (m == null) { m = new Mesh { name = nombre }; AssetDatabase.CreateAsset(m, ruta); }
                m.Clear();
                m.CombineMeshes(partes.ToArray(), true, true);
                m.RecalculateBounds();
                EditorUtility.SetDirty(m);
                return m;
            }
            var meshCuerpos = Combinar("Mesh_Fila_Cuerpos", cuerpos);
            var meshFrentes = Combinar("Mesh_Fila_Frentes", frentes);
            string rutaLeds = $"{RutaMesh}/Mesh_Fila_LEDs.asset";
            var meshLeds = AssetDatabase.LoadAssetAtPath<Mesh>(rutaLeds);
            if (meshLeds == null) { meshLeds = new Mesh { name = "Mesh_Fila_LEDs" }; AssetDatabase.CreateAsset(meshLeds, rutaLeds); }
            meshLeds.Clear();
            meshLeds.SetVertices(v); meshLeds.SetNormals(n); meshLeds.SetColors(c); meshLeds.SetTriangles(tri, 0);
            meshLeds.RecalculateBounds();
            EditorUtility.SetDirty(meshLeds);

            var raiz = new GameObject("Fila_Racks");
            var col2 = raiz.AddComponent<BoxCollider>();
            col2.size = new Vector3(3f, 2f, 3f); col2.center = new Vector3(0f, 1f, 0f);
            GameObject Parte(string nombre, Mesh mesh, string mat, bool sombras)
            {
                var go = new GameObject(nombre);
                go.transform.SetParent(raiz.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = M(mat);
                mr.shadowCastingMode = sombras ? ShadowCastingMode.On : ShadowCastingMode.Off;
                return go;
            }
            MarcarEstatico(Parte("Cuerpos", meshCuerpos, "M_Rack_Cuerpo", true));
            MarcarEstatico(Parte("Frentes", meshFrentes, "M_Rack_Frente", false));
            MarcarEstatico(Parte("LEDs", meshLeds, "M_LED_Rack", false), false);
            MarcarEstatico(raiz);
            Guardar(raiz, "Entorno");
        }
    }
}
