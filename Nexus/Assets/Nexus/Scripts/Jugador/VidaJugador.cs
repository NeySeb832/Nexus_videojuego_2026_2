using System.Collections;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// "Oportunidades de Evasión": el técnico tiene 3 corazones. Cada vez que un androide lo
    /// alcanza pierde uno, recibe un empujón y queda unos segundos invulnerable.
    /// Con 1 corazón camina herido; con 0 muere y salta la pantalla de Game Over.
    /// El Speedmax recupera 1 corazón y el Chocorramo los recupera todos.
    /// </summary>
    public class VidaJugador : MonoBehaviour
    {
        [SerializeField] float segundosInvulnerable = 2.5f;
        [SerializeField] float fuerzaEmpujon = 3.5f;

        static readonly int ParamGolpe = Animator.StringToHash("Golpe");
        static readonly int ParamHerido = Animator.StringToHash("Herido");
        static readonly int ParamMuerto = Animator.StringToHash("Muerto");

        Animator animator;
        ControladorJugador controlador;
        CharacterController cc;
        float invulnerableHasta;

        public bool Muerto { get; private set; }
        public bool Invulnerable => Time.time < invulnerableHasta;

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            controlador = GetComponent<ControladorJugador>();
            cc = GetComponent<CharacterController>();
        }

        void Start() => ActualizarAnimacion();

        public void RecibirDanio(Vector3 origen)
        {
            if (Muerto || Invulnerable || (NivelNexus.Instancia != null && NivelNexus.Instancia.Terminado)) return;

            EstadoPartida.Salud = Mathf.Max(0, EstadoPartida.Salud - 1);
            EstadoPartida.Notificar();
            invulnerableHasta = Time.time + segundosInvulnerable;

            if (EstadoPartida.Salud <= 0)
            {
                Muerto = true;
                controlador.PuedeMoverse = false;
                if (animator != null) animator.SetBool(ParamMuerto, true);
                NivelNexus.Instancia?.GameOver("ANOMALÍA PURGADA", "Los Androides de Seguridad te alcanzaron.");
                return;
            }

            if (animator != null) animator.SetTrigger(ParamGolpe);
            HUDNexus.Aviso("¡Te alcanzó un androide!");
            StartCoroutine(Empujon(origen));
            ActualizarAnimacion();
        }

        IEnumerator Empujon(Vector3 origen)
        {
            Vector3 dir = transform.position - origen; dir.y = 0f;
            dir = dir.sqrMagnitude < 0.01f ? -transform.forward : dir.normalized;
            controlador.PuedeMoverse = false;
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                cc.Move(dir * fuerzaEmpujon * (1f - t / 0.35f) * Time.deltaTime);
                yield return null;
            }
            yield return new WaitForSeconds(0.4f);
            if (!Muerto) controlador.PuedeMoverse = true;
        }

        public bool Curar(int corazones)
        {
            if (Muerto || EstadoPartida.Salud >= EstadoPartida.SaludMaxima) return false;
            EstadoPartida.Salud = Mathf.Min(EstadoPartida.SaludMaxima, EstadoPartida.Salud + corazones);
            EstadoPartida.Notificar();
            ActualizarAnimacion();
            return true;
        }

        void ActualizarAnimacion()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetBool(ParamHerido, EstadoPartida.Salud == 1);
        }
    }
}
