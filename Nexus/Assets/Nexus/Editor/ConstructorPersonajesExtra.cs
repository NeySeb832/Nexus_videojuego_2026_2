using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Importa los personajes secundarios (dos operadores SWAT y un empleado de Nexus), que en el
    /// juego aparecen como cadáveres que el jugador puede registrar.
    ///
    /// Los FBX originales de Mixamo traían las texturas de 4096 px incrustadas (hasta 115 MB por
    /// archivo, más que el límite de GitHub). Se convirtieron antes con Blender
    /// (Evidencias/herramientas/exportar_personaje.py): un FBX sin texturas y, aparte, las texturas
    /// de máximo 1024 px por canal: T_[Modelo]_[Material]_Base / _Normal / _MetalSuave / _Emision.
    /// Aquí se configuran esas texturas, se crean materiales URP y el rig queda como Humanoid.
    /// </summary>
    public static class ConstructorPersonajesExtra
    {
        public const string Carpeta = "Assets/Nexus/Modelos/Personajes";
        public static readonly string[] Modelos = { "Swat_A", "Swat_B", "Empleado" };

        [MenuItem("Nexus/Personajes/Importar SWAT y empleado")]
        public static void Importar()
        {
            foreach (var nombre in Modelos)
            {
                string carpeta = $"{Carpeta}/{nombre}";
                string rutaModelo = $"{carpeta}/{nombre}.fbx";
                AssetDatabase.ImportAsset(rutaModelo, ImportAssetOptions.ForceUpdate);
                var mi = (ModelImporter)AssetImporter.GetAtPath(rutaModelo);
                if (mi == null) { Debug.LogError("[Nexus] Falta " + rutaModelo); continue; }
                mi.importAnimation = false; mi.importCameras = false; mi.importLights = false;
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
                mi.SaveAndReimport();

                var nombres = AssetDatabase.LoadAllAssetsAtPath(rutaModelo).OfType<Material>().Select(m => m.name).Distinct().ToList();
                foreach (var mat in nombres)
                {
                    var m = CrearMaterial(nombre, mat.Replace('.', '_'), carpeta);
                    if (m != null) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), mat), m);
                }
                mi.SaveAndReimport();

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
                var anim = go.GetComponent<Animator>();
                var inst = Object.Instantiate(go);
                var rs = inst.GetComponentsInChildren<Renderer>();
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                Object.DestroyImmediate(inst);
                Debug.Log($"[Nexus] {nombre}: materiales [{string.Join(", ", nombres)}], humanoide válido = {(anim != null && anim.avatar != null && anim.avatar.isHuman)}, tamaño {b.size}");
            }
            AssetDatabase.SaveAssets();
        }

        static Material CrearMaterial(string modelo, string mat, string carpeta)
        {
            string prefijo = $"{carpeta}/T_{modelo}_{mat}";
            if (!File.Exists(prefijo + "_Base.png")) { Debug.LogWarning("[Nexus] Sin textura base para " + mat); return null; }
            bool recorte = mat.ToLowerInvariant().Contains("hair");
            ImportarTextura(prefijo + "_Base.png", false, true, recorte);
            bool conNormal = File.Exists(prefijo + "_Normal.png"), conEmision = File.Exists(prefijo + "_Emision.png");
            if (conNormal) ImportarTextura(prefijo + "_Normal.png", true, false, false);
            ImportarTextura(prefijo + "_MetalSuave.png", false, false, true);
            if (conEmision) ImportarTextura(prefijo + "_Emision.png", false, true, false);

            string rutaMat = $"{carpeta}/M_{modelo}_{mat}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, rutaMat); }
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefijo + "_Base.png"));
            m.SetColor("_BaseColor", Color.white);
            if (conNormal)
            {
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefijo + "_Normal.png"));
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefijo + "_MetalSuave.png"));
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Smoothness", 0.6f);
            m.SetFloat("_WorkflowMode", 1f);
            if (conEmision)
            {
                m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefijo + "_Emision.png"));
                m.SetColor("_EmissionColor", Color.white * 0.6f);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            if (recorte)
            {
                // Pelo y pestañas: recorte por alfa, visible por ambas caras
                m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.4f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0f);
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static void ImportarTextura(string ruta, bool normal, bool srgb, bool alfa)
        {
            AssetDatabase.ImportAsset(ruta);
            var ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = srgb;
            ti.alphaSource = alfa ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            ti.alphaIsTransparency = false;
            ti.maxTextureSize = 1024;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
        }
    }
}
