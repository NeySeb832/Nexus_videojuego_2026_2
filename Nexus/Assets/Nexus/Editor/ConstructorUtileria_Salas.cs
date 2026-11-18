using TMPro;
using UnityEditor;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Utilería de cafetería (máquinas expendedoras, mesas, mesón, nevera), almacén y zona técnica.
    /// Ver ConstructorUtileria.cs para las convenciones.
    /// </summary>
    public static partial class ConstructorFase1
    {
        static Texture2D Tex(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{RutaTex}/{n}.png");

        // =====================================================================
        //  CAFETERÍA
        // =====================================================================
        /// <summary>
        /// Máquina expendedora (1 x 0,8 x 1,9 m). Vidrio con los productos en repisas, panel de
        /// control con teclado y pantallita, bandeja de salida. Las filas de productos quedan aparte
        /// ("Dinamico") para ocultarlas a medida que se venden. La versión rota no funciona:
        /// vidrio estrellado, sin luz y productos tirados en el suelo.
        /// </summary>
        static void PrefabMaquina(string nombre, TipoColeccionable producto, int existencias, Color colorMarca, string marca, bool rota)
        {
            var raiz = new GameObject(nombre);
            var v = Visual(raiz);
            var mCuerpo = Lit("M_Utl_" + nombre + "_Cuerpo", Tex("T_Metal_Cepillado"), null, colorMarca, 0.25f, 0.5f).name;
            var mAcento = Led("M_Utl_" + nombre + "_Acento", false, (rota ? new Color(0.3f, 0.05f, 0.03f) : Color.Lerp(colorMarca, Color.white, 0.5f)) * 2.5f, 1f, false, 0f).name;

            Caja("Respaldo", v, new Vector3(0f, 0.95f, -0.03f), new Vector3(1f, 1.9f, 0.06f), mCuerpo);
            Caja("Lado_I", v, new Vector3(-0.48f, 0.95f, -0.4f), new Vector3(0.04f, 1.9f, 0.8f), mCuerpo);
            Caja("Lado_D", v, new Vector3(0.48f, 0.95f, -0.4f), new Vector3(0.04f, 1.9f, 0.8f), mCuerpo);
            Caja("Techo", v, new Vector3(0f, 1.83f, -0.4f), new Vector3(1f, 0.14f, 0.8f), mCuerpo);
            Caja("Base", v, new Vector3(0f, 0.22f, -0.4f), new Vector3(1f, 0.44f, 0.8f), mCuerpo);
            Caja("Panel_Control", v, new Vector3(0.33f, 1.1f, -0.4f), new Vector3(0.3f, 1.32f, 0.8f), mCuerpo);
            Caja("Interior", v, new Vector3(-0.14f, 1.1f, -0.2f), new Vector3(0.64f, 1.3f, 0.02f), "M_Utl_MetalOscuro");
            if (!rota) Caja("Tubo_Luz", v, new Vector3(-0.14f, 1.72f, -0.5f), new Vector3(0.6f, 0.025f, 0.04f), "M_Utl_Luz_Fria");
            for (int i = 0; i < 4; i++)
                Caja("Repisa_" + i, v, new Vector3(-0.14f, 0.6f + i * 0.31f, -0.48f), new Vector3(0.62f, 0.015f, 0.5f), "M_Utl_MetalGris");
            if (rota) Plano("Vidrio", v, new Vector3(-0.14f, 1.1f, -0.79f), new Vector2(0.64f, 1.3f), "M_Utl_Vidrio_Roto");
            Caja("Bandeja", v, new Vector3(-0.14f, 0.28f, -0.802f), new Vector3(0.5f, 0.14f, 0.01f), "M_Plastico_Negro");
            Caja("Franja", v, new Vector3(0f, 1.77f, -0.805f), new Vector3(0.96f, 0.02f, 0.01f), mAcento);
            Plano("Pantallita", v, new Vector3(0.33f, 1.48f, -0.801f), new Vector2(0.22f, 0.09f), "M_Utl_Pantalla_Apagada");
            for (int f = 0; f < 4; f++)
                for (int c = 0; c < 3; c++)
                    Caja("Tecla", v, new Vector3(0.27f + c * 0.06f, 1.3f - f * 0.06f, -0.805f), new Vector3(0.045f, 0.045f, 0.012f), "M_Utl_MetalGris");
            Caja("Ranura_Monedas", v, new Vector3(0.33f, 0.98f, -0.805f), new Vector3(0.03f, 0.09f, 0.012f), "M_Plastico_Negro");

            // Productos: cada fila es un objeto aparte que se oculta al venderse
            var dinamico = new GameObject("Dinamico").transform;
            dinamico.SetParent(raiz.transform, false);
            var filas = new GameObject[4];
            var rnd = new System.Random(nombre.Length * 31);
            for (int i = 0; i < 4; i++)
            {
                var fila = new GameObject("Fila_" + i).transform;
                fila.SetParent(dinamico, false);
                var tmp = new GameObject("tmp").transform;
                tmp.SetParent(fila, false);
                float y = 0.6f + i * 0.31f + 0.0075f;
                for (int c = 0; c < 4; c++)
                {
                    if (rota && rnd.NextDouble() < 0.6) continue;
                    float x = -0.38f + c * 0.16f;
                    if (producto == TipoColeccionable.BarritaProteina)
                        Caja("Barrita", tmp, new Vector3(x, y + 0.025f, -0.6f), new Vector3(0.13f, 0.05f, 0.045f), "M_Barrita_Proteina", new Vector3(0f, 0f, 0f));
                    else
                        Cil("Lata", tmp, new Vector3(x, y + 0.06f, -0.6f), 0.033f, 0.12f, "M_Lata_Energetica");
                    Cil("Espiral", tmp, new Vector3(x, y + 0.05f, -0.45f), 0.045f, 0.22f, "M_Disco_Metal", new Vector3(90f, 0f, 0f));
                }
                if (tmp.childCount == 0) { Object.DestroyImmediate(tmp.gameObject); }
                else Combinar(tmp, fila, $"{nombre}_Fila{i}", false);
                filas[i] = fila.gameObject;
            }
            if (rota)
            {
                // Productos que se cayeron cuando alguien rompió el vidrio
                var suelo = new GameObject("tmp").transform;
                suelo.SetParent(raiz.transform, false);
                var piso = Visual(raiz);   // se combina con el resto
                for (int k = 0; k < 5; k++)
                    Cil("Lata_Suelo", piso, new Vector3(-0.4f + (float)rnd.NextDouble() * 0.9f, 0.033f, -0.95f - (float)rnd.NextDouble() * 0.5f), 0.033f, 0.12f, "M_Lata_Energetica", new Vector3(0f, rnd.Next(0, 180), 90f));
                for (int k = 0; k < 6; k++)
                    Caja("Vidrio_Suelo", piso, new Vector3(-0.3f + (float)rnd.NextDouble() * 0.6f, 0.003f, -0.9f - (float)rnd.NextDouble() * 0.4f), new Vector3(0.08f, 0.004f, 0.06f), "M_Utl_Vidrio_Roto", new Vector3(0f, rnd.Next(0, 180), 0f));
                Object.DestroyImmediate(suelo.gameObject);
                Combinar(piso, raiz.transform, nombre + "_Suelo", false);
            }

            // Vidrio transparente (se dibuja aparte, al final, para no tapar los productos)
            if (!rota)
            {
                var vidrio = Plano("Vidrio", raiz.transform, new Vector3(-0.14f, 1.1f, -0.79f), new Vector2(0.64f, 1.3f), "M_Utl_Vidrio");
                vidrio.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var luz = new GameObject("Luz_Interior").AddComponent<Light>();
                luz.transform.SetParent(raiz.transform, false);
                luz.transform.localPosition = new Vector3(-0.14f, 1.3f, -0.95f);
                luz.type = LightType.Point; luz.range = 2.4f; luz.intensity = 1.1f;
                luz.color = Color.Lerp(colorMarca, Color.white, 0.6f);
                luz.shadows = LightShadows.None;
            }

            var tMarca = Rotulo("Marca", raiz.transform, new Vector3(0f, 1.84f, -0.807f), new Vector2(0.94f, 0.12f), marca, marca.Length > 12 ? 0.62f : 0.95f, rota ? new Color(0.6f, 0.25f, 0.2f) : Color.white);
            tMarca.fontStyle = FontStyles.Bold;
            var letrero = Rotulo("Letrero", raiz.transform, new Vector3(0.33f, 1.48f, -0.803f), new Vector2(0.21f, 0.08f), rota ? "ERROR" : "DISPONIBLE", 0.32f, rota ? new Color(1f, 0.3f, 0.25f) : new Color(0.4f, 1f, 0.55f));

            // Colisión sólida aparte y, si funciona, zona de interacción enfrente
            var solido = new GameObject("Solido");
            solido.transform.SetParent(raiz.transform, false);
            var bc = solido.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.95f, -0.4f); bc.size = new Vector3(1f, 1.9f, 0.8f);
            if (!rota)
            {
                var zona = raiz.AddComponent<BoxCollider>();
                zona.isTrigger = true; zona.center = new Vector3(0f, 1f, -1.2f); zona.size = new Vector3(1.6f, 2f, 1.4f);
                var m = raiz.AddComponent<MaquinaExpendedora>();
                m.Configurar(producto, existencias);
                var so = new SerializedObject(m);
                var arr = so.FindProperty("productosVisibles");
                arr.arraySize = filas.Length;
                for (int i = 0; i < filas.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = filas[i];
                so.FindProperty("letrero").objectReferenceValue = letrero;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Terminar(raiz, false);
        }

        static void PrefabMesaCafeteria()
        {
            var raiz = new GameObject("Mesa_Cafeteria");
            var v = Visual(raiz);
            Cil("Tablero", v, new Vector3(0f, 0.74f, 0f), 0.45f, 0.03f, "M_Utl_Laminado");
            Cil("Columna", v, new Vector3(0f, 0.37f, 0f), 0.04f, 0.72f, "M_Utl_MetalOscuro");
            Cil("Pie", v, new Vector3(0f, 0.015f, 0f), 0.25f, 0.03f, "M_Utl_MetalOscuro");
            // Restos de la última comida
            Caja("Bandeja", v, new Vector3(0.1f, 0.765f, 0.05f), new Vector3(0.4f, 0.015f, 0.3f), "M_Utl_PlasticoGris", new Vector3(0f, 20f, 0f));
            Cil("Vaso", v, new Vector3(-0.2f, 0.8f, -0.1f), 0.035f, 0.11f, "M_Utl_Blanco");
            Caja("Envoltura", v, new Vector3(0.05f, 0.782f, 0.1f), new Vector3(0.13f, 0.02f, 0.05f), "M_Barrita_Proteina", new Vector3(0f, 35f, 0f));
            Cil("Lata", v, new Vector3(0.25f, 0.79f, -0.2f), 0.033f, 0.12f, "M_Lata_Energetica", new Vector3(0f, 0f, 90f));
            // Sillas plásticas alrededor (una volcada)
            ArmarSillaPlastica(v, new Vector3(0f, 0f, -0.65f), 0f, Vector3.zero);
            ArmarSillaPlastica(v, new Vector3(0.62f, 0f, 0.2f), -110f, Vector3.zero);
            ArmarSillaPlastica(v, new Vector3(-0.75f, 0f, 0.35f), 120f, new Vector3(0f, 0f, 85f));
            Terminar(raiz, true, true, new Bounds(new Vector3(0f, 0.45f, 0f), new Vector3(1.5f, 0.9f, 1.5f)));
        }

        static void ArmarSillaPlastica(Transform v, Vector3 o, float giro, Vector3 inclinacion)
        {
            var s = Pivote("Silla_Plastica", v, o, new Vector3(inclinacion.x, giro, inclinacion.z));
            Caja("Asiento", s, new Vector3(0f, 0.45f, 0f), new Vector3(0.42f, 0.03f, 0.42f), "M_Utl_Naranja");
            Caja("Respaldo", s, new Vector3(0f, 0.68f, -0.2f), new Vector3(0.4f, 0.4f, 0.03f), "M_Utl_Naranja", new Vector3(-6f, 0f, 0f));
            foreach (var (x, z) in new[] { (-0.18f, -0.18f), (0.18f, -0.18f), (-0.18f, 0.18f), (0.18f, 0.18f) })
                Cil("Pata", s, new Vector3(x, 0.22f, z), 0.012f, 0.44f, "M_Utl_MetalOscuro");
        }

        static void PrefabSillaPlastica()
        {
            var raiz = new GameObject("Silla_Plastica");
            ArmarSillaPlastica(Visual(raiz), Vector3.zero, 0f, Vector3.zero);
            Terminar(raiz);
        }

        /// <summary>Mesón de la cafetería con cafetera y microondas.</summary>
        static void PrefabMeson()
        {
            var raiz = new GameObject("Meson_Cafeteria");
            var v = Visual(raiz);
            Caja("Mueble", v, new Vector3(0f, 0.44f, -0.3f), new Vector3(1.6f, 0.88f, 0.58f), "M_Utl_Blanco");
            Caja("Cubierta", v, new Vector3(0f, 0.9f, -0.31f), new Vector3(1.64f, 0.04f, 0.62f), "M_Utl_MetalGris");
            for (int i = 0; i < 3; i++)
            {
                Caja("Puerta_" + i, v, new Vector3(-0.53f + i * 0.53f, 0.44f, -0.595f), new Vector3(0.5f, 0.8f, 0.012f), "M_Utl_PlasticoGris");
                Caja("Tirador_" + i, v, new Vector3(-0.53f + i * 0.53f + 0.18f, 0.7f, -0.605f), new Vector3(0.02f, 0.12f, 0.015f), "M_Disco_Metal");
            }
            Caja("Cafetera", v, new Vector3(-0.5f, 1.1f, -0.22f), new Vector3(0.28f, 0.36f, 0.28f), "M_Plastico_Negro");
            Cil("Jarra", v, new Vector3(-0.5f, 1.0f, -0.36f), 0.065f, 0.16f, "M_Utl_Vidrio_Roto");
            Caja("Microondas", v, new Vector3(0.35f, 1.07f, -0.24f), new Vector3(0.5f, 0.3f, 0.36f), "M_Utl_Blanco");
            Plano("Ventana_Micro", v, new Vector3(0.3f, 1.07f, -0.421f), new Vector2(0.32f, 0.2f), "M_Utl_Pantalla_Apagada");
            Caja("Panel_Micro", v, new Vector3(0.53f, 1.07f, -0.422f), new Vector3(0.08f, 0.22f, 0.004f), "M_Plastico_Negro");
            for (int i = 0; i < 3; i++) Cil("Vaso_" + i, v, new Vector3(-0.15f + i * 0.09f, 0.97f, -0.4f), 0.035f, 0.1f, "M_Utl_Blanco");
            Terminar(raiz);
        }

        static void PrefabNevera()
        {
            var raiz = new GameObject("Nevera");
            var v = Visual(raiz);
            Caja("Cuerpo", v, new Vector3(0f, 0.9f, -0.35f), new Vector3(0.72f, 1.8f, 0.66f), "M_Utl_Blanco");
            Caja("Junta", v, new Vector3(0f, 1.25f, -0.684f), new Vector3(0.7f, 0.012f, 0.01f), "M_Plastico_Negro");
            Caja("Manija_A", v, new Vector3(-0.28f, 1.5f, -0.71f), new Vector3(0.03f, 0.3f, 0.04f), "M_Disco_Metal");
            Caja("Manija_B", v, new Vector3(-0.28f, 0.95f, -0.71f), new Vector3(0.03f, 0.3f, 0.04f), "M_Disco_Metal");
            Plano("Nota", v, new Vector3(0.12f, 1.45f, -0.684f), new Vector2(0.15f, 0.2f), "M_Papel", new Vector3(0f, 0f, 6f));
            Plano("Nota_2", v, new Vector3(0.1f, 0.9f, -0.684f), new Vector2(0.15f, 0.2f), "M_Papel", new Vector3(0f, 0f, -9f));
            Terminar(raiz);
        }

        // =====================================================================
        //  ALMACÉN Y ZONA TÉCNICA
        // =====================================================================
        static void PrefabEstanteria()
        {
            var raiz = new GameObject("Estanteria");
            var v = Visual(raiz);
            var rnd = new System.Random(301);
            foreach (var (x, z) in new[] { (-0.73f, -0.04f), (0.73f, -0.04f), (-0.73f, -0.46f), (0.73f, -0.46f) })
                Caja("Poste", v, new Vector3(x, 1f, z), new Vector3(0.04f, 2f, 0.04f), "M_Utl_MetalGris");
            for (int i = 0; i < 4; i++)
            {
                float y = 0.12f + i * 0.58f;
                Caja("Nivel_" + i, v, new Vector3(0f, y, -0.25f), new Vector3(1.5f, 0.03f, 0.48f), "M_Utl_MetalGris");
                float x = -0.65f;
                while (x < 0.6f)
                {
                    float w = 0.25f + (float)rnd.NextDouble() * 0.3f;
                    if (rnd.NextDouble() < 0.75 && i < 3)
                    {
                        float h = 0.18f + (float)rnd.NextDouble() * 0.28f;
                        Caja("Caja", v, new Vector3(x + w / 2f, y + 0.015f + h / 2f, -0.25f), new Vector3(w * 0.92f, h, 0.36f + (float)rnd.NextDouble() * 0.08f), rnd.NextDouble() < 0.15 ? "M_Rack_Cuerpo" : "M_Utl_Carton", new Vector3(0f, rnd.Next(-6, 6), 0f));
                    }
                    x += w;
                }
            }
            Terminar(raiz, true, true, new Bounds(new Vector3(0f, 1f, -0.25f), new Vector3(1.5f, 2f, 0.5f)));
        }

        static void PrefabPilaCajas(int variante)
        {
            var raiz = new GameObject("Pila_Cajas_" + variante);
            var v = Visual(raiz);
            var rnd = new System.Random(400 + variante);
            int n = 2 + variante;
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                float w = 0.45f + (float)rnd.NextDouble() * 0.25f, h = 0.3f + (float)rnd.NextDouble() * 0.2f;
                bool alLado = i == 1 && variante > 0;
                Vector3 pos = alLado ? new Vector3(0.55f, h / 2f, -0.35f) : new Vector3(0f, y + h / 2f, -0.35f);
                Caja("Caja_" + i, v, pos, new Vector3(w, h, w * 0.9f), "M_Utl_Carton", new Vector3(0f, rnd.Next(-15, 15), 0f));
                if (!alLado) y += h;
            }
            if (variante == 2)
                Caja("Caja_Abierta", v, new Vector3(-0.5f, 0.12f, -0.6f), new Vector3(0.4f, 0.24f, 0.35f), "M_Utl_Carton", new Vector3(0f, 35f, 12f));
            Terminar(raiz);
        }

        static void PrefabBobinaCable()
        {
            var raiz = new GameObject("Bobina_Cable");
            var v = Visual(raiz);
            Cil("Disco_A", v, new Vector3(-0.22f, 0.36f, -0.4f), 0.36f, 0.04f, "M_Madera", new Vector3(0f, 0f, 90f));
            Cil("Disco_B", v, new Vector3(0.22f, 0.36f, -0.4f), 0.36f, 0.04f, "M_Madera", new Vector3(0f, 0f, 90f));
            Cil("Cable", v, new Vector3(0f, 0.36f, -0.4f), 0.26f, 0.4f, "M_Utl_Naranja", new Vector3(0f, 0f, 90f));
            // Extremo del cable tirado por el suelo
            for (int i = 0; i < 6; i++)
                Cil("Tramo_" + i, v, new Vector3(0.1f + i * 0.05f, 0.02f, -0.75f - i * 0.18f), 0.018f, 0.2f, "M_Utl_Naranja", new Vector3(90f, i * 9f, 0f));
            Terminar(raiz, true, true, new Bounds(new Vector3(0f, 0.36f, -0.4f), new Vector3(0.5f, 0.72f, 0.72f)));
        }

        /// <summary>Gabinete eléctrico de pared. La versión abierta tiene cables sueltos que echan chispas.</summary>
        static void PrefabGabinete(string nombre, bool abierto)
        {
            var raiz = new GameObject(nombre);
            var v = Visual(raiz);
            Caja("Caja", v, new Vector3(0f, 1.3f, -0.15f), new Vector3(0.72f, 0.92f, 0.3f), "M_Utl_MetalGris");
            Cil("Ducto", v, new Vector3(0.2f, 2.2f, -0.12f), 0.035f, 0.9f, "M_Utl_MetalGris");
            Cil("Ducto_2", v, new Vector3(-0.2f, 2.2f, -0.12f), 0.025f, 0.9f, "M_Utl_MetalGris");
            if (!abierto)
            {
                Caja("Puerta", v, new Vector3(0f, 1.3f, -0.305f), new Vector3(0.68f, 0.88f, 0.012f), "M_Utl_MetalOscuro");
                Caja("Peligro", v, new Vector3(0f, 1.5f, -0.313f), new Vector3(0.16f, 0.16f, 0.004f), "M_Peligro", new Vector3(0f, 0f, 45f));
                Caja("Cerradura", v, new Vector3(0.27f, 1.3f, -0.315f), new Vector3(0.03f, 0.08f, 0.01f), "M_Disco_Metal");
            }
            else
            {
                var bis = Pivote("Bisagra", v, new Vector3(-0.35f, 0f, -0.31f), new Vector3(0f, 115f, 0f));
                Caja("Puerta", bis, new Vector3(0.34f, 1.3f, 0f), new Vector3(0.68f, 0.88f, 0.012f), "M_Utl_MetalOscuro");
                Caja("Fondo", v, new Vector3(0f, 1.3f, -0.29f), new Vector3(0.66f, 0.84f, 0.01f), "M_Utl_MetalOscuro");
                string[] colores = { "M_Utl_Rojo", "M_Utl_Naranja", "M_Plastico_Negro", "M_Utl_Azul" };
                for (int i = 0; i < 4; i++)
                    Cil("Cable_" + i, v, new Vector3(-0.15f + i * 0.1f, 0.85f - i * 0.04f, -0.33f), 0.012f, 0.45f + i * 0.08f, colores[i], new Vector3(8f - i * 5f, 0f, i * 6f - 9f));
                for (int i = 0; i < 6; i++) Caja("Breaker_" + i, v, new Vector3(-0.2f + (i % 3) * 0.2f, 1.5f - (i / 3) * 0.25f, -0.3f), new Vector3(0.1f, 0.16f, 0.03f), "M_Plastico_Negro");
            }
            Caja("Luz_A", v, new Vector3(-0.2f, 1.82f, -0.3f), new Vector3(0.04f, 0.04f, 0.01f), "M_Utl_LED_Rojo");
            Caja("Luz_B", v, new Vector3(-0.12f, 1.82f, -0.3f), new Vector3(0.04f, 0.04f, 0.01f), abierto ? "M_Plastico_Negro" : "M_Utl_LED_Verde");
            if (abierto)
            {
                var fx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPre + "/Efectos/FX_Chispazo.prefab");
                if (fx != null)
                {
                    var c = (GameObject)PrefabUtility.InstantiatePrefab(fx, raiz.transform);
                    c.transform.localPosition = new Vector3(0f, 0.65f, -0.35f);
                }
            }
            Terminar(raiz, true, true, new Bounds(new Vector3(0f, 1.3f, -0.15f), new Vector3(0.72f, 0.92f, 0.3f)));
        }

        static void PrefabCarrito()
        {
            var raiz = new GameObject("Carrito_Herramientas");
            var v = Visual(raiz);
            Caja("Cuerpo", v, new Vector3(0f, 0.52f, -0.32f), new Vector3(0.72f, 0.8f, 0.46f), "M_Utl_Rojo");
            for (int i = 0; i < 4; i++)
            {
                Caja("Cajon_" + i, v, new Vector3(0f, 0.25f + i * 0.18f, -0.555f), new Vector3(0.66f, 0.15f, 0.01f), "M_Utl_Rojo");
                Caja("Tirador_" + i, v, new Vector3(0f, 0.29f + i * 0.18f, -0.565f), new Vector3(0.3f, 0.02f, 0.02f), "M_Disco_Metal");
            }
            Caja("Bandeja", v, new Vector3(0f, 0.93f, -0.32f), new Vector3(0.74f, 0.03f, 0.48f), "M_Utl_MetalOscuro");
            Caja("Llave", v, new Vector3(-0.15f, 0.955f, -0.3f), new Vector3(0.25f, 0.015f, 0.04f), "M_Disco_Metal", new Vector3(0f, 25f, 0f));
            Caja("Martillo", v, new Vector3(0.15f, 0.96f, -0.35f), new Vector3(0.05f, 0.03f, 0.3f), "M_Utl_MetalOscuro", new Vector3(0f, -15f, 0f));
            foreach (var (x, z) in new[] { (-0.3f, -0.12f), (0.3f, -0.12f), (-0.3f, -0.52f), (0.3f, -0.52f) })
                Cil("Rueda", v, new Vector3(x, 0.05f, z), 0.05f, 0.04f, "M_Plastico_Negro", new Vector3(0f, 0f, 90f));
            Terminar(raiz);
        }

        static void PrefabBancoTrabajo()
        {
            var raiz = new GameObject("Banco_Trabajo");
            var v = Visual(raiz);
            Caja("Tablero", v, new Vector3(0f, 0.9f, -0.4f), new Vector3(1.8f, 0.06f, 0.75f), "M_Madera");
            foreach (var x in new[] { -0.85f, 0.85f })
                foreach (var z in new[] { -0.08f, -0.72f })
                    Caja("Pata", v, new Vector3(x, 0.44f, z), new Vector3(0.06f, 0.87f, 0.06f), "M_Utl_MetalOscuro");
            Caja("Repisa", v, new Vector3(0f, 0.2f, -0.4f), new Vector3(1.7f, 0.03f, 0.65f), "M_Utl_MetalOscuro");
            Caja("Tablero_Herramientas", v, new Vector3(0f, 1.6f, -0.02f), new Vector3(1.6f, 0.9f, 0.02f), "M_Utl_MetalOscuro");
            var rnd = new System.Random(501);
            for (int i = 0; i < 9; i++)
                Caja("Herramienta", v, new Vector3(-0.65f + i * 0.16f, 1.6f + (float)rnd.NextDouble() * 0.2f, -0.04f), new Vector3(0.04f, 0.2f + (float)rnd.NextDouble() * 0.15f, 0.02f), i % 3 == 0 ? "M_Utl_Rojo" : "M_Disco_Metal", new Vector3(0f, 0f, rnd.Next(-20, 20)));
            // Servidor desarmado, tornillo de banco y lámpara
            Caja("Servidor", v, new Vector3(-0.3f, 0.98f, -0.45f), new Vector3(0.48f, 0.09f, 0.6f), "M_Rack_Cuerpo", new Vector3(0f, 12f, 0f));
            Caja("Tapa_Servidor", v, new Vector3(0.25f, 0.94f, -0.3f), new Vector3(0.46f, 0.01f, 0.55f), "M_Rack_Cuerpo", new Vector3(0f, -20f, 0f));
            Caja("Tornillo_Banco", v, new Vector3(0.7f, 1.0f, -0.6f), new Vector3(0.18f, 0.14f, 0.14f), "M_Utl_Azul");
            Cil("Lampara_Base", v, new Vector3(-0.75f, 0.94f, -0.2f), 0.07f, 0.02f, "M_Utl_MetalOscuro");
            Caja("Lampara_Brazo", v, new Vector3(-0.72f, 1.15f, -0.25f), new Vector3(0.02f, 0.42f, 0.02f), "M_Utl_MetalOscuro", new Vector3(-15f, 0f, 0f));
            Caja("Lampara_Cabeza", v, new Vector3(-0.7f, 1.34f, -0.36f), new Vector3(0.12f, 0.06f, 0.16f), "M_Utl_MetalOscuro", new Vector3(30f, 0f, 0f));
            Cil("Lata", v, new Vector3(0.45f, 0.99f, -0.2f), 0.033f, 0.12f, "M_Lata_Energetica");
            Terminar(raiz, true, true, new Bounds(new Vector3(0f, 0.5f, -0.4f), new Vector3(1.8f, 1f, 0.78f)));
        }
    }
}
