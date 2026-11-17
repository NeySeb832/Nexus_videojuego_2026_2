using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus
{
    /// <summary>
    /// Movimiento del técnico con CharacterController.
    ///
    /// Modo TANQUE (por defecto, como los survival horror clásicos):
    ///   W / S (o stick arriba/abajo) = avanzar / retroceder en la dirección hacia la que mira,
    ///   A / D (stick izquierda/derecha) = girar sobre sí mismo, Shift = correr,
    ///   Shift + S = media vuelta rápida.
    /// Con cámaras fijas este control es el más cómodo: el cambio de cámara nunca altera hacia
    /// dónde camina el personaje.
    ///
    /// Modo relativo a la cámara (alternativo): "arriba" es hacia el fondo de la cámara activa.
    /// Al cambiar de cámara se conserva la orientación anterior hasta que el jugador suelte o
    /// cambie claramente la dirección, para que no se dé la vuelta solo.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ControladorJugador : MonoBehaviour
    {
        [SerializeField] bool controlTanque = true;

        [Header("Velocidades (iguales a las de las animaciones de Mixamo, para que no patinen)")]
        [SerializeField] float velocidadCaminar = 1.8f;
        [SerializeField] float velocidadCorrer = 5.0f;
        [SerializeField] float velocidadRetroceder = 1.2f;

        [Header("Giro")]
        [Tooltip("Modo tanque: grados por segundo al girar con A/D")]
        [SerializeField] float giroTanque = 170f;
        [Tooltip("Modo tanque: duración de la media vuelta rápida (Shift + S)")]
        [SerializeField] float duracionMediaVuelta = 0.35f;
        [Tooltip("Modo relativo a la cámara: grados por segundo al orientarse")]
        [SerializeField] float giroGradosPorSegundo = 600f;

        [SerializeField] float gravedad = -20f;
        [SerializeField] Animator animator;

        static readonly int ParamVelocidad = Animator.StringToHash("Velocidad");
        static readonly int ParamMultiplicador = Animator.StringToHash("MultiplicadorVel");

        CharacterController cc;
        InputAction accionMover, accionCorrer;
        float velocidadVertical;
        bool tieneMultiplicador;

        // Modo tanque
        bool corriendoAntes, atrasAntes;
        float mediaVueltaRestante;
        float yawMediaVueltaDestino;

        // Modo relativo a la cámara
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
        /// <summary>Correr forzado para pruebas automáticas.</summary>
        public bool? CorrerSimulado { get; set; }

        public bool ControlTanque { get => controlTanque; set => controlTanque = value; }

        /// <summary>Apuntando o atacando: el técnico no camina, solo gira (lo controla ArmaJugador).</summary>
        public bool Apuntando { get; set; }
        /// <summary>Giro pedido por el jugador en este cuadro (-1 a 1), para soltar el apuntado automático.</summary>
        public float EntradaGiro { get; private set; }

        Vector2 LeerEntrada() => EntradaSimulada ?? (accionMover != null ? accionMover.ReadValue<Vector2>() : Vector2.zero);
        bool LeerCorrer() => CorrerSimulado ?? (accionCorrer != null && accionCorrer.IsPressed());

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
            if (animator != null)
                foreach (var p in animator.parameters)
                    if (p.nameHash == ParamMultiplicador) tieneMultiplicador = true;
            if (controlTanque) Invoke(nameof(RecordarControles), 0.6f);
        }

        void RecordarControles()
        {
            // Durante la cinemática de entrada se espera a que termine
            if (Cinematica.EnCurso) { Invoke(nameof(RecordarControles), 0.8f); return; }
            HUDNexus.Aviso("W / S  avanzar · retroceder     A / D  girar     SHIFT  correr     SHIFT + S  media vuelta");
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
            bool corriendo = PuedeMoverse && LeerCorrer();
            EntradaGiro = Mathf.Abs(entrada.x) > 0.2f ? entrada.x : 0f;
            if (Apuntando)
            {
                // Con el arma levantada no se camina: solo se gira, más despacio
                transform.Rotate(0f, EntradaGiro * giroTanque * 0.6f * Time.deltaTime, 0f);
                entrada = Vector2.zero; corriendo = false;
            }

            Vector3 movimiento;
            float velocidadAnimacion, sentido = 1f;
            if (controlTanque) movimiento = MovimientoTanque(entrada, corriendo, out velocidadAnimacion, out sentido);
            else movimiento = MovimientoRelativoCamara(entrada, corriendo, out velocidadAnimacion);

            if (cc.isGrounded && velocidadVertical < 0f) velocidadVertical = -2f;
            velocidadVertical += gravedad * Time.deltaTime;
            cc.Move((movimiento + Vector3.up * velocidadVertical) * Time.deltaTime);

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(ParamVelocidad, velocidadAnimacion, 0.1f, Time.deltaTime);
                if (tieneMultiplicador) animator.SetFloat(ParamMultiplicador, sentido);
            }
        }

        // ======================= Modo tanque =======================
        Vector3 MovimientoTanque(Vector2 entrada, bool corriendo, out float velocidadAnimacion, out float sentido)
        {
            sentido = 1f;
            float avance = Mathf.Abs(entrada.y) > 0.2f ? entrada.y : 0f;
            float giro = Mathf.Abs(entrada.x) > 0.2f ? entrada.x : 0f;
            bool atras = avance < -0.5f;

            // Media vuelta rápida: Shift + S (al presionar cualquiera de las dos teniendo la otra)
            if (mediaVueltaRestante <= 0f && atras && corriendo && (!corriendoAntes || !atrasAntes))
            {
                mediaVueltaRestante = duracionMediaVuelta;
                yawMediaVueltaDestino = transform.eulerAngles.y + 180f;
            }
            corriendoAntes = corriendo; atrasAntes = atras;

            if (mediaVueltaRestante > 0f)
            {
                float paso = 180f / duracionMediaVuelta * Time.deltaTime;
                mediaVueltaRestante -= Time.deltaTime;
                float yaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, yawMediaVueltaDestino, paso);
                if (mediaVueltaRestante <= 0f) yaw = yawMediaVueltaDestino;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                VelocidadActual = 0f;
                velocidadAnimacion = velocidadCaminar * 0.6f;   // pasitos mientras gira
                return Vector3.zero;
            }

            transform.Rotate(0f, giro * giroTanque * Time.deltaTime, 0f);

            float velocidad;
            if (avance > 0f) velocidad = (corriendo ? velocidadCorrer : velocidadCaminar) * avance;
            else if (avance < 0f) { velocidad = velocidadRetroceder * avance; sentido = -1f; }   // camina hacia atrás (animación en reversa)
            else velocidad = 0f;

            VelocidadActual = Mathf.Abs(velocidad);
            // Girando en el sitio: un paso suave para que no "patine" quieto
            velocidadAnimacion = VelocidadActual > 0.01f ? VelocidadActual : Mathf.Abs(giro) * velocidadCaminar * 0.5f;
            return transform.forward * velocidad;
        }

        // ======================= Modo relativo a la cámara =======================
        Vector3 MovimientoRelativoCamara(Vector2 entrada, bool corriendo, out float velocidadAnimacion)
        {
            // Se libera el bloqueo cuando el jugador suelta o cambia claramente de dirección
            if (direccionBloqueada &&
                (entrada.sqrMagnitude < 0.04f || Vector2.Angle(entrada, entradaAlCambiar) > 40f))
                direccionBloqueada = false;
            if (!direccionBloqueada && camaraActual != null)
                referencia = OrientacionPlana(camaraActual.transform);

            Vector3 direccion = referencia * new Vector3(entrada.x, 0f, entrada.y);
            float intensidad = Mathf.Clamp01(entrada.magnitude);
            VelocidadActual = (corriendo ? velocidadCorrer : velocidadCaminar) * intensidad;

            if (direccion.sqrMagnitude > 0.001f)
            {
                Quaternion objetivo = Quaternion.LookRotation(direccion);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, objetivo,
                    giroGradosPorSegundo * Time.deltaTime);
            }
            velocidadAnimacion = VelocidadActual;
            return direccion.normalized * VelocidadActual;
        }
    }
}
