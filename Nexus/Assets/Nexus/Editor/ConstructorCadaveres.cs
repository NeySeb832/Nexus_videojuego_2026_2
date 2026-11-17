using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Cadáveres de quienes entraron antes que los técnicos. Cada modelo (SWAT A, SWAT B, empleado)
    /// se pone en una pose y se "congela": la pose se escribe en los huesos y el prefab ya no
    /// necesita Animator.
    ///
    /// Las poses se describen con los músculos del sistema Humanoid de Unity (valores de -1 a 1,
    /// los mismos que usa el Animator), así que sirven para cualquier personaje humanoide:
    ///  - Tendido: el último cuadro de la animación de muerte de Mixamo, con la cabeza girada.
    ///  - Pared:   sentado en el suelo con la espalda contra la pared, piernas estiradas, cabeza
    ///             caída y brazos sueltos (pose creada a mano, músculo por músculo).
    /// El pivote de "Pared" queda en el punto de la espalda que toca la pared; el de "Tendido", en
    /// el centro del cuerpo. Así el constructor de niveles solo tiene que apoyarlos.
    /// </summary>
    public static class ConstructorCadaveres
    {
        const string Salida = "Assets/Nexus/Prefabs/Cadaveres";

        public class Pose
        {
            public string Base = "neutral";               // "neutral" o el nombre de un clip de Mixamo
            public float Tiempo = 1f;                      // fracción del clip
            public Vector3 Cuerpo;                         // giro del cuerpo (grados)
            public Dictionary<string, float> Musculos = new Dictionary<string, float>();
        }

        static Dictionary<string, float> M(params (string, float)[] v) => v.ToDictionary(x => x.Item1, x => x.Item2);

        public static readonly Dictionary<string, Pose> Poses = new Dictionary<string, Pose>
        {
            ["Tendido"] = new Pose
            {
                Base = "Anim_Morir", Tiempo = 1f,
                Musculos = M(("Head Turn Left-Right", 0.6f), ("Neck Turn Left-Right", 0.4f), ("Head Tilt Left-Right", 0.3f))
            },
            ["Pared"] = new Pose
            {
                Base = "neutral",
                // En los músculos de Unity, "Front-Back" positivo es hacia atrás y "Nod Down-Up"
                // positivo es hacia arriba: piernas al frente y cabeza caída llevan valores negativos
                Cuerpo = new Vector3(-22f, 0f, 0f),
                Musculos = M(
                    ("Spine Front-Back", -0.3f), ("Chest Front-Back", -0.25f), ("Spine Left-Right", 0.15f),
                    ("Neck Nod Down-Up", -0.55f), ("Head Nod Down-Up", -0.6f), ("Head Tilt Left-Right", 0.45f), ("Head Turn Left-Right", -0.25f),
                    ("Left Upper Leg Front-Back", -0.42f), ("Right Upper Leg Front-Back", -0.78f),
                    ("Left Upper Leg In-Out", 0.3f), ("Right Upper Leg In-Out", 0.2f),
                    ("Left Upper Leg Twist In-Out", -0.3f), ("Right Upper Leg Twist In-Out", -0.2f),
                    ("Left Lower Leg Stretch", 0.85f), ("Right Lower Leg Stretch", -0.55f),
                    ("Left Foot Up-Down", 0.2f), ("Right Foot Up-Down", 0.1f),
                    ("Left Arm Down-Up", -0.6f), ("Right Arm Down-Up", -0.55f),
                    ("Left Arm Front-Back", -0.15f), ("Right Arm Front-Back", -0.25f),
                    ("Left Forearm Stretch", 0.5f), ("Right Forearm Stretch", 0.3f),
                    ("Left Hand Down-Up", -0.3f), ("Right Hand Down-Up", -0.2f))
            },
        };

        public static readonly string[] Modelos = { "Swat_A", "Swat_B", "Empleado" };

        [MenuItem("Nexus/Personajes/Crear cadáveres")]
        public static void CrearPrefabs()
        {
            Directory.CreateDirectory(Salida);
            var sangre = MaterialSangre();
            var informe = new List<string>();
            foreach (var modelo in Modelos)
                foreach (var pose in Poses.Keys)
                {
                    var go = Posar(modelo, pose);
                    if (go == null) continue;
                    AgregarSangre(go, sangre, pose == "Pared");
                    AgregarColisiones(go, pose);
                    string ruta = $"{Salida}/Cadaver_{modelo}_{pose}.prefab";
                    PrefabUtility.SaveAsPrefabAsset(go, ruta);
                    informe.Add($"{ruta}");
                    Object.DestroyImmediate(go);
                }
            AssetDatabase.SaveAssets();
            Debug.Log("[Nexus] Cadáveres creados:\n" + string.Join("\n", informe));
        }

        /// <summary>Instancia el modelo con la pose aplicada y el pivote ya ajustado. Lo usan también las vistas previas.</summary>
        public static GameObject Posar(string modelo, string nombrePose)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{ConstructorPersonajesExtra.Carpeta}/{modelo}/{modelo}.fbx");
            if (fbx == null) { Debug.LogError("[Nexus] Falta el modelo " + modelo); return null; }
            var pose = Poses[nombrePose];
            var modeloGo = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            PrefabUtility.UnpackPrefabInstance(modeloGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            modeloGo.name = "Modelo";
            var anim = modeloGo.GetComponent<Animator>();
            var handler = new HumanPoseHandler(anim.avatar, modeloGo.transform);

            var hp = new HumanPose();
            if (pose.Base != "neutral")
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath($"Assets/Nexus/Animaciones/Mixamo/{pose.Base}.fbx").OfType<AnimationClip>()
                           .FirstOrDefault(c => !c.name.StartsWith("__preview"));
                AnimationMode.StartAnimationMode();
                try
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(modeloGo, clip, clip.length * pose.Tiempo - 0.001f);
                    AnimationMode.EndSampling();
                    handler.GetHumanPose(ref hp);
                }
                finally { AnimationMode.StopAnimationMode(); }
            }
            else
            {
                handler.GetHumanPose(ref hp);
                for (int i = 0; i < hp.muscles.Length; i++) hp.muscles[i] = 0f;
                // Dedos algo cerrados: mano sin fuerza
                for (int i = 0; i < HumanTrait.MuscleCount; i++)
                    if (HumanTrait.MuscleName[i].Contains("Stretched")) hp.muscles[i] = 0.1f;
                hp.bodyRotation = Quaternion.Euler(pose.Cuerpo);
            }
            for (int i = 0; i < HumanTrait.MuscleCount; i++)
                if (pose.Musculos.TryGetValue(HumanTrait.MuscleName[i], out float v)) hp.muscles[i] = v;
            handler.SetHumanPose(ref hp);
            anim.enabled = false;   // la pose queda escrita en los huesos

            // Raíz del cadáver: el modelo se mueve dentro para que el pivote quede bien apoyado
            var raiz = new GameObject($"Cadaver_{modelo}_{nombrePose}");
            modeloGo.transform.SetParent(raiz.transform, true);
            var (min, max) = LimitesReales(modeloGo);
            Vector3 centro = (min + max) / 2f;
            Vector3 pivote = nombrePose == "Pared" ? new Vector3(centro.x, min.y, min.z) : new Vector3(centro.x, min.y, centro.z);
            modeloGo.transform.position -= pivote;
            var b = LimitesReales(modeloGo);
            raiz.AddComponent<MarcadorCadaver>().Configurar(b.min, b.max, anim.GetBoneTransform(HumanBodyBones.RightHand), anim.GetBoneTransform(HumanBodyBones.Hips));
            return raiz;
        }

        /// <summary>Límites de las mallas en la pose actual (BakeMesh: los bounds del SkinnedMeshRenderer no se actualizan en el editor).</summary>
        static (Vector3 min, Vector3 max) LimitesReales(GameObject go)
        {
            Vector3 min = Vector3.one * 1e6f, max = -Vector3.one * 1e6f;
            var tmp = new Mesh();
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.BakeMesh(tmp, true);
                var m = smr.transform.localToWorldMatrix;
                foreach (var v in tmp.vertices) { var w = m.MultiplyPoint3x4(v); min = Vector3.Min(min, w); max = Vector3.Max(max, w); }
            }
            Object.DestroyImmediate(tmp);
            return (min, max);
        }

        static void AgregarColisiones(GameObject go, string pose)
        {
            var mc = go.GetComponent<MarcadorCadaver>();
            Vector3 size = mc.Max - mc.Min, c = (mc.Max + mc.Min) / 2f;
            // Bloque bajo: el jugador no camina por encima del cuerpo y los androides lo rodean
            var solido = new GameObject("Colision");
            solido.transform.SetParent(go.transform, false);
            var bc = solido.AddComponent<BoxCollider>();
            bc.center = new Vector3(c.x, 0.2f, c.z);
            bc.size = new Vector3(Mathf.Max(0.4f, size.x * 0.8f), 0.4f, Mathf.Max(0.4f, size.z * 0.85f));
            // Área desde la que se puede registrar (trigger en la raíz: la usa Interactuable)
            var area = go.AddComponent<BoxCollider>();
            area.isTrigger = true;
            area.center = new Vector3(c.x, 1f, c.z);
            area.size = new Vector3(size.x + 1.8f, 2f, size.z + 1.8f);
        }

        // ======================= Sangre =======================
        static Material MaterialSangre()
        {
            string rutaTex = "Assets/Nexus/Materiales/Entorno/T_Charco_Sangre.png", rutaMat = "Assets/Nexus/Materiales/Entorno/M_Charco_Sangre.mat";
            if (!File.Exists(rutaTex))
            {
                // Mancha irregular: varios círculos con ruido, borde suave
                const int n = 256;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var rnd = new System.Random(7);
                var gotas = Enumerable.Range(0, 9).Select(_ => (new Vector2((float)rnd.NextDouble() * 0.5f + 0.25f, (float)rnd.NextDouble() * 0.5f + 0.25f), (float)rnd.NextDouble() * 0.16f + 0.08f)).ToList();
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        var p = new Vector2(x / (float)n, y / (float)n);
                        float d = gotas.Max(g => 1f - Vector2.Distance(p, g.Item1) / g.Item2);
                        d += (Mathf.PerlinNoise(x * 0.05f, y * 0.05f) - 0.5f) * 0.5f;
                        float a = Mathf.Clamp01(d * 4f);
                        float oscuro = 0.55f + 0.45f * Mathf.PerlinNoise(x * 0.12f + 3f, y * 0.12f);
                        t.SetPixel(x, y, new Color(0.32f * oscuro, 0.02f, 0.02f, a * 0.92f));
                    }
                t.Apply();
                File.WriteAllBytes(rutaTex, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(rutaTex);
                var ti = (TextureImporter)AssetImporter.GetAtPath(rutaTex);
                ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
            }
            var m = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, rutaMat); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_Smoothness", 0.5f);   // húmedo pero sin reflejos blancos de las lámparas
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 10;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void AgregarSangre(GameObject go, Material m, bool pared)
        {
            var mc = go.GetComponent<MarcadorCadaver>();
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Charco_Sangre";
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(go.transform, false);
            Vector3 c = (mc.Max + mc.Min) / 2f;
            float tam = pared ? 1.3f : 1.8f;
            q.transform.localPosition = new Vector3(c.x, 0.012f, pared ? 0.45f : c.z);
            q.transform.localRotation = Quaternion.Euler(90f, 37f, 0f);
            q.transform.localScale = new Vector3(tam, tam, 1f);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
