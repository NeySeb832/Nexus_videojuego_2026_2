using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Construye el Androide de Seguridad SENTINEL a partir del Y Bot de Mixamo:
    /// importación Humanoid, materiales (cuerpo metálico oscuro y juntas rojas), visor,
    /// linterna roja, cono de visión (trigger) y NavMeshAgent.
    /// Menú: Nexus → Enemigos → Construir androide.
    /// </summary>
    public static class ConstructorAndroides
    {
        const string Origen = "../Descargas_Nixamo/Y Bot.fbx";
        const string RutaModelo = "Assets/Nexus/Modelos/Androides/YBot.fbx";
        const string RutaMat = "Assets/Nexus/Materiales/Androides";
        const string RutaPrefab = "Assets/Nexus/Prefabs/Enemigos/Androide_Sentinel.prefab";
        public const float DistanciaVision = 9f, AnguloVision = 55f;

        [MenuItem("Nexus/Enemigos/Construir androide")]
        public static void Construir()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaModelo));
            Directory.CreateDirectory(RutaMat);
            Directory.CreateDirectory(Path.GetDirectoryName(RutaPrefab));
            if (!File.Exists(RutaModelo)) File.Copy(Origen, RutaModelo);
            AssetDatabase.ImportAsset(RutaModelo);

            // Rig Humanoid: el androide comparte las animaciones de los técnicos
            var mi = (ModelImporter)AssetImporter.GetAtPath(RutaModelo);
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importAnimation = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.SaveAndReimport();

            // Materiales
            var cuerpo = CrearMaterial("M_Androide_Cuerpo", Shader.Find("Universal Render Pipeline/Lit"));
            cuerpo.SetColor("_BaseColor", new Color(0.13f, 0.14f, 0.16f));
            cuerpo.SetFloat("_Metallic", 0.55f); cuerpo.SetFloat("_Smoothness", 0.7f);
            var juntas = CrearMaterial("M_Androide_Juntas", Shader.Find("Universal Render Pipeline/Lit"));
            juntas.SetColor("_BaseColor", new Color(0.05f, 0.02f, 0.02f));
            juntas.SetFloat("_Smoothness", 0.5f);
            juntas.EnableKeyword("_EMISSION");
            juntas.SetColor("_EmissionColor", new Color(1f, 0.08f, 0.04f) * 2.2f);
            juntas.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            var visor = CrearMaterial("M_Androide_Visor", Shader.Find("Nexus/LED Parpadeo"));
            visor.SetColor("_Color", new Color(1f, 0.05f, 0.03f) * 8f);
            visor.SetFloat("_UsarColorVertice", 0f); visor.SetFloat("_Parpadeo", 0f); visor.SetFloat("_Pulso", 0.6f);
            AssetDatabase.SaveAssets();

            // ---------- Prefab ----------
            var raiz = new GameObject("Androide_Sentinel");
            var rb = raiz.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            var capsula = raiz.AddComponent<CapsuleCollider>();
            capsula.radius = 0.3f; capsula.height = 1.8f; capsula.center = new Vector3(0f, 0.9f, 0f);
            var agente = raiz.AddComponent<NavMeshAgent>();
            agente.radius = 0.35f; agente.height = 1.8f; agente.speed = 1.5f;
            agente.angularSpeed = 320f; agente.acceleration = 14f; agente.stoppingDistance = 0.15f;
            agente.enabled = false;   // lo activa AndroideIA cuando la malla de navegación ya existe
            var ia = raiz.AddComponent<AndroideIA>();
            var audio = raiz.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.spatialBlend = 1f; audio.maxDistance = 25f;

            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo), raiz.transform);
            modelo.name = "Modelo";
            var anim = modelo.GetComponent<Animator>();
            anim.applyRootMotion = false;
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Nexus/Animaciones/AC_Tecnico.controller");
            foreach (var smr in modelo.GetComponentsInChildren<SkinnedMeshRenderer>())
                smr.sharedMaterial = smr.name.ToLower().Contains("joint") ? juntas : cuerpo;

            // Visor rojo pegado al hueso de la cabeza
            var cabeza = anim.GetBoneTransform(HumanBodyBones.Head);
            var v = GameObject.CreatePrimitive(PrimitiveType.Cube);
            v.name = "Visor";
            Object.DestroyImmediate(v.GetComponent<Collider>());
            v.transform.SetParent(cabeza, false);
            v.transform.position = cabeza.position + raiz.transform.forward * 0.1f + Vector3.up * 0.08f;
            v.transform.rotation = raiz.transform.rotation;
            v.transform.localScale = Vector3.one;
            v.transform.localScale = new Vector3(0.17f / cabeza.lossyScale.x, 0.035f / cabeza.lossyScale.y, 0.04f / cabeza.lossyScale.z);
            var vr = v.GetComponent<MeshRenderer>();
            vr.sharedMaterial = visor; vr.shadowCastingMode = ShadowCastingMode.Off;

            // Ojos: punto fijo a la altura de la cabeza, con la linterna y el cono de visión
            var ojos = new GameObject("Ojos").transform;
            ojos.SetParent(raiz.transform, false);
            ojos.localPosition = new Vector3(0f, 1.55f, 0.15f);
            ojos.localRotation = Quaternion.Euler(12f, 0f, 0f);   // mira un poco hacia el suelo

            var luz = new GameObject("Linterna_Roja").AddComponent<Light>();
            luz.transform.SetParent(ojos, false);
            luz.type = LightType.Spot;
            luz.color = new Color(1f, 0.12f, 0.08f);
            luz.spotAngle = AnguloVision + 10f; luz.innerSpotAngle = AnguloVision * 0.6f;
            luz.range = DistanciaVision + 1f; luz.intensity = 7f;
            luz.shadows = LightShadows.None;

            var cono = new GameObject("Cono_Vision");
            cono.transform.SetParent(ojos, false);
            cono.layer = 2;   // Ignore Raycast: no bloquea rayos ni entra en la malla de navegación
            var mc = cono.AddComponent<MeshCollider>();
            mc.sharedMesh = MeshCono();
            mc.convex = true; mc.isTrigger = true;
            cono.AddComponent<ConoVision>().Androide = ia;

            var so = new SerializedObject(ia);
            so.FindProperty("ojos").objectReferenceValue = ojos;
            so.FindProperty("linterna").objectReferenceValue = luz;
            so.FindProperty("animator").objectReferenceValue = anim;
            so.FindProperty("audioAlarma").objectReferenceValue = audio;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Object.DestroyImmediate(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Nexus] Androide SENTINEL construido.");
        }

        static Material CrearMaterial(string nombre, Shader shader)
        {
            string ruta = $"{RutaMat}/{nombre}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, ruta); }
            else m.shader = shader;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Pirámide de 8 lados (cono) con el vértice en los ojos, abierta hacia +Z.</summary>
        static Mesh MeshCono()
        {
            const int lados = 8;
            float radio = DistanciaVision * Mathf.Tan(AnguloVision * 0.5f * Mathf.Deg2Rad);
            var v = new List<Vector3> { Vector3.zero };
            for (int i = 0; i < lados; i++)
            {
                float a = i * Mathf.PI * 2f / lados;
                v.Add(new Vector3(Mathf.Cos(a) * radio, Mathf.Sin(a) * radio, DistanciaVision));
            }
            var t = new List<int>();
            for (int i = 0; i < lados; i++) t.AddRange(new[] { 0, 1 + (i + 1) % lados, 1 + i });
            for (int i = 1; i < lados - 1; i++) t.AddRange(new[] { 1, 1 + i, 2 + i });
            string ruta = "Assets/Nexus/Modelos/Androides/Mesh_Cono_Vision.asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (m == null) { m = new Mesh { name = "Mesh_Cono_Vision" }; AssetDatabase.CreateAsset(m, ruta); }
            m.Clear();
            m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
