using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Nexus
{
    /// <summary>
    /// Androide de Seguridad SENTINEL. Máquina de estados:
    ///  - Inactivo:     apagado hasta que suena la alarma (al extraer un Disco Maestro).
    ///  - Patrulla:     recorre sus puntos de ruta con el NavMeshAgent, a paso lento.
    ///  - Persecución:  el jugador entró en su cono de visión y nada lo tapa → corre hacia él.
    ///  - Búsqueda:     lo perdió de vista, lo oyó correr, oyó una máquina o sigue un señuelo de red
    ///                  → va a ese punto y mira alrededor.
    ///  - Recalibrando: acaba de alcanzar al jugador → se detiene unos segundos (da tiempo a huir).
    ///  - Apagado:      el programa KILLSWITCH de Deisy lo desconectó; se reinicia solo al terminar.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class AndroideIA : MonoBehaviour
    {
        public enum Estado { Inactivo, Patrulla, Persecucion, Busqueda, Recalibrando, Apagado, Destruido }

        /// <summary>Se dispara cuando un androide detecta al jugador (alarma sonora, música de tensión).</summary>
        public static event Action<AndroideIA> AlDetectar;

        /// <summary>Androides activos en la escena (los usan las habilidades y las máquinas ruidosas).</summary>
        public static readonly List<AndroideIA> Todos = new List<AndroideIA>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarEstaticos()
        {
            Todos.Clear();
            AlDetectar = null;
            ultimoAvisoDeteccion = -10f;
            Pausados = false;
        }

        /// <summary>Durante una cinemática los androides no se mueven ni atacan (sí se encienden).</summary>
        public static bool Pausados { get; set; }

        /// <summary>
        /// Alarma general (al extraer el Disco Maestro): todas las unidades se encienden y van tras el
        /// técnico. Durante "segundos" el sistema les transmite su posición aunque no lo vean.
        /// </summary>
        public static void AlarmaGeneral(float segundos)
        {
            foreach (var a in new List<AndroideIA>(Todos)) a.Cazar(segundos);
        }

        /// <summary>Reinicio manual del final: CERBERUS 7.0 desconecta todas las unidades para siempre.</summary>
        public static void DesconectarTodos()
        {
            foreach (var a in new List<AndroideIA>(Todos))
            {
                if (a.Destruido || a.agente == null) continue;
                a.estado = Estado.Apagado;
                a.finApagado = float.MaxValue;
                a.cazarHasta = 0f;
                if (a.agente.enabled && a.agente.isOnNavMesh) { a.agente.ResetPath(); a.agente.isStopped = true; }
                if (a.animator != null) { a.animator.SetFloat(ParamVelocidad, 0f); a.animator.SetBool(ParamMuerto, true); }
                if (a.linterna != null) a.linterna.enabled = false;
                a.Brillo(0f);
            }
        }

        [Header("Ruta y velocidad")]
        [SerializeField] Vector3[] ruta;
        [SerializeField] float velocidadPatrulla = 1.5f;
        [SerializeField] float velocidadPersecucion = 4.0f;
        [SerializeField] float esperaEnPunto = 1.2f;
        [SerializeField] bool activarConAlarma;
        [Tooltip("Anclado en su estación de carga: solo la alarma lo enciende (sin sensor de cercanía)")]
        [SerializeField] bool enEstacion;

        [Header("Sentidos")]
        [SerializeField] Transform ojos;
        [Tooltip("Si el jugador corre a menos de esta distancia, el androide lo oye")]
        [SerializeField] float radioOido = 5f;
        [SerializeField] float distanciaAtrapar = 1.1f;
        [SerializeField] float tiempoOlvido = 3f;
        [SerializeField] float tiempoBusqueda = 4f;
        [SerializeField] float tiempoRecalibrar = 2.5f;

        [Header("Resistencia")]
        [Tooltip("Puntos de vida: pistola 2 por bala, escopeta hasta ~2 por perdigón de cerca, cuchillo 2,5")]
        [SerializeField] float saludMaxima = 12f;

        [Header("Presentación")]
        [SerializeField] Light linterna;
        [SerializeField] Animator animator;
        [SerializeField] AudioSource audioAlarma;

        static readonly int ParamVelocidad = Animator.StringToHash("Velocidad");
        static readonly int ParamMuerto = Animator.StringToHash("Muerto");
        const int MascaraVision = ~(1 << 2);   // ignora la capa "Ignore Raycast" (zonas y triggers)

        NavMeshAgent agente;
        Transform jugador;
        VidaJugador vida;
        ControladorJugador controlJugador;
        Estado estado;
        int indiceRuta;
        float esperaHasta, ultimaVezVisto, finBusqueda, finRecalibrar, proximaVista, proximoDestino;
        float finApagado, atraidoHasta;
        // Unidad en reposo (se activa con la alarma o con su sensor de cercanía)
        float despertarEn = -1f;
        bool perseguirAlDespertar;
        Vector3 destinoAlDespertar;
        Vector3 ultimaPosicion;
        bool visto;
        static float ultimoAvisoDeteccion = -10f;

        // Partes luminosas (visor y juntas rojas): se apagan con el KILLSWITCH
        readonly List<(Renderer r, int propiedad, Color color)> brillos = new List<(Renderer, int, Color)>();
        MaterialPropertyBlock bloque;

        float salud = -1f, aturdidoHasta, destelloHasta;

        public bool JugadorEnCono { get; set; }
        public Estado EstadoActual => estado;
        public bool Destruido => estado == Estado.Destruido;
        public float Salud => salud < 0f ? saludMaxima : salud;
        public Vector3 Pecho => transform.position + Vector3.up * 1.25f;
        /// <summary>Se puede apagar con el KILLSWITCH: está encendido y no está ya desconectado.</summary>
        public bool PuedeApagarse => agente != null && agente.enabled && estado != Estado.Inactivo && estado != Estado.Apagado;
        public float SegundosApagado => estado == Estado.Apagado ? Mathf.Max(0f, finApagado - Time.time) : 0f;

        public void Configurar(Vector3[] puntos, float vPatrulla, float vPersecucion, bool conAlarma, bool estacion = false)
        {
            ruta = puntos; velocidadPatrulla = vPatrulla; velocidadPersecucion = vPersecucion; activarConAlarma = conAlarma || estacion;
            enEstacion = estacion;
        }

        float cazarHasta;
        bool enPausa, detenidoAntes;

        [Tooltip("Unidad de reserva del jefe final: no despierta con la alarma, solo cuando el jefe la llama")]
        [SerializeField] bool reservaDelJefe;
        /// <summary>Multiplica el daño recibido (el jefe: blindado 0,5, expuesto 2).</summary>
        public float MultiplicadorDanio { get; set; } = 1f;
        /// <summary>Lo detiene otro script (el jefe mientras carga su descarga): gira hacia el jugador pero no camina.</summary>
        public bool Detenido { get; set; }
        public float SaludMaxima => saludMaxima;
        public void ConfigurarReserva(bool reserva) => reservaDelJefe = reserva;
        /// <summary>Brillo del visor y las juntas (efectos del jefe).</summary>
        public void Destello(float k) => Brillo(k);

        /// <summary>El jefe llama a sus refuerzos: se encienden y van tras el técnico.</summary>
        public void Activar(float segundosCaza)
        {
            reservaDelJefe = false;
            Cazar(segundosCaza);
        }

        void Cazar(float segundos)
        {
            if (Destruido || jugador == null || reservaDelJefe) return;
            cazarHasta = Time.time + segundos;
            if (estado == Estado.Inactivo)
            {
                Despertar(true, UnityEngine.Random.Range(0.1f, 0.6f));
                perseguirAlDespertar = true;
            }
            else if (estado != Estado.Apagado) Perseguir();
        }

        /// <summary>Plano de la cinemática de alarma: el visor parpadea y el androide se enciende.</summary>
        public void DespertarEnCinematica()
        {
            if (estado != Estado.Inactivo) return;
            Despertar(true, 1.1f);
            perseguirAlDespertar = true;
        }

        void Awake()
        {
            agente = GetComponent<NavMeshAgent>();
            agente.enabled = false;   // se activa en Start, cuando la malla de navegación ya existe
            if (animator == null) animator = GetComponentInChildren<Animator>();
            bloque = new MaterialPropertyBlock();
            int idColor = Shader.PropertyToID("_Color"), idEmision = Shader.PropertyToID("_EmissionColor");
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null) continue;
                if (m.shader.name.StartsWith("Nexus/") && m.HasProperty(idColor)) brillos.Add((r, idColor, m.GetColor(idColor)));
                else if (m.IsKeywordEnabled("_EMISSION") && m.HasProperty(idEmision)) brillos.Add((r, idEmision, m.GetColor(idEmision)));
            }
        }

        void OnEnable() { Coleccionable.AlRecoger += AlRecogerObjeto; Todos.Add(this); }
        void OnDisable() { Coleccionable.AlRecoger -= AlRecogerObjeto; Todos.Remove(this); }

        // ======================= Habilidades y ruidos =======================

        /// <summary>
        /// Daño de las armas. Cada impacto lo frena un instante, enciende el visor de golpe y lo pone
        /// a perseguir al jugador (ya sabe dónde está); una unidad en reposo se reactiva. Si está
        /// desconectado por el KILLSWITCH recibe el doble. A cero se desploma y se apaga del todo.
        /// </summary>
        public void RecibirDanio(float cantidad, Vector3 punto)
        {
            if (Destruido) return;
            if (salud < 0f) salud = saludMaxima;
            if (estado == Estado.Apagado) cantidad *= 2f;
            cantidad *= MultiplicadorDanio;
            salud -= cantidad;
            if (salud <= 0f) { Destruir(); return; }

            aturdidoHasta = Time.time + 0.35f;
            destelloHasta = Time.time + 0.12f;
            if (estado == Estado.Inactivo) Despertar(true, 0.25f);
            else if (estado != Estado.Apagado && estado != Estado.Recalibrando && jugador != null) Perseguir();
        }

        void Destruir()
        {
            salud = 0f;
            estado = Estado.Destruido;
            if (agente != null && agente.enabled && agente.isOnNavMesh) { agente.ResetPath(); agente.isStopped = true; }
            if (agente != null) agente.enabled = false;
            if (linterna != null) linterna.enabled = false;
            if (animator != null) { animator.SetFloat(ParamVelocidad, 0f); animator.SetBool(ParamMuerto, true); }
            Brillo(0f);
            JugadorEnCono = false;
            // Queda en el suelo: sin colisión que estorbe ni cono de visión
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            Todos.Remove(this);
            HUDNexus.Aviso("UNIDAD SENTINEL NEUTRALIZADA");
        }

        /// <summary>KILLSWITCH: desconecta al androide unos segundos. Devuelve false si no se pudo.</summary>
        public bool Apagar(float segundos)
        {
            if (!PuedeApagarse || !agente.isOnNavMesh) return false;
            CambiarEstado(Estado.Apagado);
            finApagado = Time.time + segundos;
            visto = false;
            agente.ResetPath();
            if (animator != null) animator.SetBool(ParamMuerto, true);
            Brillo(0f);
            return true;
        }

        /// <summary>
        /// Un ruido o un señuelo de red llama la atención del androide: va a ese punto y se queda
        /// mirando alrededor durante "segundos". Si en el camino ve al jugador, lo persigue.
        /// </summary>
        public void Atraer(Vector3 punto, float segundos)
        {
            if (agente == null || !agente.enabled || !agente.isOnNavMesh) return;
            if (estado == Estado.Inactivo || estado == Estado.Apagado || estado == Estado.Recalibrando) return;
            if (estado == Estado.Persecucion && visto) return;   // si te está viendo, no se distrae
            atraidoHasta = Mathf.Max(atraidoHasta, Time.time + segundos);
            Investigar(punto);
        }

        void Brillo(float k)
        {
            foreach (var (r, propiedad, color) in brillos)
            {
                if (r == null) continue;
                if (Mathf.Abs(k - 1f) < 0.001f) { r.SetPropertyBlock(null); continue; }   // valor original
                r.GetPropertyBlock(bloque);
                bloque.SetColor(propiedad, color * k);
                r.SetPropertyBlock(bloque);
            }
        }

        void Start()
        {
            var j = GameObject.FindGameObjectWithTag("Player");
            if (j != null)
            {
                jugador = j.transform;
                vida = j.GetComponent<VidaJugador>();
                controlJugador = j.GetComponent<ControladorJugador>();
            }
            agente.enabled = true;
            agente.Warp(transform.position);
            CambiarEstado(activarConAlarma ? Estado.Inactivo : Estado.Patrulla);
            if (estado == Estado.Patrulla) IrAlPunto(0);
            else Brillo(0.04f);   // en reposo: visor y juntas casi apagados
        }

        void AlRecogerObjeto(Coleccionable c)
        {
            if (c.Tipo != TipoColeccionable.DiscoDuro || estado != Estado.Inactivo || reservaDelJefe) return;
            // La alarma despierta al androide y lo manda a donde está el jugador
            Despertar(false, 0.9f);
        }

        /// <summary>Secuencia de encendido de una unidad en reposo: el visor parpadea y luego actúa.</summary>
        void Despertar(bool perseguir, float segundos)
        {
            if (despertarEn > 0f) return;
            despertarEn = Time.time + segundos;
            perseguirAlDespertar = perseguir;
            destinoAlDespertar = jugador != null ? jugador.position : transform.position;
        }

        /// <summary>
        /// Unidad en reposo: casi apagada, con un pulso tenue cada pocos segundos. Su sensor de
        /// cercanía la reactiva si el jugador pasa pegado a ella, corre cerca o se le planta enfrente.
        /// </summary>
        void EnReposo()
        {
            if (despertarEn > 0f)
            {
                Brillo(Mathf.Repeat(Time.time, 0.2f) < 0.1f ? 1f : 0.1f);
                if (Time.time < despertarEn) return;
                despertarEn = -1f;
                Brillo(1f);
                if (perseguirAlDespertar) Perseguir();
                else Investigar(destinoAlDespertar);
                return;
            }
            float fase = Mathf.Repeat(Time.time + transform.position.x, 3.5f);
            Brillo(fase < 0.35f ? 0.04f + 0.25f * Mathf.Sin(fase / 0.35f * Mathf.PI) : 0.04f);

            if (enEstacion) return;   // anclado: solo la alarma lo despierta
            if (vida != null && vida.Muerto) return;
            float d = Vector3.Distance(transform.position, jugador.position);
            bool pegado = d < 2.2f;
            bool corriendoCerca = Oye(d);
            bool deFrente = false;
            if (d < 4.5f && JugadorEnCono && Time.time >= proximaVista)
            {
                proximaVista = Time.time + 0.2f;
                deFrente = PuedeVerAlJugador();
            }
            if (pegado || corriendoCerca || deFrente)
            {
                HUDNexus.Aviso("¡Una unidad SENTINEL en reposo se reactivó!");
                Despertar(true, 1.1f);
            }
        }

        void Update()
        {
            if (Destruido) return;
            if (jugador == null || !agente.isOnNavMesh) return;
            if (Pausados)
            {
                // Cinemática: quieto, pero la secuencia de encendido del visor sí se ve
                enPausa = true;
                agente.isStopped = true;
                if (animator != null) animator.SetFloat(ParamVelocidad, 0f, 0.1f, Time.deltaTime);
                if (estado == Estado.Inactivo && despertarEn > 0f)
                    Brillo(Time.time < despertarEn ? (Mathf.Repeat(Time.time, 0.2f) < 0.1f ? 1f : 0.1f) : 1f);
                return;
            }
            if (enPausa)
            {
                enPausa = false;
                agente.isStopped = estado == Estado.Inactivo || estado == Estado.Recalibrando || estado == Estado.Apagado;
            }
            if (Detenido && estado != Estado.Inactivo && estado != Estado.Apagado)
            {
                // Quieto, encarando al técnico (el jefe cargando su descarga)
                detenidoAntes = true;
                agente.isStopped = true;
                if (animator != null) animator.SetFloat(ParamVelocidad, 0f, 0.1f, Time.deltaTime);
                Vector3 h = jugador.position - transform.position; h.y = 0f;
                if (h.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(h), 120f * Time.deltaTime);
                return;
            }
            if (detenidoAntes) { detenidoAntes = false; agente.isStopped = estado == Estado.Recalibrando; }
            if (animator != null) animator.SetFloat(ParamVelocidad, agente.velocity.magnitude, 0.1f, Time.deltaTime);

            // Impacto reciente: el visor destella y el androide se frena un instante
            if (destelloHasta > 0f)
            {
                if (Time.time < destelloHasta) Brillo(3f);
                else { destelloHasta = 0f; Brillo(estado == Estado.Inactivo ? 0.04f : estado == Estado.Apagado ? 0f : 1f); }
            }
            if (aturdidoHasta > 0f)
            {
                if (Time.time < aturdidoHasta) { agente.isStopped = true; return; }
                aturdidoHasta = 0f;
                agente.isStopped = estado == Estado.Inactivo || estado == Estado.Recalibrando || estado == Estado.Apagado;
            }
            if (estado == Estado.Inactivo) { EnReposo(); return; }
            if (estado == Estado.Apagado) { Reiniciando(); return; }

            if (Time.time >= proximaVista)
            {
                proximaVista = Time.time + 0.12f;
                visto = PuedeVerAlJugador();
            }
            float distancia = Vector3.Distance(transform.position, jugador.position);

            switch (estado)
            {
                case Estado.Patrulla:
                    if (visto || Time.time < cazarHasta) { Perseguir(); break; }
                    if (Oye(distancia)) { Investigar(jugador.position); break; }
                    if (!agente.pathPending && agente.remainingDistance < 0.3f)
                    {
                        if (esperaHasta <= 0f) esperaHasta = Time.time + esperaEnPunto;
                        else if (Time.time >= esperaHasta) { esperaHasta = 0f; IrAlPunto(indiceRuta + 1); }
                    }
                    break;

                case Estado.Persecucion:
                    // Con la alarma, el sistema le pasa la posición del técnico aunque no lo vea
                    if (visto || Time.time < cazarHasta) { ultimaVezVisto = Time.time; ultimaPosicion = jugador.position; }
                    if (Time.time >= proximoDestino) { proximoDestino = Time.time + 0.2f; agente.SetDestination(ultimaPosicion); }
                    if (distancia < distanciaAtrapar) { Atrapar(); break; }
                    if (Time.time - ultimaVezVisto > tiempoOlvido) Investigar(ultimaPosicion);
                    break;

                case Estado.Busqueda:
                    if (visto || Time.time < cazarHasta) { Perseguir(); break; }
                    if (!agente.pathPending && agente.remainingDistance < 0.4f)
                    {
                        transform.Rotate(0f, 90f * Time.deltaTime, 0f);   // mira alrededor
                        if (Time.time < atraidoHasta) { finBusqueda = 0f; break; }   // sigue pendiente del señuelo
                        if (finBusqueda <= 0f) finBusqueda = Time.time + tiempoBusqueda;
                        else if (Time.time >= finBusqueda) { finBusqueda = 0f; Regresar(); }
                    }
                    break;

                case Estado.Recalibrando:
                    if (Time.time >= finRecalibrar) Regresar();
                    break;
            }
            ActualizarLinterna();
        }

        /// <summary>Apagado: al final el visor parpadea, se levanta y vuelve a su ruta.</summary>
        void Reiniciando()
        {
            float resta = finApagado - Time.time;
            if (resta < 1.6f) Brillo(Mathf.Repeat(Time.time, 0.26f) < 0.13f ? 1f : 0.08f);
            if (resta < 1.0f && animator != null && animator.GetBool(ParamMuerto)) animator.SetBool(ParamMuerto, false);
            if (resta > 0f) return;
            Brillo(1f);
            atraidoHasta = 0f;
            Regresar();
        }

        bool PuedeVerAlJugador()
        {
            if (vida != null && vida.Muerto) return false;
            float d = Vector3.Distance(transform.position, jugador.position);
            if (!JugadorEnCono && d > 1.6f) return false;   // muy cerca lo "siente" aunque esté detrás
            Vector3 origen = ojos.position + transform.forward * 0.4f;
            Vector3 objetivo = jugador.position + Vector3.up * 1.2f;
            Vector3 dir = objetivo - origen;
            if (Physics.Raycast(origen, dir.normalized, out RaycastHit hit, dir.magnitude + 0.5f, MascaraVision, QueryTriggerInteraction.Ignore))
                return hit.collider.CompareTag("Player");
            return true;
        }

        bool Oye(float distancia) =>
            controlJugador != null && distancia < radioOido && controlJugador.VelocidadActual > 3f;

        void IrAlPunto(int i)
        {
            if (ruta == null || ruta.Length == 0) return;
            indiceRuta = (i + ruta.Length) % ruta.Length;
            agente.SetDestination(ruta[indiceRuta]);
        }

        void Perseguir()
        {
            bool nuevo = estado != Estado.Persecucion;
            CambiarEstado(Estado.Persecucion);
            ultimaVezVisto = Time.time;
            ultimaPosicion = jugador.position;
            if (nuevo)
            {
                AlDetectar?.Invoke(this);
                if (audioAlarma != null && audioAlarma.clip != null) audioAlarma.Play();
                if (Time.time - ultimoAvisoDeteccion > 6f)
                {
                    ultimoAvisoDeteccion = Time.time;
                    HUDNexus.Aviso("¡UN ANDROIDE TE DETECTÓ!");
                }
            }
        }

        void Investigar(Vector3 punto)
        {
            CambiarEstado(Estado.Busqueda);
            finBusqueda = 0f;
            agente.SetDestination(punto);
        }

        void Atrapar()
        {
            if (vida != null) vida.RecibirDanio(transform.position);
            CambiarEstado(Estado.Recalibrando);
            finRecalibrar = Time.time + tiempoRecalibrar;
            agente.ResetPath();
        }

        void Regresar()
        {
            CambiarEstado(Estado.Patrulla);
            // Vuelve al punto de ruta más cercano
            int mejor = 0; float mejorD = float.MaxValue;
            for (int i = 0; ruta != null && i < ruta.Length; i++)
            {
                float d = (ruta[i] - transform.position).sqrMagnitude;
                if (d < mejorD) { mejorD = d; mejor = i; }
            }
            IrAlPunto(mejor);
        }

        void CambiarEstado(Estado nuevo)
        {
            estado = nuevo;
            agente.speed = nuevo == Estado.Persecucion ? velocidadPersecucion
                         : nuevo == Estado.Busqueda ? velocidadPatrulla * 1.6f
                         : velocidadPatrulla;
            agente.isStopped = nuevo == Estado.Inactivo || nuevo == Estado.Recalibrando || nuevo == Estado.Apagado;
            if (linterna != null) linterna.enabled = nuevo != Estado.Inactivo && nuevo != Estado.Apagado;
        }

        void ActualizarLinterna()
        {
            if (linterna == null) return;
            float intensidad = estado == Estado.Persecucion ? 14f + 4f * Mathf.Sin(Time.time * 18f)
                             : estado == Estado.Busqueda ? 10f
                             : estado == Estado.Recalibrando ? (Mathf.Repeat(Time.time, 0.3f) < 0.15f ? 2f : 9f)
                             : 7f;
            linterna.intensity = intensidad;
        }

        void OnDrawGizmosSelected()
        {
            if (ruta == null) return;
            Gizmos.color = Color.red;
            for (int i = 0; i < ruta.Length; i++)
            {
                Gizmos.DrawSphere(ruta[i] + Vector3.up * 0.1f, 0.2f);
                Gizmos.DrawLine(ruta[i] + Vector3.up * 0.1f, ruta[(i + 1) % ruta.Length] + Vector3.up * 0.1f);
            }
        }
    }
}
