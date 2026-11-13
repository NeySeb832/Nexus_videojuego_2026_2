using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus
{
    /// <summary>
    /// Movimiento del técnico con CharacterController, relativo a la cámara fija activa.
    ///
    /// Problema clásico de las cámaras fijas: si el jugador mantiene "arriba" y la cámara cambia
    /// a un ángulo opuesto, de repente "arriba" significa la dirección contraria y el personaje
    /// se da la vuelta. Solución: al cambiar de cámara se conserva la orientación de la cámara
    /// anterior hasta que el jugador suelte o cambie claramente la dirección.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ControladorJugador : MonoBehaviour
    {
        // Igual a la velocidad de las animaciones de Mixamo, para que los pies no "patinen"
        [SerializeField] float velocidadCaminar = 1.8f;
        [SerializeField] float velocidadCorrer = 5.0f;
        [SerializeField] float giroGradosPorSegundo = 600f;
        [SerializeField] float gravedad = -20f;
        [SerializeField] Animator animator;

        static readonly int ParamVelocidad = Animator.StringToHash("Velocidad");

        CharacterController cc;
        InputAction accionMover, accionCorrer;
        float velocidadVertical;

        Camera camaraActual;
        Quaternion referencia = Quaternion.identity;
        bool direccionBloqueada;
        Vector2 entradaAlCambiar;

        /// <summary>Permite congelar al jugador durante diálogos, menús o al recibir daño.</summary>
        public bool PuedeMoverse { get; set; } = true;

        /// <summary>Velocidad horizontal actual (la usan el sonido de pasos y la IA para "oír").</summary>
        public float VelocidadActual { get; private set; }

        /// <summary>Entrada forzada para pruebas automáticas: si tiene valor, reemplaza al teclado/mando.</summary>
        public Vector2? EntradaSimulada { get; set; }

        Vector2 LeerEntrada() => EntradaSimulada ?? accionMover.ReadValue<Vector2>();

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            accionMover = InputSystem.actions.FindAction("Player/Move");
            accionCorrer = InputSystem.actions.FindAction("Player/Sprint");
        }

        void OnEnable()
        {
            GestorCamaras.AlCambiarCamara += CambioDeCamara;
            // Las acciones del proyecto no se habilitan solas en esta versión del Input System
            // (ni en el editor sin recarga de dominio ni en las compilaciones): sin esto,
            // ReadValue devuelve siempre (0,0) y el personaje no camina.
            accionMover?.Enable();
            accionCorrer?.Enable();
        }
        void OnDisable() => GestorCamaras.AlCambiarCamara -= CambioDeCamara;

        void Start()
        {
            camaraActual = GestorCamaras.Instancia != null ? GestorCamaras.Instancia.Activa : Camera.main;
            if (camaraActual != null) referencia = OrientacionPlana(camaraActual.transform);
        }

        void CambioDeCamara(Camera nueva)
        {
            camaraActual = nueva;
            Vector2 entrada = LeerEntrada();
            if (entrada.sqrMagnitude > 0.04f)
            {
                direccionBloqueada = true;
                entradaAlCambiar = entrada;
            }
        }

        static Quaternion OrientacionPlana(Transform t)
        {
            Vector3 f = t.forward;
            f.y = 0f;
            return f.sqrMagnitude < 1e-4f ? Quaternion.identity : Quaternion.LookRotation(f.normalized);
        }

        void Update()
        {
            Vector2 entrada = PuedeMoverse ? LeerEntrada() : Vector2.zero;

            // Se libera el bloqueo cuando el jugador suelta o cambia claramente de dirección
            if (direccionBloqueada &&
                (entrada.sqrMagnitude < 0.04f || Vector2.Angle(entrada, entradaAlCambiar) > 40f))
                direccionBloqueada = false;
            if (!direccionBloqueada && camaraActual != null)
                referencia = OrientacionPlana(camaraActual.transform);

            Vector3 direccion = referencia * new Vector3(entrada.x, 0f, entrada.y);
            float intensidad = Mathf.Clamp01(entrada.magnitude);
            bool corriendo = accionCorrer != null && accionCorrer.IsPressed();
            VelocidadActual = (corriendo ? velocidadCorrer : velocidadCaminar) * intensidad;

            if (direccion.sqrMagnitude > 0.001f)
            {
                Quaternion objetivo = Quaternion.LookRotation(direccion);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, objetivo,
                    giroGradosPorSegundo * Time.deltaTime);
            }

            if (cc.isGrounded && velocidadVertical < 0f) velocidadVertical = -2f;
            velocidadVertical += gravedad * Time.deltaTime;

            Vector3 movimiento = direccion.normalized * VelocidadActual + Vector3.up * velocidadVertical;
            cc.Move(movimiento * Time.deltaTime);

            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetFloat(ParamVelocidad, VelocidadActual, 0.1f, Time.deltaTime);
        }
    }
}
