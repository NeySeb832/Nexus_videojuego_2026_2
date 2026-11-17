using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Animaciones de la escopeta, creadas por código porque no se encontraron animaciones de
    /// escopeta de corredera con la misma estructura de huesos.
    ///
    /// Cada clip es una animación Humanoid escrita músculo por músculo (los mismos músculos que
    /// muestra el Avatar de Unity, de -1 a 1), así que sirve para Neyder y Deisy por igual:
    ///  - Escopeta_Listo:   agarre de las dos manos, cabeza atenta, leve balanceo de respiración.
    ///  - Escopeta_Apuntar: mejilla sobre la culata (cabeza inclinada hacia el arma), dedo en el gatillo.
    ///  - Escopeta_Recarga: cabeza agachada mirando la ventana de carga, mano izquierda abierta para
    ///                      tomar los cartuchos.
    /// Van en una capa propia ("Escopeta") con una máscara de brazos, manos y cabeza: las piernas
    /// siguen caminando con la capa base. Encima, IKTecnico coloca las manos exactamente sobre el
    /// arma y hace el retroceso, el bombeo y la recarga cartucho a cartucho.
    /// </summary>
    public static class ConstructorAnimacionesEscopeta
    {
        const string RutaController = "Assets/Nexus/Animaciones/AC_Tecnico.controller";
        const string Carpeta = "Assets/Nexus/Animaciones/Creadas";

        static Dictionary<string, float> M(params (string, float)[] v) => v.ToDictionary(x => x.Item1, x => x.Item2);

        // Mano cerrada sobre la empuñadura / el guardamanos
        static readonly Dictionary<string, float> AgarreDerecho = M(
            ("RightHand.Thumb.1 Stretched", -0.2f), ("RightHand.Thumb.2 Stretched", -0.4f), ("RightHand.Thumb.3 Stretched", -0.4f), ("RightHand.Thumb.Spread", 0.3f),
            ("RightHand.Index.1 Stretched", -0.25f), ("RightHand.Index.2 Stretched", -0.45f), ("RightHand.Index.3 Stretched", -0.35f),
            ("RightHand.Middle.1 Stretched", -0.75f), ("RightHand.Middle.2 Stretched", -0.85f), ("RightHand.Middle.3 Stretched", -0.75f),
            ("RightHand.Ring.1 Stretched", -0.8f), ("RightHand.Ring.2 Stretched", -0.85f), ("RightHand.Ring.3 Stretched", -0.75f),
            ("RightHand.Little.1 Stretched", -0.8f), ("RightHand.Little.2 Stretched", -0.85f), ("RightHand.Little.3 Stretched", -0.75f));
        static readonly Dictionary<string, float> AgarreIzquierdo = M(
            ("LeftHand.Thumb.1 Stretched", 0.1f), ("LeftHand.Thumb.2 Stretched", -0.2f), ("LeftHand.Thumb.3 Stretched", -0.3f), ("LeftHand.Thumb.Spread", 0.5f),
            ("LeftHand.Index.1 Stretched", -0.55f), ("LeftHand.Index.2 Stretched", -0.65f), ("LeftHand.Index.3 Stretched", -0.5f),
            ("LeftHand.Middle.1 Stretched", -0.6f), ("LeftHand.Middle.2 Stretched", -0.7f), ("LeftHand.Middle.3 Stretched", -0.55f),
            ("LeftHand.Ring.1 Stretched", -0.65f), ("LeftHand.Ring.2 Stretched", -0.7f), ("LeftHand.Ring.3 Stretched", -0.55f),
            ("LeftHand.Little.1 Stretched", -0.7f), ("LeftHand.Little.2 Stretched", -0.7f), ("LeftHand.Little.3 Stretched", -0.55f),
            ("Left Forearm Twist In-Out", 0.6f), ("Left Hand In-Out", 0.2f));
        static readonly Dictionary<string, float> ManoIzquierdaAbierta = M(
            ("LeftHand.Thumb.Spread", 0.6f), ("LeftHand.Index.1 Stretched", -0.1f), ("LeftHand.Middle.1 Stretched", -0.15f),
            ("LeftHand.Ring.1 Stretched", -0.2f), ("LeftHand.Little.1 Stretched", -0.25f), ("Left Forearm Twist In-Out", 0.3f));

        [MenuItem("Nexus/Personajes/Animaciones de escopeta (crear)")]
        public static void Crear()
        {
            if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/Nexus/Animaciones", "Creadas");

            var listo = Clip("Escopeta_Listo", 2.4f, new[]
            {
                (0f,   Junto(AgarreDerecho, AgarreIzquierdo, M(("Head Nod Down-Up", -0.12f), ("Neck Nod Down-Up", -0.1f), ("Right Shoulder Down-Up", 0.05f)))),
                (1.2f, Junto(AgarreDerecho, AgarreIzquierdo, M(("Head Nod Down-Up", -0.18f), ("Neck Nod Down-Up", -0.12f), ("Right Shoulder Down-Up", 0.1f), ("Head Turn Left-Right", 0.05f)))),
                (2.4f, Junto(AgarreDerecho, AgarreIzquierdo, M(("Head Nod Down-Up", -0.12f), ("Neck Nod Down-Up", -0.1f), ("Right Shoulder Down-Up", 0.05f)))),
            });
            var apuntar = Clip("Escopeta_Apuntar", 1.6f, new[]
            {
                (0f,   Junto(AgarreDerecho, AgarreIzquierdo, M(("Head Tilt Left-Right", -0.2f), ("Neck Tilt Left-Right", -0.12f), ("Head Nod Down-Up", -0.25f), ("Right Shoulder Down-Up", 0.25f), ("Right Shoulder Front-Back", 0.2f), ("RightHand.Index.1 Stretched", 0.1f)))),
                (0.8f, Junto(AgarreDerecho, AgarreIzquierdo, M(("Head Tilt Left-Right", -0.22f), ("Neck Tilt Left-Right", -0.13f), ("Head Nod Down-Up", -0.27f), ("Right Shoulder Down-Up", 0.27f), ("Right Shoulder Front-Back", 0.22f), ("RightHand.Index.1 Stretched", 0.1f)))),
                (1.6f, Junto(AgarreDerecho, AgarreIzquierdo, M(("Head Tilt Left-Right", -0.2f), ("Neck Tilt Left-Right", -0.12f), ("Head Nod Down-Up", -0.25f), ("Right Shoulder Down-Up", 0.25f), ("Right Shoulder Front-Back", 0.2f), ("RightHand.Index.1 Stretched", 0.1f)))),
            });
            var recarga = Clip("Escopeta_Recarga", ArmaJugador.SegundosPorCartucho, new[]
            {
                (0f,                                     Junto(AgarreDerecho, ManoIzquierdaAbierta, M(("Head Nod Down-Up", -0.55f), ("Neck Nod Down-Up", -0.4f), ("Head Turn Left-Right", 0.15f)))),
                (ArmaJugador.SegundosPorCartucho * 0.5f, Junto(AgarreDerecho, AgarreIzquierdo,      M(("Head Nod Down-Up", -0.6f), ("Neck Nod Down-Up", -0.45f), ("Head Turn Left-Right", 0.2f)))),
                (ArmaJugador.SegundosPorCartucho,        Junto(AgarreDerecho, ManoIzquierdaAbierta, M(("Head Nod Down-Up", -0.55f), ("Neck Nod Down-Up", -0.4f), ("Head Turn Left-Right", 0.15f)))),
            });
            var mascara = Mascara();
            AgregarCapa(listo, apuntar, recarga, mascara);
            AssetDatabase.SaveAssets();
            Debug.Log("[Nexus] Animaciones de escopeta creadas y capa 'Escopeta' agregada al controlador.");
        }

        static Dictionary<string, float> Junto(params Dictionary<string, float>[] partes)
        {
            var r = new Dictionary<string, float>();
            foreach (var p in partes) foreach (var kv in p) r[kv.Key] = kv.Value;
            return r;
        }

        static AnimationClip Clip(string nombre, float duracion, (float t, Dictionary<string, float> m)[] claves)
        {
            string ruta = $"{Carpeta}/{nombre}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, ruta); }
            clip.ClearCurves();
            foreach (var musculo in claves.SelectMany(c => c.m.Keys).Distinct())
            {
                var curva = new AnimationCurve(claves.Select(c => new Keyframe(c.t, c.m.TryGetValue(musculo, out float v) ? v : 0f)).ToArray());
                for (int i = 0; i < curva.length; i++) AnimationUtility.SetKeyLeftTangentMode(curva, i, AnimationUtility.TangentMode.ClampedAuto);
                for (int i = 0; i < curva.length; i++) AnimationUtility.SetKeyRightTangentMode(curva, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), musculo), curva);
            }
            var ajustes = AnimationUtility.GetAnimationClipSettings(clip);
            ajustes.loopTime = true;
            ajustes.stopTime = duracion;
            AnimationUtility.SetAnimationClipSettings(clip, ajustes);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static AvatarMask Mascara()
        {
            string ruta = $"{Carpeta}/Mascara_Brazos_Cabeza.mask";
            var m = AssetDatabase.LoadAssetAtPath<AvatarMask>(ruta);
            if (m == null) { m = new AvatarMask(); AssetDatabase.CreateAsset(m, ruta); }
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) m.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            foreach (var p in new[] { AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm,
                                      AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers, AvatarMaskBodyPart.LeftHandIK, AvatarMaskBodyPart.RightHandIK })
                m.SetHumanoidBodyPartActive(p, true);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void AgregarCapa(AnimationClip listo, AnimationClip apuntar, AnimationClip recarga, AvatarMask mascara)
        {
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaController);
            if (!ac.parameters.Any(p => p.name == "Recargando")) ac.AddParameter("Recargando", AnimatorControllerParameterType.Bool);
            if (!ac.parameters.Any(p => p.name == "Apuntando")) ac.AddParameter("Apuntando", AnimatorControllerParameterType.Bool);

            // Se reemplaza la capa si ya existía (la herramienta se puede volver a ejecutar)
            var capas = ac.layers.ToList();
            int previa = capas.FindIndex(l => l.name == "Escopeta");
            if (previa >= 0) ac.RemoveLayer(previa);

            var sm = new AnimatorStateMachine { name = "Escopeta", hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(sm, ac);
            var sListo = sm.AddState("Escopeta_Listo", new Vector3(300f, 100f, 0f)); sListo.motion = listo;
            var sApuntar = sm.AddState("Escopeta_Apuntar", new Vector3(300f, 220f, 0f)); sApuntar.motion = apuntar;
            var sRecarga = sm.AddState("Escopeta_Recarga", new Vector3(560f, 160f, 0f)); sRecarga.motion = recarga;
            sm.defaultState = sListo;

            AnimatorStateTransition T(AnimatorState a, AnimatorState b, string param, bool valor, float dur)
            {
                var t = a.AddTransition(b);
                t.hasExitTime = false; t.duration = dur;
                t.AddCondition(valor ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, param);
                return t;
            }
            T(sListo, sApuntar, "Apuntando", true, 0.12f);
            T(sApuntar, sListo, "Apuntando", false, 0.2f);
            T(sListo, sRecarga, "Recargando", true, 0.15f);
            T(sApuntar, sRecarga, "Recargando", true, 0.15f);
            T(sRecarga, sListo, "Recargando", false, 0.2f);

            ac.AddLayer(new AnimatorControllerLayer
            {
                name = "Escopeta", stateMachine = sm, avatarMask = mascara, defaultWeight = 0f,
                blendingMode = AnimatorLayerBlendingMode.Override, iKPass = true
            });
            // La capa base también calcula IK: la mirada de las cinemáticas
            var ls = ac.layers;
            ls[0].iKPass = true;
            ac.layers = ls;
            EditorUtility.SetDirty(ac);
        }
    }
}
