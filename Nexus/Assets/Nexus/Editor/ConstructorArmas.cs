using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Importa los modelos de las armas (cuchillo, pistola y escopeta) desde las descargas
    /// originales (carpeta Videojuego/Modelos/_fuente, fuera del repositorio por su peso) y prepara
    /// sus materiales para URP:
    ///  - texturas reducidas de 4096 a 1024 px (en pantalla un arma ocupa poco; ahorra memoria en
    ///    Android y WebGL y deja el repositorio liviano);
    ///  - mapas normales en formato DirectX convertidos a OpenGL (se invierte el canal verde);
    ///  - mapa metálico + rugosidad combinados en uno solo (URP lee el metal en R y la suavidad,
    ///    que es 1 - rugosidad, en el canal alfa);
    ///  - un material por conjunto de texturas, asignado al modelo por el nombre de su material.
    /// Menú: Nexus → Armas → 1. Importar modelos y materiales.
    /// </summary>
    public static partial class ConstructorArmas
    {
        const string Destino = "Assets/Nexus/Modelos/Armas";
        static string Fuente => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Modelos/_fuente"));

        /// <summary>Un conjunto de texturas = un material del modelo.</summary>
        class Conjunto
        {
            public string Nombre, Base, Normal, Metal, Rugosidad, AO;
            public bool NormalDirectX;
            public string[] CoincideCon;   // partes del nombre del material del modelo que usan este conjunto
        }

        class Arma
        {
            public string Nombre, Archivo;
            public Conjunto[] Conjuntos;
        }

        static readonly Arma[] Armas =
        {
            new Arma
            {
                Nombre = "Pistola", Archivo = "M9_Handgun.fbx",
                Conjuntos = new[]
                {
                    new Conjunto { Nombre = "XC2", Base = "XC2_Base_Color.jpg", Normal = "XC2_Normal_DirectX.jpg", NormalDirectX = true,
                                   Metal = "XC2_Metallic.jpg", Rugosidad = "XC2_Roughness.jpg", AO = "XC2_Mixed_AO.jpg", CoincideCon = new[] { "xc2" } },
                    new Conjunto { Nombre = "M9", Base = "M9_Base_Color.png", Normal = "M9_Normal_DirectX.png", NormalDirectX = true,
                                   Metal = "M9_Metallic.png", Rugosidad = "M9_Roughness.png", AO = "M9_Mixed_AO.png", CoincideCon = new[] { "" } },
                }
            },
            new Arma
            {
                Nombre = "Escopeta", Archivo = "Remington870.fbx",
                Conjuntos = new[]
                {
                    new Conjunto { Nombre = "Remington", Base = "Material.001_Base_color.png", Normal = "Material.001_Normal_OpenGL.png",
                                   Metal = "Material.001_Metallic.png", Rugosidad = "Material.001_Roughness.png", AO = "Material.001_Mixed_AO.png", CoincideCon = new[] { "" } },
                }
            },
            new Arma
            {
                Nombre = "Cuchillo", Archivo = "Bayoneta.dae",
                Conjuntos = new[]
                {
                    new Conjunto { Nombre = "Hoja", Base = "Knife_albedo.jpg", Normal = "Knife_normal.png", Metal = "Knife_metallic.jpg", Rugosidad = "Knife_roughness.jpg", AO = "Knife_AO.jpg", CoincideCon = new[] { "knife" } },
                    new Conjunto { Nombre = "Mango", Base = "Grip_albedo.jpg", Normal = "Grip_normal.png", Metal = "Grip_metallic.jpg", Rugosidad = "Grip_roughness.jpg", AO = "Grip_AO.jpg", CoincideCon = new[] { "grip" } },
                    new Conjunto { Nombre = "Guarda", Base = "Mid_albedo.jpg", Normal = "Mid_normal.png", Metal = "Mid_metallic.jpg", Rugosidad = "Mid_roughness.jpg", AO = "Mid_AO.jpg", CoincideCon = new[] { "mid", "" } },
                }
            },
        };

        [MenuItem("Nexus/Armas/1. Importar modelos y materiales")]
        public static void ImportarModelos()
        {
            if (!Directory.Exists(Fuente)) { Debug.LogError("[Nexus] No existe la carpeta de origen " + Fuente); return; }
            var informe = new List<string>();
            foreach (var arma in Armas)
            {
                string carpeta = $"{Destino}/{arma.Nombre}";
                Directory.CreateDirectory(carpeta);
                string origen = Path.Combine(Fuente, arma.Nombre);

                // Materiales
                var materiales = new Dictionary<Conjunto, Material>();
                foreach (var cj in arma.Conjuntos)
                    materiales[cj] = CrearMaterial(arma.Nombre, cj, origen, carpeta);

                // Modelo: se importa con sus materiales embebidos solo para leer sus nombres y
                // reasignarlos a los nuestros
                string rutaModelo = $"{carpeta}/{arma.Nombre}{Path.GetExtension(arma.Archivo)}";
                File.Copy(Path.Combine(origen, arma.Archivo), rutaModelo, true);
                AssetDatabase.ImportAsset(rutaModelo, ImportAssetOptions.ForceUpdate);
                var mi = (ModelImporter)AssetImporter.GetAtPath(rutaModelo);
                mi.importAnimation = false; mi.importCameras = false; mi.importLights = false;
                mi.animationType = ModelImporterAnimationType.None;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
                mi.SaveAndReimport();
                var nombres = AssetDatabase.LoadAllAssetsAtPath(rutaModelo).OfType<Material>().Select(m => m.name).Distinct().ToList();
                foreach (var nombre in nombres)
                {
                    var cj = arma.Conjuntos.FirstOrDefault(c => c.CoincideCon.Any(k => k.Length > 0 && nombre.ToLowerInvariant().Contains(k)))
                             ?? arma.Conjuntos.First(c => c.CoincideCon.Contains(""));
                    mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), nombre), materiales[cj]);
                }
                mi.SaveAndReimport();

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
                var b = Limites(go);
                int tris = go.GetComponentsInChildren<MeshFilter>(true).Sum(f => f.sharedMesh != null ? f.sharedMesh.triangles.Length / 3 : 0)
                         + go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Sum(s => s.sharedMesh != null ? s.sharedMesh.triangles.Length / 3 : 0);
                informe.Add($"{arma.Nombre}: materiales [{string.Join(", ", nombres)}] tamaño {b.size.x:0.###} x {b.size.y:0.###} x {b.size.z:0.###} centro {b.center} triángulos {tris}");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Nexus] Armas importadas:\n" + string.Join("\n", informe));
        }

        static Bounds Limites(GameObject go)
        {
            var inst = Object.Instantiate(go);
            var rs = inst.GetComponentsInChildren<Renderer>();
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds();
            foreach (var r in rs) b.Encapsulate(r.bounds);
            Object.DestroyImmediate(inst);
            return b;
        }

        // ======================= Texturas =======================
        static Material CrearMaterial(string arma, Conjunto cj, string origen, string carpeta)
        {
            string prefijo = $"{carpeta}/T_{arma}_{cj.Nombre}";
            var baseCol = Redimensionar(Cargar(Path.Combine(origen, cj.Base)), 1024);
            File.WriteAllBytes(prefijo + "_Base.jpg", baseCol.EncodeToJPG(92));

            var normal = Redimensionar(Cargar(Path.Combine(origen, cj.Normal)), 1024);
            if (cj.NormalDirectX)
            {
                var px = normal.GetPixels32();
                for (int i = 0; i < px.Length; i++) px[i].g = (byte)(255 - px[i].g);
                normal.SetPixels32(px); normal.Apply();
            }
            File.WriteAllBytes(prefijo + "_Normal.png", normal.EncodeToPNG());

            var metal = Redimensionar(Cargar(Path.Combine(origen, cj.Metal)), 1024);
            var rugosidad = Redimensionar(Cargar(Path.Combine(origen, cj.Rugosidad)), 1024);
            var pm = metal.GetPixels32(); var pr = rugosidad.GetPixels32();
            for (int i = 0; i < pm.Length; i++) pm[i] = new Color32(pm[i].r, 0, 0, (byte)(255 - pr[i].r));
            metal.SetPixels32(pm); metal.Apply();
            File.WriteAllBytes(prefijo + "_MetalSuave.png", metal.EncodeToPNG());

            var ao = Redimensionar(Cargar(Path.Combine(origen, cj.AO)), 512);
            File.WriteAllBytes(prefijo + "_AO.jpg", ao.EncodeToJPG(90));
            foreach (var t in new[] { baseCol, normal, metal, rugosidad, ao }) Object.DestroyImmediate(t);

            AssetDatabase.Refresh();
            Importar(prefijo + "_Base.jpg", false, true);
            Importar(prefijo + "_Normal.png", true, false);
            Importar(prefijo + "_MetalSuave.png", false, false);
            Importar(prefijo + "_AO.jpg", false, false);

            string rutaMat = $"{carpeta}/M_Arma_{arma}_{cj.Nombre}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, rutaMat); }
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefijo + "_Base.jpg"));
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefijo + "_Normal.png"));
            m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefijo + "_MetalSuave.png"));
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Smoothness", 1f);
            m.SetFloat("_WorkflowMode", 1f);
            m.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefijo + "_AO.jpg"));
            m.EnableKeyword("_OCCLUSIONMAP");
            EditorUtility.SetDirty(m);
            return m;
        }

        static Texture2D Cargar(string ruta)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            t.LoadImage(File.ReadAllBytes(ruta));
            return t;
        }

        /// <summary>Reducción con filtro bilineal en GPU (lineal: no altera los valores de los mapas).</summary>
        static Texture2D Redimensionar(Texture2D src, int tam)
        {
            int w = Mathf.Min(tam, src.width), h = Mathf.Min(tam, src.height);
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            src.filterMode = FilterMode.Bilinear;
            Graphics.Blit(src, rt);
            var previo = RenderTexture.active;
            RenderTexture.active = rt;
            var res = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            res.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            res.Apply();
            RenderTexture.active = previo;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(src);
            return res;
        }

        static void Importar(string ruta, bool normal, bool srgb)
        {
            AssetDatabase.ImportAsset(ruta);
            var ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = srgb;
            ti.maxTextureSize = 1024;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
        }
    }
}
