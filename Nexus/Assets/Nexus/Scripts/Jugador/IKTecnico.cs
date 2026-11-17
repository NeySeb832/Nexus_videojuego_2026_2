using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Cinemática inversa (IK) del técnico, sobre las animaciones de la capa "Escopeta":
    ///
    /// ESCOPETA DE CORREDERA (Remington 870). Referencias de técnica de tiro usadas:
    ///  - Culata apoyada en el "bolsillo" del hombro, mejilla sobre la culata, mano fuerte en la
    ///    empuñadura y mano de apoyo en el guardamanos (la corredera).
    ///  - "Listo" (low ready): culata en el hombro y el cañón inclinado hacia abajo, para ver por
    ///    encima del arma.
    ///  - Tras cada disparo: retroceso hacia el hombro y el cañón sube; luego el bombeo (la mano de
    ///    apoyo lleva la corredera atrás y adelante) expulsa la vaina y carga el cartucho siguiente.
    ///  - Recarga: el arma se gira con la ventana de carga hacia abajo y a un lado; la mano de apoyo
    ///    toma un cartucho del cinturón y lo empuja en el tubo, uno por uno.
    /// Cada pose dice dónde va la culata respecto al hombro derecho y cómo se orienta el arma; la
    /// mano derecha (que sostiene el arma) se coloca para que el arma quede así y la izquierda va al
    /// guardamanos o a la ventana de carga. Los codos se guían con "hints".
    ///
    /// MIRADA: en las cinemáticas, el técnico gira la cabeza y el torso para mirar a su alrededor.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class IKTecnico : MonoBehaviour
    {
        [Header("Escopeta: culata respecto al hombro derecho (x derecha, y arriba, z adelante)")]
        public Vector3 culataApuntar = new Vector3(-0.07f, -0.03f, 0.05f);
        public Vector3 giroApuntar = new Vector3(1f, 0f, 0f);
        public Vector3 culataListo = new Vector3(-0.05f, -0.1f, 0.08f);
        public Vector3 giroListo = new Vector3(30f, -14f, 0f);
        public Vector3 culataRecarga = new Vector3(-0.02f, -0.4f, 0.02f);   // culata junto a la cadera
        public Vector3 giroRecarga = new Vector3(-22f, -18f, -40f);

        [Header("Puntos del arma (espacio local, pivote en la empuñadura)")]
        public Vector3 culata = new Vector3(0f, 0.0f, -0.34f);
        public Vector3 guardamanos = new Vector3(0f, -0.015f, 0.3f);
        public Vector3 ventanaCarga = new Vector3(0f, -0.06f, 0.06f);
        public Vector3 cinturon = new Vector3(-0.18f, 0.98f, 0.12f);   // de dónde saca los cartuchos (espacio del personaje)

        [Header("Retroceso y bombeo")]
        public float retroceso = 0.07f;
        public float subidaCanon = 10f;
        public float recorridoBombeo = 0.1f;

        Animator anim;
        ArmaJugador arma;
        int capaEscopeta = -1;
        float pesoApuntar, pesoRecarga, pesoManos;
        Quaternion correccionMano = Quaternion.identity;
        bool correccionLista;

        float mirarInicio = -1f, mirarDuracion;

        void Awake()
        {
            anim = GetComponent<Animator>();
            arma = GetComponentInParent<ArmaJugador>();
            capaEscopeta = anim.GetLayerIndex("Escopeta");
        }

        /// <summary>Cinemática: el técnico mira a la izquierda, a la derecha y vuelve al frente.</summary>
        public void MirarAlrededor(float duracion)
        {
            mirarInicio = Time.time; mirarDuracion = duracion;
        }

        void OnAnimatorIK(int capa)
        {
            bool escopeta = arma != null && arma.Equipada == TipoArma.Escopeta && arma.ArmaEnMano != null && capaEscopeta >= 0;
            int capaFinal = escopeta ? capaEscopeta : 0;
            if (capa != capaFinal) return;

            Mirada(escopeta);
            if (escopeta) ManosEscopeta();
            else
            {
                pesoManos = 0f; armaCalculada = false;
                anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f); anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f); anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            }
        }

        // ======================= Mirada =======================
        void Mirada(bool escopeta)
        {
            var cabeza = anim.GetBoneTransform(HumanBodyBones.Head);
            if (cabeza == null) return;
            if (mirarInicio >= 0f && Time.time - mirarInicio < mirarDuracion)
            {
                float t = (Time.time - mirarInicio) / mirarDuracion;
                // izquierda (0 → 0,35), derecha (0,35 → 0,75), al frente (0,75 → 1)
                float ang = t < 0.35f ? Mathf.SmoothStep(0f, -75f, t / 0.35f)
                          : t < 0.75f ? Mathf.SmoothStep(-75f, 70f, (t - 0.35f) / 0.4f)
                          : Mathf.SmoothStep(70f, 0f, (t - 0.75f) / 0.25f);
                float peso = Mathf.Clamp01(Mathf.Min(t, 1f - t) * 8f);
                Vector3 adelante = Quaternion.Euler(0f, ang, 0f) * transform.forward;
                anim.SetLookAtWeight(peso, 0.3f, 1f, 1f, 0.55f);
                anim.SetLookAtPosition(cabeza.position + adelante * 4f + Vector3.up * (0.25f * Mathf.Sin(t * Mathf.PI * 3f) - 0.15f));
                return;
            }
            if (escopeta && arma.ArmaEnMano != null)
            {
                // Con la escopeta: la vista sigue el cañón (al apuntar, mejilla en la culata)
                var boca = arma.ArmaEnMano.Find("Boca");
                Vector3 haciaDonde = boca != null ? boca.position + arma.ArmaEnMano.forward * 6f : cabeza.position + transform.forward * 6f;
                anim.SetLookAtWeight(Mathf.Lerp(0.25f, 0.7f, pesoApuntar), 0.05f, 1f, 1f, 0.6f);
                anim.SetLookAtPosition(haciaDonde);
            }
            else anim.SetLookAtWeight(0f);
        }

        // ======================= Escopeta =======================
        void ManosEscopeta()
        {
            var hombro = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var mano = anim.GetBoneTransform(HumanBodyBones.RightHand);
            var datos = arma.Datos;
            if (hombro == null || mano == null || datos == null) return;

            float dt = Time.deltaTime;
            // Corrección entre el giro del hueso de la mano y el que espera el IK humanoide (constante
            // para cada avatar). Se mide con la capa ya entera y dos cuadros sin IK, para comparar la
            // misma pose: el hueso (cuadro anterior) y el objetivo de la animación (este cuadro).
            if (!correccionLista)
            {
                if (anim.GetLayerWeight(capaEscopeta) < 0.99f) cuadrosSinIK = 0;
                else if (++cuadrosSinIK >= 3)
                {
                    correccionMano = Quaternion.Inverse(mano.rotation) * anim.GetIKRotation(AvatarIKGoal.RightHand);
                    hombroLocal = transform.InverseTransformPoint(hombro.position);
                    correccionLista = true;
                }
                if (!correccionLista)
                {
                    anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f); anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                    anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                    armaCalculada = false;
                    return;
                }
            }
            pesoManos = Mathf.MoveTowards(pesoManos, 1f, dt * 5f);
            pesoApuntar = Mathf.MoveTowards(pesoApuntar, arma.Apuntando ? 1f : 0f, dt * 7f);
            pesoRecarga = Mathf.MoveTowards(pesoRecarga, arma.Recargando ? 1f : 0f, dt * 6f);

            var (posArma, rotArma, bombeo) = PoseArma();
            posArmaCalc = posArma; rotArmaCalc = rotArma; armaCalculada = true;
            Quaternion cuerpo = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            // Mano derecha: en la empuñadura, girada como la sostiene en la animación de pistola calibrada
            Quaternion rotEnMano = Quaternion.Euler(datos.rotacionEnMano);
            Quaternion rotMano = rotArma * Quaternion.Inverse(rotEnMano);
            Vector3 posMano = posArma - rotMano * Vector3.Scale(mano.lossyScale, datos.posicionEnMano);

            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, pesoManos);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, pesoManos);
            anim.SetIKPosition(AvatarIKGoal.RightHand, posMano);
            anim.SetIKRotation(AvatarIKGoal.RightHand, rotMano * correccionMano);
            ManoIzquierda(posArma, rotArma, bombeo, cuerpo);
        }

        Vector3 hombroLocal;
        int cuadrosSinIK;
        bool armaCalculada;
        Vector3 posArmaCalc;
        Quaternion rotArmaCalc;

        /// <summary>
        /// El arma no sigue el giro de la mano (que el IK solo aproxima): se coloca exactamente en su
        /// pose después de la animación, y las manos ya están sobre ella.
        /// </summary>
        void LateUpdate()
        {
            if (!armaCalculada || arma == null || arma.Equipada != TipoArma.Escopeta || arma.ArmaEnMano == null) return;
            var (pos, rot, _) = PoseArma();
            arma.ArmaEnMano.SetPositionAndRotation(Vector3.Lerp(arma.ArmaEnMano.position, pos, pesoManos), Quaternion.Slerp(arma.ArmaEnMano.rotation, rot, pesoManos));
        }

        /// <summary>Pose del arma según el estado (listo, apuntando, recargando) y el retroceso.</summary>
        (Vector3 pos, Quaternion rot, float bombeo) PoseArma()
        {
            Quaternion cuerpo = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            Vector3 c = Vector3.Lerp(culataListo, culataApuntar, pesoApuntar);
            Vector3 g = Vector3.Lerp(giroListo, giroApuntar, pesoApuntar);
            c = Vector3.Lerp(c, culataRecarga, pesoRecarga);
            g = Vector3.Lerp(g, giroRecarga, pesoRecarga);

            // Retroceso: 0,05 s hacia atrás y arriba, vuelve en 0,25 s
            float ts = Time.time - arma.UltimoDisparo;
            float golpe = ts < 0.05f ? ts / 0.05f : ts < 0.3f ? 1f - Mathf.SmoothStep(0f, 1f, (ts - 0.05f) / 0.25f) : 0f;
            c += new Vector3(0f, 0.02f, -retroceso) * golpe;
            g.x -= subidaCanon * golpe;
            // Bombeo: entre 0,32 y 0,68 s después del disparo la corredera va atrás y vuelve
            float bombeo = ts > 0.32f && ts < 0.68f ? Mathf.Sin((ts - 0.32f) / 0.36f * Mathf.PI) : 0f;

            Quaternion rotArma = cuerpo * Quaternion.Euler(g);
            // El hombro se toma del cuerpo (posición medida una vez), no del hueso animado: así el arma
            // queda firme aunque el torso se balancee al caminar, y los brazos compensan con el IK
            Vector3 posCulata = transform.TransformPoint(hombroLocal) + cuerpo * c;
            Vector3 posArma = posCulata - rotArma * culata;
            return (posArma, rotArma, bombeo);
        }

        /// <summary>Mano izquierda: guardamanos (con el bombeo) o, al recargar, del cinturón a la ventana de carga.</summary>
        void ManoIzquierda(Vector3 posArma, Quaternion rotArma, float bombeo, Quaternion cuerpo)
        {
            var hombro = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Vector3 izq = posArma + rotArma * (guardamanos + Vector3.back * (recorridoBombeo * bombeo));
            if (pesoRecarga > 0.01f)
            {
                float tr = Time.time - arma.InicioRecarga - 0.3f;
                float ciclo = tr < 0f ? 0f : Mathf.Repeat(tr, ArmaJugador.SegundosPorCartucho) / ArmaJugador.SegundosPorCartucho;
                Vector3 cinto = transform.position + cuerpo * cinturon;
                Vector3 ventana = posArma + rotArma * ventanaCarga;
                // 0-0,35: va al cinturón · 0,35-0,7: sube a la ventana · 0,7-1: empuja el cartucho
                Vector3 ciclico = ciclo < 0.35f ? Vector3.Lerp(ventana, cinto, Mathf.SmoothStep(0f, 1f, ciclo / 0.35f))
                                : ciclo < 0.7f ? Vector3.Lerp(cinto, ventana, Mathf.SmoothStep(0f, 1f, (ciclo - 0.35f) / 0.35f))
                                : ventana + rotArma * Vector3.forward * (0.05f * Mathf.Sin((ciclo - 0.7f) / 0.3f * Mathf.PI));
                if (tr < 0f) ciclico = Vector3.Lerp(izq, ventana, Mathf.Clamp01((tr + 0.3f) / 0.3f));
                izq = Vector3.Lerp(izq, ciclico, pesoRecarga);
            }
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, pesoManos);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            anim.SetIKPosition(AvatarIKGoal.LeftHand, izq);

            // Codos: el derecho hacia afuera y abajo, el izquierdo debajo del arma
            Vector3 derecha = cuerpo * Vector3.right;
            anim.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0.6f * pesoManos);
            anim.SetIKHintPosition(AvatarIKHint.RightElbow, hombro.position + derecha * 0.35f + Vector3.down * 0.3f - cuerpo * Vector3.forward * 0.1f);
            anim.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0.6f * pesoManos);
            anim.SetIKHintPosition(AvatarIKHint.LeftElbow, izq + Vector3.down * 0.35f - derecha * 0.15f - cuerpo * Vector3.forward * 0.2f);
        }
    }
}
