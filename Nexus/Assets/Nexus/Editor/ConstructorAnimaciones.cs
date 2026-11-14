using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Configura las animaciones de Mixamo como Humanoid y crea el Animator Controller del técnico,
    /// compartido por los dos avatares (Neyder y Deisy) gracias al sistema Humanoid.
    /// Menú: Nexus → Personajes → Configurar animaciones.
    /// </summary>
    public static class ConstructorAnimaciones
    {
        const string Carpeta = "Assets/Nexus/Animaciones/Mixamo";
        const string RutaController = "Assets/Nexus/Animaciones/AC_Tecnico.controller";

        // Velocidades del ControladorJugador: los umbrales del Blend Tree deben coincidir
        public const float VelCaminar = 1.8f, VelCorrer = 5.0f;

        [MenuItem("Nexus/Personajes/Configurar animaciones")]
        public static void Configurar()
        {
            AssetDatabase.Refresh();
            ConfigurarClip("Anim_Reposo", "Reposo", true);
            ConfigurarClip("Anim_Caminar", "Caminar", true);
            ConfigurarClip("Anim_Correr", "Correr", true);
            ConfigurarClip("Anim_Caminar_Herido", "Caminar_Herido", true);
            ConfigurarClip("Anim_Golpe", "Golpe", false);
            ConfigurarClip("Anim_Caminar_Golpe", "Caminar_Golpe", false);
            ConfigurarClip("Anim_Morir", "Morir", false);
            ConfigurarClip("Anim_Pistola_Reposo", "Pistola_Reposo", true);
            ConfigurarClip("Anim_Pistola_Caminar", "Pistola_Caminar", true);
            ConfigurarClip("Anim_Disparar", "Disparar", false);
            ConfigurarClip("Anim_Cuchillo", "Cuchillo", false);
            CrearController();
            Debug.Log("[Nexus] Animaciones configuradas y AC_Tecnico creado.");
        }

        /// <summary>
        /// Rig Humanoid + ajustes del clip. Las animaciones de Mixamo no son "In Place": la cadera se
        /// desplaza. Se deja el desplazamiento horizontal (XZ) como "root motion" y el Animator lo
        /// descarta (applyRootMotion = false), así el personaje anima en el sitio y lo mueve el script.
        /// </summary>
        static void ConfigurarClip(string archivo, string nombreClip, bool bucle)
        {
            string ruta = $"{Carpeta}/{archivo}.fbx";
            var mi = (ModelImporter)AssetImporter.GetAtPath(ruta);
            if (mi == null) { Debug.LogWarning($"[Nexus] Falta {ruta}"); return; }

            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = true;
            mi.importCameras = false;
            mi.importLights = false;
            mi.SaveAndReimport();   // genera el Avatar y los clips por defecto

            var clips = mi.defaultClipAnimations;
            if (clips.Length == 0) { Debug.LogWarning($"[Nexus] {ruta} no tiene animación"); return; }
            var c = clips[0];
            c.name = nombreClip;
            c.loopTime = bucle;
            c.loopPose = bucle;
            c.lockRootRotation = true;        // Root Transform Rotation: Bake Into Pose
            c.keepOriginalOrientation = true;
            c.lockRootHeightY = true;         // Root Transform Position (Y): Bake Into Pose
            c.keepOriginalPositionY = true;
            c.lockRootPositionXZ = false;     // XZ = root motion (se descarta en el Animator)
            mi.clipAnimations = new[] { c };
            mi.SaveAndReimport();
        }

        static AnimationClip Clip(string archivo) =>
            AssetDatabase.LoadAllAssetsAtPath($"{Carpeta}/{archivo}.fbx")
                .OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        static void CrearController()
        {
            AssetDatabase.DeleteAsset(RutaController);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(RutaController);
            ac.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
            ac.AddParameter("Herido", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Golpe", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Muerto", AnimatorControllerParameterType.Bool);

            var sm = ac.layers[0].stateMachine;

            // Locomoción normal: reposo → caminar → correr según la velocidad
            var loco = ac.CreateBlendTreeInController("Locomocion", out BlendTree arbol, 0);
            arbol.blendParameter = "Velocidad";
            arbol.useAutomaticThresholds = false;
            arbol.AddChild(Clip("Anim_Reposo"), 0f);
            arbol.AddChild(Clip("Anim_Caminar"), VelCaminar);
            arbol.AddChild(Clip("Anim_Correr"), VelCorrer);

            // Locomoción herido (salud baja): reposo → caminar herido
            var herido = sm.AddState("Locomocion_Herido", new Vector3(300f, 120f, 0f));
            var arbolHerido = new BlendTree { name = "Herido", blendParameter = "Velocidad", useAutomaticThresholds = false, hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(arbolHerido, ac);
            arbolHerido.AddChild(Clip("Anim_Reposo"), 0f);
            arbolHerido.AddChild(Clip("Anim_Caminar_Herido"), VelCaminar);
            herido.motion = arbolHerido;

            var golpe = sm.AddState("Golpe", new Vector3(300f, -60f, 0f));
            golpe.motion = Clip("Anim_Golpe");
            var morir = sm.AddState("Morir", new Vector3(560f, 40f, 0f));
            morir.motion = Clip("Anim_Morir");

            sm.defaultState = loco;

            // Normal <-> herido
            var aHerido = loco.AddTransition(herido);
            aHerido.hasExitTime = false; aHerido.duration = 0.25f;
            aHerido.AddCondition(AnimatorConditionMode.If, 0f, "Herido");
            var aNormal = herido.AddTransition(loco);
            aNormal.hasExitTime = false; aNormal.duration = 0.25f;
            aNormal.AddCondition(AnimatorConditionMode.IfNot, 0f, "Herido");

            // Golpe desde cualquier estado, vuelve a la locomoción al terminar
            var aGolpe = sm.AddAnyStateTransition(golpe);
            aGolpe.hasExitTime = false; aGolpe.duration = 0.1f; aGolpe.canTransitionToSelf = false;
            aGolpe.AddCondition(AnimatorConditionMode.If, 0f, "Golpe");
            aGolpe.AddCondition(AnimatorConditionMode.IfNot, 0f, "Muerto");
            var finGolpe = golpe.AddTransition(loco);
            finGolpe.hasExitTime = true; finGolpe.exitTime = 0.8f; finGolpe.duration = 0.2f;

            // Muerte
            var aMorir = sm.AddAnyStateTransition(morir);
            aMorir.hasExitTime = false; aMorir.duration = 0.15f; aMorir.canTransitionToSelf = false;
            aMorir.AddCondition(AnimatorConditionMode.If, 0f, "Muerto");

            AgregarReinicio(ac);
            AgregarMultiplicador(ac);
            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Morir → Locomoción cuando "Muerto" vuelve a ser falso: el androide apagado con el
        /// KILLSWITCH se reincorpora. Se agrega sobre el controlador existente (sin recrearlo, para
        /// no cambiar su GUID y romper las referencias de los prefabs y escenas).
        /// </summary>
        [MenuItem("Nexus/Personajes/Actualizar controlador (reinicio y retroceso)")]
        public static void AgregarReinicioExistente()
        {
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaController);
            if (ac == null) { Debug.LogError("[Nexus] Falta " + RutaController); return; }
            AgregarReinicio(ac);
            AgregarMultiplicador(ac);
            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Parámetro "MultiplicadorVel" (por defecto 1) como multiplicador de velocidad de los
        /// estados de locomoción: con -1 la animación de caminar se reproduce en reversa, que es
        /// como el control tipo tanque muestra al técnico retrocediendo.
        /// </summary>
        static void AgregarMultiplicador(AnimatorController ac)
        {
            if (!ac.parameters.Any(p => p.name == "MultiplicadorVel"))
                ac.AddParameter(new AnimatorControllerParameter { name = "MultiplicadorVel", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            foreach (var s in ac.layers[0].stateMachine.states.Select(x => x.state))
                if (s.name == "Locomocion" || s.name == "Locomocion_Herido")
                {
                    s.speedParameter = "MultiplicadorVel";
                    s.speedParameterActive = true;
                }
            Debug.Log("[Nexus] Multiplicador de velocidad de la locomoción listo.");
        }

        static void AgregarReinicio(AnimatorController ac)
        {
            var sm = ac.layers[0].stateMachine;
            var morir = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Morir");
            var loco = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Locomocion");
            if (morir == null || loco == null) return;
            if (morir.transitions.Any(t => t.destinationState == loco)) return;
            var t = morir.AddTransition(loco);
            t.hasExitTime = false; t.duration = 0.8f;
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Muerto");
            Debug.Log("[Nexus] Transición Morir → Locomoción agregada.");
        }
    }
}
