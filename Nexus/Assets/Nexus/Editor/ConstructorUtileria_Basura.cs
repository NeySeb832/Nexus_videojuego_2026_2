using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Basura y abandono (papeles, latas, bolsas, canecas, escombros, manchas, charcos, cables,
    /// conos) y efectos de partículas (chispas, polvo) + el señuelo de red de Neyder.
    /// La basura no tiene colisión (salvo canecas y conos) para no estorbar la navegación.
    /// </summary>
    public static partial class ConstructorFase1
    {
        static void ConstruirBasura()
        {
            for (int i = 0; i < 3; i++) PrefabPapeles(i);
            for (int i = 0; i < 2; i++) PrefabLatas(i);
            for (int i = 0; i < 2; i++) PrefabBolsas(i);
            PrefabCaneca("Caneca", false);
            PrefabCaneca("Caneca_Volcada", true);
            for (int i = 0; i < 2; i++) PrefabEscombros(i);
            PrefabMancha("Mancha", "M_Utl_Mancha", 1.6f);
            PrefabMancha("Charco", "M_Utl_Charco", 1.3f);
            PrefabConos();
            for (int i = 0; i < 2; i++) PrefabCables(i);
        }

        static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

        static void PrefabPapeles(int variante)
        {
            var raiz = new GameObject("Basura_Papeles_" + variante);
            var v = Visual(raiz);
            var rnd = new System.Random(600 + variante);
            int n = 5 + variante * 2;
            for (int i = 0; i < n; i++)
                Plano("Hoja", v, new Vector3(R(rnd, -0.8f, 0.8f), 0.004f + i * 0.0006f, R(rnd, -0.8f, 0.8f)), new Vector2(0.21f, 0.297f), "M_Papel", new Vector3(90f, rnd.Next(0, 360), 0f));
            for (int i = 0; i < 2 + variante; i++)
                Esfera("Bola", v, new Vector3(R(rnd, -0.7f, 0.7f), 0.035f, R(rnd, -0.7f, 0.7f)), new Vector3(0.08f, 0.07f, 0.075f), "M_Papel");
            if (variante == 2) Caja("Carpeta", v, new Vector3(0.3f, 0.006f, -0.2f), new Vector3(0.24f, 0.012f, 0.32f), "M_Utl_Naranja", new Vector3(0f, 40f, 0f));
            Terminar(raiz, false, false);
        }

        static void PrefabLatas(int variante)
        {
            var raiz = new GameObject("Basura_Latas_" + variante);
            var v = Visual(raiz);
            var rnd = new System.Random(700 + variante);
            for (int i = 0; i < 3 + variante; i++)
            {
                bool aplastada = rnd.NextDouble() < 0.4;
                Cil("Lata", v, new Vector3(R(rnd, -0.5f, 0.5f), aplastada ? 0.012f : 0.033f, R(rnd, -0.5f, 0.5f)), 0.033f, aplastada ? 0.03f : 0.12f, "M_Lata_Energetica", new Vector3(0f, rnd.Next(0, 360), aplastada ? 0f : 90f));
            }
            Caja("Envoltura", v, new Vector3(R(rnd, -0.4f, 0.4f), 0.01f, R(rnd, -0.4f, 0.4f)), new Vector3(0.13f, 0.02f, 0.05f), "M_Barrita_Proteina", new Vector3(0f, rnd.Next(0, 360), 0f));
            Terminar(raiz, false, false);
        }

        static void PrefabBolsas(int variante)
        {
            var raiz = new GameObject("Bolsa_Basura_" + variante);
            var v = Visual(raiz);
            Esfera("Bolsa", v, new Vector3(0f, 0.22f, -0.35f), new Vector3(0.55f, 0.45f, 0.5f), "M_Utl_Bolsa");
            Esfera("Nudo", v, new Vector3(0.02f, 0.46f, -0.35f), new Vector3(0.1f, 0.1f, 0.1f), "M_Utl_Bolsa");
            if (variante == 1)
            {
                Esfera("Bolsa_2", v, new Vector3(0.45f, 0.18f, -0.5f), new Vector3(0.45f, 0.36f, 0.42f), "M_Utl_Bolsa");
                Plano("Hoja", v, new Vector3(0.1f, 0.004f, -0.75f), new Vector2(0.21f, 0.297f), "M_Papel", new Vector3(90f, 30f, 0f));
            }
            Terminar(raiz, false, true);
        }

        static void PrefabCaneca(string nombre, bool volcada)
        {
            var raiz = new GameObject(nombre);
            var v = Visual(raiz);
            var t = volcada ? Pivote("Caneca", v, new Vector3(0f, 0.2f, -0.35f), new Vector3(90f, 0f, 0f)) : Pivote("Caneca", v, new Vector3(0f, 0f, -0.25f), Vector3.zero);
            Vector3 b = volcada ? new Vector3(0f, -0.2f, 0f) : Vector3.zero;
            Cil("Cuerpo", t, b + new Vector3(0f, 0.3f, 0f), 0.2f, 0.6f, "M_Utl_PlasticoGris");
            Cil("Borde", t, b + new Vector3(0f, 0.6f, 0f), 0.21f, 0.03f, "M_Utl_MetalOscuro");
            Cil("Contenido", t, b + new Vector3(0f, 0.59f, 0f), 0.18f, 0.02f, "M_Utl_Bolsa");
            var rnd = new System.Random(nombre.Length);
            if (!volcada)
                for (int i = 0; i < 3; i++) Esfera("Papel", t, new Vector3(R(rnd, -0.1f, 0.1f), 0.63f, R(rnd, -0.1f, 0.1f)), new Vector3(0.08f, 0.07f, 0.08f), "M_Papel");
            else
            {
                // Lo que se regó al caer
                for (int i = 0; i < 6; i++)
                    Plano("Hoja", v, new Vector3(R(rnd, -0.5f, 0.5f), 0.004f + i * 0.0006f, R(rnd, -1.4f, -0.9f)), new Vector2(0.21f, 0.297f), "M_Papel", new Vector3(90f, rnd.Next(0, 360), 0f));
                for (int i = 0; i < 3; i++)
                    Cil("Lata", v, new Vector3(R(rnd, -0.4f, 0.4f), 0.033f, R(rnd, -1.3f, -0.9f)), 0.033f, 0.12f, "M_Lata_Energetica", new Vector3(0f, rnd.Next(0, 180), 90f));
                Esfera("Bolsa", v, new Vector3(0.2f, 0.12f, -1.0f), new Vector3(0.35f, 0.24f, 0.3f), "M_Utl_Bolsa");
            }
            Terminar(raiz, !volcada, true, volcada ? (Bounds?)null : new Bounds(new Vector3(0f, 0.3f, -0.25f), new Vector3(0.42f, 0.6f, 0.42f)));
        }

        /// <summary>Paneles del cielo raso que se cayeron, con trozos rotos.</summary>
        static void PrefabEscombros(int variante)
        {
            var raiz = new GameObject("Escombros_" + variante);
            var v = Visual(raiz);
            var rnd = new System.Random(800 + variante);
            Caja("Panel", v, new Vector3(0f, 0.12f, 0f), new Vector3(0.6f, 0.02f, 0.6f), "M_Utl_Panel_Techo", new Vector3(R(rnd, 15f, 25f), rnd.Next(0, 90), 0f));
            for (int i = 0; i < 6 + variante * 3; i++)
            {
                float s = R(rnd, 0.06f, 0.22f);
                Caja("Trozo", v, new Vector3(R(rnd, -0.7f, 0.7f), 0.012f, R(rnd, -0.7f, 0.7f)), new Vector3(s, 0.02f, s * R(rnd, 0.5f, 1f)), "M_Utl_Panel_Techo", new Vector3(R(rnd, -8f, 8f), rnd.Next(0, 180), 0f));
            }
            Caja("Perfil", v, new Vector3(0.2f, 0.02f, -0.3f), new Vector3(1.1f, 0.025f, 0.03f), "M_Utl_MetalGris", new Vector3(0f, R(rnd, 0f, 180f), 3f));
            Terminar(raiz, false, true);
        }

        static void PrefabMancha(string nombre, string material, float tam)
        {
            var raiz = new GameObject(nombre);
            var v = Visual(raiz);
            Plano("Mancha", v, new Vector3(0f, 0.006f, 0f), new Vector2(tam, tam * 0.8f), material, new Vector3(90f, 0f, 0f));
            Terminar(raiz, false, false);
        }

        /// <summary>Dos conos de seguridad con cinta de precaución entre ellos (área acordonada).</summary>
        static void PrefabConos()
        {
            var raiz = new GameObject("Conos_Precaucion");
            var v = Visual(raiz);
            foreach (float x in new[] { -0.9f, 0.9f })
            {
                Caja("Base", v, new Vector3(x, 0.015f, -0.5f), new Vector3(0.34f, 0.03f, 0.34f), "M_Utl_Naranja");
                for (int i = 0; i < 4; i++)
                    Cil("Cono", v, new Vector3(x, 0.08f + i * 0.13f, -0.5f), 0.13f - i * 0.028f, 0.13f, i == 2 ? "M_Utl_Blanco" : "M_Utl_Naranja");
            }
            Caja("Cinta", v, new Vector3(0f, 0.55f, -0.5f), new Vector3(1.8f, 0.05f, 0.004f), "M_Peligro");
            Caja("Cinta_Suelta", v, new Vector3(0.55f, 0.3f, -0.5f), new Vector3(0.6f, 0.05f, 0.004f), "M_Peligro", new Vector3(0f, 0f, -60f));
            Terminar(raiz, true, true, new Bounds(new Vector3(0f, 0.3f, -0.5f), new Vector3(2.1f, 0.6f, 0.35f)));
        }

        static void PrefabCables(int variante)
        {
            var raiz = new GameObject("Cables_Suelo_" + variante);
            var v = Visual(raiz);
            var rnd = new System.Random(900 + variante);
            string[] mats = { "M_Plastico_Negro", "M_Utl_Naranja", "M_Utl_Azul" };
            for (int c = 0; c < 2 + variante; c++)
            {
                Vector3 p = new Vector3(R(rnd, -0.4f, 0.4f), 0.015f, -0.02f);
                float ang = R(rnd, -30f, 30f);
                for (int s = 0; s < 9; s++)
                {
                    ang += R(rnd, -35f, 35f);
                    var dir = Quaternion.Euler(0f, 180f + ang, 0f) * Vector3.forward;
                    float largo = R(rnd, 0.18f, 0.3f);
                    Cil("Tramo", v, p + dir * largo * 0.5f, 0.012f, largo + 0.02f, mats[c % mats.Length], Quaternion.LookRotation(dir).eulerAngles + new Vector3(90f, 0f, 0f));
                    p += dir * largo;
                }
            }
            Terminar(raiz, false, false);
        }

        // =====================================================================
        //  EFECTOS
        // =====================================================================
        static void ConstruirEfectos()
        {
            var mParticula = M("M_Particula_Aditiva");
            Chispas("FX_Chispas", mParticula, false);
            Chispas("FX_Chispazo", mParticula, true);
            Polvo(mParticula);
            PrefabSenuelo();
        }

        static ParticleSystem NuevoSistema(GameObject go)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        /// <summary>Chispas naranjas que saltan y caen. "intermitente" = cable roto que chisporrotea cada pocos segundos.</summary>
        static void Chispas(string nombre, Material mat, bool intermitente)
        {
            var go = new GameObject(nombre);
            var ps = NuevoSistema(go);
            var main = ps.main;
            main.duration = intermitente ? 3.5f : 0.6f;
            main.loop = intermitente;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.45f, 0.1f));
            main.gravityModifier = 1.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            main.playOnAwake = true;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(intermitente
                ? new[] { new ParticleSystem.Burst(0.2f, 18), new ParticleSystem.Burst(0.45f, 8), new ParticleSystem.Burst(2.3f, (short)14, (short)22, 1, 0.01f) { probability = 0.6f } }
                : new[] { new ParticleSystem.Burst(0f, 40) });
            var forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Sphere;
            forma.radius = 0.06f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.05f; r.lengthScale = 1f;
            r.shadowCastingMode = ShadowCastingMode.Off;
            if (!intermitente)
            {
                // Destello de luz breve del chispazo
                var luz = new GameObject("Destello").AddComponent<Light>();
                luz.transform.SetParent(go.transform, false);
                luz.type = LightType.Point; luz.range = 3f; luz.intensity = 3f;
                luz.color = new Color(0.6f, 0.9f, 1f);
                luz.gameObject.AddComponent<DestelloLuz>();
            }
            PrefabUtility.SaveAsPrefabAsset(go, $"{RutaPre}/Efectos/{nombre}.prefab");
            Object.DestroyImmediate(go);
        }

        /// <summary>Motas de polvo flotando en el aire (la caja de emisión se ajusta al tamaño de cada sala).</summary>
        static void Polvo(Material mat)
        {
            var go = new GameObject("FX_Polvo");
            var ps = NuevoSistema(go);
            var main = ps.main;
            main.duration = 10f; main.loop = true; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
            main.startColor = new Color(0.85f, 0.9f, 1f, 0.35f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 70;
            var em = ps.emission;
            em.rateOverTime = 6f;
            var forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(8f, 2.4f, 8f);
            var ruido = ps.noise;
            ruido.enabled = true; ruido.strength = 0.08f; ruido.frequency = 0.25f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            PrefabUtility.SaveAsPrefabAsset(go, $"{RutaPre}/Efectos/FX_Polvo.prefab");
            Object.DestroyImmediate(go);
        }

        /// <summary>Repetidor de red que lanza Neyder: caja, antena, LED, anillo de pulso y luz.</summary>
        static void PrefabSenuelo()
        {
            var raiz = new GameObject("Senuelo_Red");
            var v = Visual(raiz);
            Caja("Cuerpo", v, new Vector3(0f, 0.04f, 0f), new Vector3(0.18f, 0.08f, 0.12f), "M_Plastico_Negro");
            Caja("Franja", v, new Vector3(0f, 0.081f, 0f), new Vector3(0.16f, 0.004f, 0.02f), "M_Neon_Cian");
            Cil("Antena", v, new Vector3(0.06f, 0.2f, 0.03f), 0.006f, 0.25f, "M_Disco_Metal");
            Caja("Punta", v, new Vector3(0.06f, 0.33f, 0.03f), new Vector3(0.02f, 0.02f, 0.02f), "M_Utl_LED_Rojo");
            Combinar(v, raiz.transform, "Senuelo_Red", false);

            var anillo = Pieza(PrimitiveType.Quad, "Anillo", raiz.transform, new Vector3(0f, 0.02f, 0f), Vector3.one, "M_Anillo_Senal", new Vector3(90f, 0f, 0f));
            anillo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var pivAnillo = new GameObject("Pulso").transform;
            pivAnillo.SetParent(raiz.transform, false);
            anillo.transform.SetParent(pivAnillo, true);
            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(raiz.transform, false);
            luz.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            luz.type = LightType.Point; luz.range = 4f; luz.intensity = 2f;
            luz.color = new Color(0.2f, 0.85f, 1f);
            luz.shadows = LightShadows.None;

            var s = raiz.AddComponent<SenueloRed>();
            var so = new SerializedObject(s);
            so.FindProperty("luz").objectReferenceValue = luz;
            so.FindProperty("anillo").objectReferenceValue = pivAnillo;
            so.FindProperty("efectoFin").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{RutaPre}/Efectos/FX_Chispas.prefab");
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, $"{RutaPre}/Efectos/Senuelo_Red.prefab");
            Object.DestroyImmediate(raiz);
        }
    }
}
