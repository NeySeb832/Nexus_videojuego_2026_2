using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Segunda parte de las armas: prefabs para la mano y para recoger, munición, calibración de
    /// cómo se sujetan y estados del Animator (apuntar, disparar, acuchillar y caminar armado).
    /// Menú: Nexus → Armas → 2. Prefabs, calibración y animaciones.
    /// </summary>
    public static partial class ConstructorArmas
    {
        const string RutaPrefabs = "Assets/Nexus/Prefabs/Armas";
        const string RutaColeccionables = "Assets/Nexus/Prefabs/Coleccionables";
        const string RutaDatos = "Assets/Nexus/Datos/DatosArmas.asset";
        const string RutaAnimaciones = "Assets/Nexus/Animaciones/Mixamo";
        const string RutaController = "Assets/Nexus/Animaciones/AC_Tecnico.controller";

        [MenuItem("Nexus/Armas/2. Prefabs, calibración y animaciones")]
        public static void ConstruirPrefabs()
        {
            Directory.CreateDirectory(RutaPrefabs);
            // Largo real, hacia dónde apunta el cañón en el modelo (+X / -X), y dónde está la
            // empuñadura (fracción del largo desde atrás y de la altura desde abajo)
            var pistola = CrearVisual("Pistola", "Pistola.fbx", 0.217f, +1, 0.16f, 0.55f, 0.88f);
            var escopeta = CrearVisual("Escopeta", "Escopeta.fbx", 1.0f, -1, 0.34f, 0.42f, 0.84f);
            var cuchillo = CrearVisual("Cuchillo", "Cuchillo.dae", 0.32f, +1, 0.14f, 0.5f, 0.5f);

            CrearRecogible("Cuchillo", TipoColeccionable.Cuchillo, "Bayoneta M9", cuchillo, 2.2f, 1);
            CrearRecogible("Pistola", TipoColeccionable.Pistola, "Pistola M9", pistola, 2.6f, 1);
            CrearRecogible("Escopeta", TipoColeccionable.Escopeta, "Escopeta Remington 870", escopeta, 0.9f, 1);
            CrearMunicion("Balas_Pistola", TipoColeccionable.BalasPistola, "Balas de pistola", false, 12);
            CrearMunicion("Cartuchos_Escopeta", TipoColeccionable.CartuchosEscopeta, "Cartuchos de escopeta", true, 4);

            var datos = AssetDatabase.LoadAssetAtPath<DatosArmas>(RutaDatos);
            if (datos == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Nexus/Datos")) AssetDatabase.CreateFolder("Assets/Nexus", "Datos");
                datos = ScriptableObject.CreateInstance<DatosArmas>();
                AssetDatabase.CreateAsset(datos, RutaDatos);
            }
            datos.pistola = pistola; datos.escopeta = escopeta; datos.cuchillo = cuchillo;
            datos.impacto = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nexus/Prefabs/Efectos/FX_Chispas.prefab");
            datos.laser = MaterialLed("M_Arma_Laser", new Color(1f, 0.05f, 0.04f) * 4f);
            datos.trazadora = MaterialLed("M_Arma_Trazadora", new Color(1f, 0.8f, 0.4f) * 5f);
            Calibrar(datos);
            EditorUtility.SetDirty(datos);

            AgregarEstadosArmas();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Nexus] Armas listas. Sujeción en la mano: posición {datos.posicionEnMano} giro {datos.rotacionEnMano}");
        }

        /// <summary>Prefab del arma para la mano: pivote en la empuñadura, cañón hacia +Z, hijo "Boca" en la punta.</summary>
        static GameObject CrearVisual(string nombre, string archivo, float largo, int signoCanon, float agarreLargo, float agarreAlto, float bocaAlto)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Destino}/{nombre}/{archivo.Replace(Path.GetFileNameWithoutExtension(archivo), nombre)}");
            var raiz = new GameObject("Arma_" + nombre);
            var orientacion = new GameObject("Orientacion").transform;
            orientacion.SetParent(raiz.transform, false);
            orientacion.localRotation = Quaternion.Euler(0f, signoCanon > 0 ? -90f : 90f, 0f);   // ±X del modelo → +Z
            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            modelo.name = "Modelo";
            modelo.transform.SetParent(orientacion, false);

            var b = LimitesMundo(raiz);
            float escala = largo / b.size.z;
            orientacion.localScale = Vector3.one * escala;
            b = LimitesMundo(raiz);
            var agarre = new Vector3(b.center.x, b.min.y + agarreAlto * b.size.y, b.min.z + agarreLargo * b.size.z);
            orientacion.localPosition -= agarre;
            var boca = new GameObject("Boca").transform;
            boca.SetParent(raiz.transform, false);
            boca.localPosition = new Vector3(0f, b.min.y + bocaAlto * b.size.y - agarre.y, b.max.z - agarre.z + 0.01f);
            foreach (var r in raiz.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var ruta = $"{RutaPrefabs}/Arma_{nombre}.prefab";
            var pf = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            Object.DestroyImmediate(raiz);
            return pf;
        }

        static Bounds LimitesMundo(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Arma flotando para recoger (como los demás objetos: gira y flota).</summary>
        static void CrearRecogible(string nombre, TipoColeccionable tipo, string visible, GameObject visualArma, float escala, int cantidad)
        {
            var raiz = new GameObject(nombre);
            var col = raiz.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = Vector3.one * 0.9f;
            raiz.AddComponent<Coleccionable>().Configurar(tipo, visible, null, null, cantidad);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(raiz.transform, false);
            visual.AddComponent<FlotarGirar>();
            var arma = (GameObject)PrefabUtility.InstantiatePrefab(visualArma);
            arma.transform.SetParent(visual.transform, false);
            arma.transform.localScale = Vector3.one * escala;
            // Centrado sobre el pivote del objeto y de costado (se ve el perfil del arma)
            var b = LimitesMundo(arma);
            arma.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            b = LimitesMundo(arma);
            arma.transform.localPosition -= b.center - raiz.transform.position;
            foreach (var r in raiz.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            PrefabUtility.SaveAsPrefabAsset(raiz, $"{RutaColeccionables}/{nombre}.prefab");
            Object.DestroyImmediate(raiz);
        }

        /// <summary>Caja de munición: balas de latón o cartuchos rojos de escopeta.</summary>
        static void CrearMunicion(string nombre, TipoColeccionable tipo, string visible, bool cartuchos, int cantidad)
        {
            var laton = MaterialLit("M_Arma_Laton", new Color(0.78f, 0.58f, 0.22f), 0.9f, 0.7f);
            var rojo = MaterialLit("M_Arma_Cartucho", new Color(0.62f, 0.07f, 0.06f), 0.1f, 0.55f);
            var caja = MaterialLit("M_Arma_Caja_Municion", cartuchos ? new Color(0.12f, 0.16f, 0.1f) : new Color(0.16f, 0.18f, 0.2f), 0.2f, 0.4f);
            var raiz = new GameObject(nombre);
            var col = raiz.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = Vector3.one * 0.9f;
            raiz.AddComponent<Coleccionable>().Configurar(tipo, visible, null, null, cantidad);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(raiz.transform, false);
            visual.AddComponent<FlotarGirar>();
            visual.transform.localScale = Vector3.one * 1.8f;
            Pieza(PrimitiveType.Cube, visual.transform, new Vector3(0f, -0.03f, 0f), new Vector3(0.16f, 0.06f, 0.1f), caja, Vector3.zero);
            Pieza(PrimitiveType.Cube, visual.transform, new Vector3(0f, -0.03f, -0.0505f), new Vector3(0.1f, 0.03f, 0.002f), laton, Vector3.zero);
            if (cartuchos)
                for (int i = 0; i < 4; i++)
                {
                    float x = -0.05f + i * 0.033f;
                    Pieza(PrimitiveType.Cylinder, visual.transform, new Vector3(x, 0.03f, 0f), new Vector3(0.024f, 0.03f, 0.024f), rojo, Vector3.zero);
                    Pieza(PrimitiveType.Cylinder, visual.transform, new Vector3(x, 0.005f, 0f), new Vector3(0.026f, 0.006f, 0.026f), laton, Vector3.zero);
                }
            else
                for (int i = 0; i < 6; i++)
                {
                    float x = -0.055f + (i % 3) * 0.04f, z = i < 3 ? -0.015f : 0.02f;
                    Pieza(PrimitiveType.Cylinder, visual.transform, new Vector3(x, 0.02f, z), new Vector3(0.012f, 0.02f, 0.012f), laton, Vector3.zero);
                    Pieza(PrimitiveType.Sphere, visual.transform, new Vector3(x, 0.042f, z), new Vector3(0.012f, 0.012f, 0.012f), laton, Vector3.zero);
                }
            foreach (var r in raiz.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            PrefabUtility.SaveAsPrefabAsset(raiz, $"{RutaColeccionables}/{nombre}.prefab");
            Object.DestroyImmediate(raiz);
        }

        static void Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 escala, Material mat, Vector3 rot)
        {
            var go = GameObject.CreatePrimitive(tipo);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos; go.transform.localScale = escala; go.transform.localRotation = Quaternion.Euler(rot);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Material MaterialLit(string nombre, Color color, float metal, float suave)
        {
            string ruta = $"{Destino}/{nombre}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, ruta); }
            m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", suave);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material MaterialLed(string nombre, Color color)
        {
            string ruta = $"{Destino}/{nombre}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(Shader.Find("Nexus/LED Parpadeo")); AssetDatabase.CreateAsset(m, ruta); }
            m.SetColor("_Color", color); m.SetFloat("_UsarColorVertice", 0f); m.SetFloat("_Intensidad", 1f);
            m.SetFloat("_Parpadeo", 0f); m.SetFloat("_Pulso", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static AnimationClip ClipDe(string archivo) =>
            AssetDatabase.LoadAllAssetsAtPath($"{RutaAnimaciones}/{archivo}.fbx").OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        /// <summary>
        /// Calcula cómo se sujeta el arma: se pone al avatar en la pose de disparo de Mixamo (brazo
        /// extendido) y se busca la posición de la palma y la dirección del brazo; el arma va en la
        /// palma apuntando hacia donde apunta el brazo. El resultado se guarda relativo al hueso de la
        /// mano, así que sigue a la mano en todas las animaciones (caminar, apuntar, disparar).
        /// </summary>
        static void Calibrar(DatosArmas datos)
        {
            var clip = ClipDe("Anim_Disparar");
            var avatar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nexus/Modelos/Personajes/Neyder.fbx");
            if (clip == null || avatar == null) { Debug.LogWarning("[Nexus] No se pudo calibrar la mano (falta el clip o el avatar)."); return; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(avatar);
            go.transform.SetPositionAndRotation(new Vector3(0f, -300f, 0f), Quaternion.identity);
            var anim = go.GetComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(RutaController);
            try
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(go, clip, clip.length * 0.35f);
                AnimationMode.EndSampling();

                var mano = anim.GetBoneTransform(HumanBodyBones.RightHand);
                var hombro = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
                var dedo = anim.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
                Vector3 palma = dedo != null ? Vector3.Lerp(mano.position, dedo.position, 0.55f) : mano.position + (mano.position - hombro.position).normalized * 0.07f;
                Vector3 adelante = (mano.position - hombro.position).normalized;
                Vector3 arriba = Vector3.ProjectOnPlane(Vector3.up, adelante).normalized;
                var giro = Quaternion.LookRotation(adelante, arriba);
                datos.posicionEnMano = mano.InverseTransformPoint(palma);
                datos.rotacionEnMano = (Quaternion.Inverse(mano.rotation) * giro).eulerAngles;
            }
            finally
            {
                AnimationMode.StopAnimationMode();
                Object.DestroyImmediate(go);
            }
        }

        // ======================= Animator =======================
        /// <summary>
        /// Agrega al controlador del técnico (sin recrearlo, para no romper referencias):
        /// locomoción con arma de fuego, apuntar (primer cuadro de la animación de disparo, quieto),
        /// disparar (retroceso) y acuchillar.
        /// </summary>
        static void AgregarEstadosArmas()
        {
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaController);
            void Param(string n, AnimatorControllerParameterType t) { if (!ac.parameters.Any(p => p.name == n)) ac.AddParameter(n, t); }
            Param("ArmaFuego", AnimatorControllerParameterType.Bool);
            Param("Apuntando", AnimatorControllerParameterType.Bool);
            Param("Disparo", AnimatorControllerParameterType.Trigger);
            Param("Acuchillar", AnimatorControllerParameterType.Trigger);

            var sm = ac.layers[0].stateMachine;
            AnimatorState Estado(string n) => sm.states.Select(s => s.state).FirstOrDefault(s => s.name == n);
            // Se rehacen los estados de armas (los demás no se tocan)
            foreach (var n in new[] { "Locomocion_Arma", "Apuntar", "Disparar", "Cuchillo" })
            {
                var e = Estado(n);
                if (e != null) sm.RemoveState(e);
            }
            foreach (var t in sm.anyStateTransitions.Where(t => t.destinationState == null).ToArray()) sm.RemoveAnyStateTransition(t);

            var loco = Estado("Locomocion");
            var herido = Estado("Locomocion_Herido");

            var locoArma = sm.AddState("Locomocion_Arma", new Vector3(300f, 260f, 0f));
            var arbol = new BlendTree { name = "Arma", blendParameter = "Velocidad", useAutomaticThresholds = false, hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(arbol, ac);
            arbol.AddChild(ClipDe("Anim_Pistola_Reposo"), 0f);
            arbol.AddChild(ClipDe("Anim_Pistola_Caminar"), ConstructorAnimaciones.VelCaminar);
            arbol.AddChild(ClipDe("Anim_Correr"), ConstructorAnimaciones.VelCorrer);
            locoArma.motion = arbol;
            locoArma.speedParameter = "MultiplicadorVel"; locoArma.speedParameterActive = true;

            var apuntar = sm.AddState("Apuntar", new Vector3(560f, 260f, 0f));
            apuntar.motion = ClipDe("Anim_Disparar");
            apuntar.speed = 0f;                 // se queda en la pose de apuntar
            apuntar.cycleOffset = 0.35f;
            var disparar = sm.AddState("Disparar", new Vector3(560f, 380f, 0f));
            disparar.motion = ClipDe("Anim_Disparar");
            disparar.speed = 1.6f;
            var cuchillo = sm.AddState("Cuchillo", new Vector3(300f, 380f, 0f));
            cuchillo.motion = ClipDe("Anim_Cuchillo");
            cuchillo.speed = 1.3f;

            AnimatorStateTransition T(AnimatorState de, AnimatorState a, float dur) { var t = de.AddTransition(a); t.hasExitTime = false; t.duration = dur; return t; }
            // Locomoción normal <-> con arma de fuego
            foreach (var t in loco.transitions.Where(t => t.destinationState == locoArma).ToArray()) loco.RemoveTransition(t);
            T(loco, locoArma, 0.2f).AddCondition(AnimatorConditionMode.If, 0f, "ArmaFuego");
            T(locoArma, loco, 0.2f).AddCondition(AnimatorConditionMode.IfNot, 0f, "ArmaFuego");
            if (herido != null)
            {
                foreach (var t in herido.transitions.Where(t => t.destinationState == locoArma).ToArray()) herido.RemoveTransition(t);
                T(herido, locoArma, 0.2f).AddCondition(AnimatorConditionMode.If, 0f, "ArmaFuego");
            }
            // Apuntar y disparar
            T(locoArma, apuntar, 0.12f).AddCondition(AnimatorConditionMode.If, 0f, "Apuntando");
            T(apuntar, locoArma, 0.15f).AddCondition(AnimatorConditionMode.IfNot, 0f, "Apuntando");
            T(apuntar, disparar, 0.04f).AddCondition(AnimatorConditionMode.If, 0f, "Disparo");
            var vuelta = disparar.AddTransition(apuntar);
            vuelta.hasExitTime = true; vuelta.exitTime = 0.55f; vuelta.duration = 0.1f;
            vuelta.AddCondition(AnimatorConditionMode.If, 0f, "Apuntando");
            var bajar = disparar.AddTransition(locoArma);
            bajar.hasExitTime = true; bajar.exitTime = 0.55f; bajar.duration = 0.15f;
            bajar.AddCondition(AnimatorConditionMode.IfNot, 0f, "Apuntando");
            // Cuchillo desde cualquier estado (menos muerto) y de vuelta a caminar
            var aCuchillo = sm.AddAnyStateTransition(cuchillo);
            aCuchillo.hasExitTime = false; aCuchillo.duration = 0.08f; aCuchillo.canTransitionToSelf = false;
            aCuchillo.AddCondition(AnimatorConditionMode.If, 0f, "Acuchillar");
            aCuchillo.AddCondition(AnimatorConditionMode.IfNot, 0f, "Muerto");
            var finCuchillo = cuchillo.AddTransition(loco);
            finCuchillo.hasExitTime = true; finCuchillo.exitTime = 0.85f; finCuchillo.duration = 0.15f;
            EditorUtility.SetDirty(ac);
        }
    }
}
