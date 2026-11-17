using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus
{
    public enum TipoArma { Ninguna = 0, Cuchillo = 1, Pistola = 2, Escopeta = 3 }

    /// <summary>
    /// Armas del técnico, al estilo de los survival horror clásicos:
    ///  - 1 / 2 / 3 elige cuchillo, pistola o escopeta (0 las guarda); también desde el inventario.
    ///  - Clic derecho o K (LT en el mando): APUNTAR. El técnico se detiene, levanta el arma y gira
    ///    solo hacia el androide más cercano que tenga enfrente y a la vista (apuntado automático,
    ///    pensado también para el celular). Con A / D se puede girar a mano.
    ///  - Clic izquierdo o J (RT): disparar mientras apunta, o acuchillar con el cuchillo.
    ///  - R (RB): recargar desde la munición del inventario.
    /// Disparar hace ruido: los androides cercanos van a ver qué pasó. El cuchillo es silencioso.
    /// </summary>
    public class ArmaJugador : MonoBehaviour
    {
        [SerializeField] DatosArmas datos;

        struct Estadisticas
        {
            public float dano, alcance, cadencia, dispersion, ruido, recarga;
            public int perdigones, cargador;
            public TipoColeccionable municion;
        }

        static Estadisticas De(TipoArma t) => t switch
        {
            TipoArma.Pistola => new Estadisticas { dano = 2f, alcance = 25f, cadencia = 0.32f, dispersion = 1.2f, ruido = 18f, recarga = 1.3f, perdigones = 1, cargador = 15, municion = TipoColeccionable.BalasPistola },
            TipoArma.Escopeta => new Estadisticas { dano = 1.6f, alcance = 14f, cadencia = 1.0f, dispersion = 7f, ruido = 24f, recarga = 2.0f, perdigones = 7, cargador = 6, municion = TipoColeccionable.CartuchosEscopeta },
            TipoArma.Cuchillo => new Estadisticas { dano = 2.5f, alcance = 1.9f, cadencia = 0.75f, dispersion = 60f, ruido = 0f },
            _ => default
        };

        static readonly int ParamArmaFuego = Animator.StringToHash("ArmaFuego");
        static readonly int ParamApuntando = Animator.StringToHash("Apuntando");
        static readonly int ParamDisparo = Animator.StringToHash("Disparo");
        static readonly int ParamAcuchillar = Animator.StringToHash("Acuchillar");
        const int MascaraDisparo = ~(1 << 2);

        Animator animator;
        ControladorJugador control;
        VidaJugador vida;
        InventarioJugador inventario;
        Transform mano;
        GameObject enMano;
        Transform boca;
        LineRenderer laser;
        float proximoAtaque, finRecarga = -1f, atacandoHasta;
        AndroideIA objetivo;
        bool girandoAMano;

        public TipoArma Equipada { get; private set; }
        public bool Apuntando { get; private set; }
        public bool Recargando => finRecarga > 0f;
        // Para las animaciones de la escopeta (IKTecnico): retroceso, bombeo y recarga cartucho a cartucho
        public float UltimoDisparo { get; private set; } = -10f;
        public float InicioRecarga { get; private set; }
        public int CartuchosARecargar { get; private set; }
        public Transform ArmaEnMano => enMano != null ? enMano.transform : null;
        public DatosArmas Datos => datos;
        public const float SegundosPorCartucho = 0.5f;
        static readonly int ParamRecargando = Animator.StringToHash("Recargando");
        int capaEscopeta = -1;
        bool tieneParamRecarga;

        // Para el botón táctil y las pruebas automáticas
        public bool ApuntarSimulado { get; set; }
        public bool DisparoSimulado { get; set; }
        public bool RecargaSimulada { get; set; }

        public static TipoArma DesdeObjeto(TipoColeccionable t) => t switch
        {
            TipoColeccionable.Cuchillo => TipoArma.Cuchillo,
            TipoColeccionable.Pistola => TipoArma.Pistola,
            TipoColeccionable.Escopeta => TipoArma.Escopeta,
            _ => TipoArma.Ninguna
        };

        static TipoColeccionable ObjetoDe(TipoArma a) => a switch
        {
            TipoArma.Cuchillo => TipoColeccionable.Cuchillo,
            TipoArma.Pistola => TipoColeccionable.Pistola,
            _ => TipoColeccionable.Escopeta
        };

        static string NombreCorto(TipoArma a) => a switch
        {
            TipoArma.Cuchillo => "CUCHILLO", TipoArma.Pistola => "PISTOLA", TipoArma.Escopeta => "ESCOPETA", _ => ""
        };

        int Cargador
        {
            get => Equipada == TipoArma.Pistola ? EstadoPartida.CargadorPistola : Equipada == TipoArma.Escopeta ? EstadoPartida.CargadorEscopeta : 0;
            set { if (Equipada == TipoArma.Pistola) EstadoPartida.CargadorPistola = value; else if (Equipada == TipoArma.Escopeta) EstadoPartida.CargadorEscopeta = value; }
        }

        void Awake()
        {
            control = GetComponent<ControladorJugador>();
            vida = GetComponent<VidaJugador>();
            inventario = GetComponent<InventarioJugador>();
        }

        void Start()
        {
            animator = GetComponentInChildren<Animator>();
            mano = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            if (animator != null)
            {
                // Manos sobre la escopeta, mirada en las cinemáticas (cinemática inversa)
                if (animator.GetComponent<IKTecnico>() == null) animator.gameObject.AddComponent<IKTecnico>();
                capaEscopeta = animator.GetLayerIndex("Escopeta");
                foreach (var p in animator.parameters) if (p.nameHash == ParamRecargando) tieneParamRecarga = true;
            }
            var tipo = (TipoArma)EstadoPartida.ArmaEquipada;
            if (tipo != TipoArma.Ninguna && !TieneArma(tipo)) tipo = TipoArma.Ninguna;
            Equipar(tipo, true);
        }

        bool TieneArma(TipoArma a) => a != TipoArma.Ninguna && inventario != null && inventario.Tiene(ObjetoDe(a));

        /// <summary>Al recoger un arma: si no lleva una de fuego en la mano, se la equipa.</summary>
        public void AlRecoger(TipoColeccionable t)
        {
            var a = DesdeObjeto(t);
            if (a == TipoArma.Ninguna) { ActualizarHUD(); return; }
            if (Equipada == TipoArma.Ninguna || (Equipada == TipoArma.Cuchillo && a != TipoArma.Cuchillo)) Equipar(a);
            else ActualizarHUD();
        }

        // ======================= Equipar =======================
        public void Equipar(TipoArma a, bool silencioso = false)
        {
            if (a != TipoArma.Ninguna && !TieneArma(a)) { if (!silencioso) HUDNexus.Aviso("No tienes esa arma"); return; }
            Equipada = a;
            EstadoPartida.ArmaEquipada = (int)a;
            finRecarga = -1f;
            if (enMano != null) Destroy(enMano);
            enMano = null; boca = null;
            var prefab = a == TipoArma.Cuchillo ? datos?.cuchillo : a == TipoArma.Pistola ? datos?.pistola : a == TipoArma.Escopeta ? datos?.escopeta : null;
            if (prefab != null && mano != null)
            {
                enMano = Instantiate(prefab, mano);
                enMano.transform.localPosition = datos.posicionEnMano;
                enMano.transform.localRotation = Quaternion.Euler(datos.rotacionEnMano);
                // El hueso de la mano puede venir escalado desde el modelo: el arma conserva su tamaño real
                var s = mano.lossyScale;
                enMano.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
                boca = enMano.transform.Find("Boca");
                foreach (var c in enMano.GetComponentsInChildren<Collider>()) c.enabled = false;
            }
            if (animator != null) animator.SetBool(ParamArmaFuego, a == TipoArma.Pistola || a == TipoArma.Escopeta);
            // Primera vez con un arma de fuego: el cargador viene lleno si hay munición
            if ((a == TipoArma.Pistola || a == TipoArma.Escopeta) && Cargador == 0 && Reserva > 0) CompletarRecarga();
            if (!silencioso && a != TipoArma.Ninguna) HUDNexus.Aviso($"Equipaste: {DatosObjetos.Nombre(ObjetoDe(a))}");
            ActualizarHUD();
        }

        int Reserva => Equipada == TipoArma.Pistola || Equipada == TipoArma.Escopeta ? inventario.Cantidad(De(Equipada).municion) : 0;

        // ======================= Ciclo =======================
        void Update()
        {
            bool pausa = Time.timeScale <= 0f || HUDNexus.DialogoAbierto || (vida != null && vida.Muerto) || Cinematica.EnCurso;
            var k = Keyboard.current; var m = Mouse.current; var g = Gamepad.current;

            if (!pausa)
            {
                if (k != null)
                {
                    if (k.digit1Key.wasPressedThisFrame) Equipar(TipoArma.Cuchillo);
                    if (k.digit2Key.wasPressedThisFrame) Equipar(TipoArma.Pistola);
                    if (k.digit3Key.wasPressedThisFrame) Equipar(TipoArma.Escopeta);
                    if (k.digit0Key.wasPressedThisFrame) Equipar(TipoArma.Ninguna);
                }
                if (g != null && (g.dpad.right.wasPressedThisFrame || g.dpad.left.wasPressedThisFrame)) Ciclar(g.dpad.right.wasPressedThisFrame ? 1 : -1);
            }

            bool apuntar = ApuntarSimulado || (m != null && m.rightButton.isPressed) || (k != null && k.kKey.isPressed) || (g != null && g.leftTrigger.isPressed);
            bool disparar = DisparoSimulado || (m != null && m.leftButton.wasPressedThisFrame) || (k != null && k.jKey.wasPressedThisFrame) || (g != null && g.rightTrigger.wasPressedThisFrame);
            bool recargar = RecargaSimulada || (k != null && k.rKey.wasPressedThisFrame) || (g != null && g.rightShoulder.wasPressedThisFrame);
            DisparoSimulado = false; RecargaSimulada = false;

            bool deFuego = Equipada == TipoArma.Pistola || Equipada == TipoArma.Escopeta;
            bool nuevoApuntando = !pausa && deFuego && apuntar && !Recargando;
            if (nuevoApuntando && !Apuntando) { objetivo = BuscarObjetivo(); girandoAMano = false; }
            Apuntando = nuevoApuntando;
            if (control != null) control.Apuntando = Apuntando || Time.time < atacandoHasta || Recargando;
            if (animator != null)
            {
                animator.SetBool(ParamApuntando, Apuntando);
                if (tieneParamRecarga) animator.SetBool(ParamRecargando, Recargando);
                // Capa de la escopeta (brazos y cabeza): entra suave al equiparla
                if (capaEscopeta >= 0)
                    animator.SetLayerWeight(capaEscopeta, Mathf.MoveTowards(animator.GetLayerWeight(capaEscopeta), Equipada == TipoArma.Escopeta ? 1f : 0f, Time.deltaTime * 6f));
            }

            if (Apuntando) Apuntar();
            ActualizarLaser();

            if (Recargando && Time.time >= finRecarga) { CompletarRecarga(); finRecarga = -1f; ActualizarHUD(); }
            if (pausa) return;

            if (disparar)
            {
                if (Equipada == TipoArma.Cuchillo) Acuchillar();
                else if (Apuntando) Disparar();
            }
            if (recargar) Recargar();
        }

        void Ciclar(int paso)
        {
            for (int i = 1; i <= 4; i++)
            {
                var a = (TipoArma)(((int)Equipada + paso * i + 8) % 4);
                if (a == TipoArma.Ninguna || TieneArma(a)) { Equipar(a); return; }
            }
        }

        // ======================= Apuntar =======================
        AndroideIA BuscarObjetivo()
        {
            var st = De(Equipada);
            AndroideIA mejor = null;
            float mejorDist = st.alcance;
            Vector3 ojos = transform.position + Vector3.up * 1.35f;
            foreach (var a in AndroideIA.Todos)
            {
                if (a == null || a.Destruido) continue;
                Vector3 hacia = a.transform.position - transform.position; hacia.y = 0f;
                float d = hacia.magnitude;
                if (d > mejorDist || Vector3.Angle(transform.forward, hacia) > 50f) continue;
                Vector3 dir = a.Pecho - ojos;
                if (Physics.Raycast(ojos, dir.normalized, out RaycastHit hit, dir.magnitude, MascaraDisparo, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<AndroideIA>() != a) continue;
                mejor = a; mejorDist = d;
            }
            return mejor;
        }

        void Apuntar()
        {
            // Si el jugador gira a mano (A / D), suelta el blanco automático
            if (control != null && Mathf.Abs(control.EntradaGiro) > 0.3f) girandoAMano = true;
            if (objetivo != null && (objetivo.Destruido || girandoAMano)) objetivo = null;
            if (objetivo == null) return;
            Vector3 hacia = objetivo.transform.position - transform.position; hacia.y = 0f;
            if (hacia.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(hacia), 540f * Time.deltaTime);
        }

        Vector3 OrigenDisparo => boca != null ? boca.position : transform.position + Vector3.up * 1.4f + transform.forward * 0.5f;

        Vector3 DireccionDisparo(Vector3 origen)
        {
            if (objetivo != null && !objetivo.Destruido) return (objetivo.Pecho - origen).normalized;
            Vector3 f = transform.forward; f.y = 0f;
            return f.normalized;
        }

        void ActualizarLaser()
        {
            bool mostrar = Apuntando && boca != null && datos != null && datos.laser != null;
            if (mostrar && laser == null)
            {
                laser = new GameObject("Mira_Laser").AddComponent<LineRenderer>();
                laser.sharedMaterial = datos.laser;
                laser.positionCount = 2; laser.widthMultiplier = 0.008f;
                laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (laser == null) return;
            laser.enabled = mostrar;
            if (!mostrar) return;
            // Termina donde pegaría la bala (el rayo del disparo sale del pecho)
            Vector3 desde = transform.position + Vector3.up * 1.35f, d = DireccionDisparo(desde);
            float alcance = De(Equipada).alcance;
            Vector3 fin = Physics.Raycast(desde, d, out RaycastHit hit, alcance, MascaraDisparo, QueryTriggerInteraction.Ignore) ? hit.point : desde + d * alcance;
            laser.SetPosition(0, boca.position); laser.SetPosition(1, fin);
        }

        // ======================= Disparar =======================
        void Disparar()
        {
            if (Time.time < proximoAtaque || Recargando) return;
            var st = De(Equipada);
            if (Cargador <= 0)
            {
                if (Reserva > 0) Recargar();
                else HUDNexus.Aviso("Sin munición");
                proximoAtaque = Time.time + 0.4f;
                return;
            }
            Cargador--;
            UltimoDisparo = Time.time;
            proximoAtaque = Time.time + st.cadencia;
            if (animator != null) animator.SetTrigger(ParamDisparo);

            Vector3 origen = OrigenDisparo;
            // El rayo sale desde el pecho (no atraviesa paredes por estar el cañón dentro de una) y la
            // dirección se calcula desde ahí mismo: si se calculara desde el cañón, que está a un lado,
            // el rayo pasaría paralelo junto al androide sin tocarlo
            Vector3 desde = transform.position + Vector3.up * 1.35f;
            Vector3 baseDir = DireccionDisparo(desde);
            int aciertos = 0;
            for (int i = 0; i < st.perdigones; i++)
            {
                Vector3 dir = Quaternion.AngleAxis(Random.Range(-st.dispersion, st.dispersion), Vector3.up)
                            * Quaternion.AngleAxis(Random.Range(-st.dispersion, st.dispersion) * 0.5f, transform.right) * baseDir;
                Vector3 fin;
                if (Physics.Raycast(desde, dir, out RaycastHit hit, st.alcance, MascaraDisparo, QueryTriggerInteraction.Ignore))
                {
                    fin = hit.point;
                    var a = hit.collider.GetComponentInParent<AndroideIA>();
                    if (a != null)
                    {
                        // La escopeta pierde fuerza con la distancia
                        float caida = Equipada == TipoArma.Escopeta ? Mathf.Lerp(1.3f, 0.35f, hit.distance / st.alcance) : 1f;
                        a.RecibirDanio(st.dano * caida, hit.point);
                        aciertos++;
                    }
                    Impacto(hit.point, hit.normal);
                }
                else fin = desde + dir * st.alcance;
                StartCoroutine(Trazadora(origen, fin));
            }
            StartCoroutine(Fogonazo(origen));
            // Ruido: los androides cercanos vienen a ver
            foreach (var a in AndroideIA.Todos)
                if (a != null && !a.Destruido && (a.transform.position - transform.position).sqrMagnitude < st.ruido * st.ruido)
                    a.Atraer(transform.position, 3f);
            ActualizarHUD();
        }

        void Acuchillar()
        {
            if (Time.time < proximoAtaque) return;
            var st = De(TipoArma.Cuchillo);
            proximoAtaque = Time.time + st.cadencia;
            atacandoHasta = Time.time + 0.55f;
            // Si hay un androide cerca enfrente, se gira hacia él
            AndroideIA cerca = null; float mejor = 2.6f;
            foreach (var a in AndroideIA.Todos)
            {
                if (a == null || a.Destruido) continue;
                Vector3 h = a.transform.position - transform.position; h.y = 0f;
                if (h.magnitude < mejor && Vector3.Angle(transform.forward, h) < 75f) { mejor = h.magnitude; cerca = a; }
            }
            if (cerca != null)
            {
                Vector3 h = cerca.transform.position - transform.position; h.y = 0f;
                transform.rotation = Quaternion.LookRotation(h);
            }
            if (animator != null) animator.SetTrigger(ParamAcuchillar);
            StartCoroutine(GolpeCuchillo(st));
        }

        IEnumerator GolpeCuchillo(Estadisticas st)
        {
            yield return new WaitForSeconds(0.28f);   // el momento del tajo en la animación
            foreach (var a in AndroideIA.Todos.ToArray())
            {
                if (a == null || a.Destruido) continue;
                Vector3 h = a.transform.position - transform.position; h.y = 0f;
                if (h.magnitude <= st.alcance && Vector3.Angle(transform.forward, h) <= st.dispersion)
                {
                    a.RecibirDanio(st.dano, a.Pecho - h.normalized * 0.25f);
                    Impacto(a.Pecho - h.normalized * 0.3f, -h.normalized);
                }
            }
        }

        // ======================= Recargar =======================
        void Recargar()
        {
            if (Equipada != TipoArma.Pistola && Equipada != TipoArma.Escopeta) return;
            var st = De(Equipada);
            if (Recargando || Cargador >= st.cargador) return;
            if (Reserva <= 0) { HUDNexus.Aviso("No te queda munición para esta arma"); return; }
            InicioRecarga = Time.time;
            CartuchosARecargar = Mathf.Min(st.cargador - Cargador, Reserva);
            // La escopeta de corredera se carga cartucho a cartucho por la ventana inferior
            float duracion = Equipada == TipoArma.Escopeta ? 0.3f + SegundosPorCartucho * CartuchosARecargar : st.recarga;
            finRecarga = Time.time + duracion;
            ActualizarHUD();
        }

        void CompletarRecarga()
        {
            var st = De(Equipada);
            int falta = st.cargador - Cargador;
            int toma = Mathf.Min(falta, Reserva);
            if (toma <= 0) return;
            inventario.Consumir(st.municion, toma);
            Cargador += toma;
        }

        // ======================= Efectos =======================
        void Impacto(Vector3 punto, Vector3 normal)
        {
            if (datos == null || datos.impacto == null) return;
            var fx = Instantiate(datos.impacto, punto + normal * 0.03f, Quaternion.LookRotation(normal));
            fx.transform.localScale = Vector3.one * 0.6f;
            Destroy(fx, 1.5f);
        }

        IEnumerator Trazadora(Vector3 a, Vector3 b)
        {
            if (datos == null || datos.trazadora == null) yield break;
            var lr = new GameObject("Trazadora").AddComponent<LineRenderer>();
            lr.sharedMaterial = datos.trazadora;
            lr.positionCount = 2; lr.SetPosition(0, a); lr.SetPosition(1, b);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (float t = 0f; t < 0.08f; t += Time.deltaTime)
            {
                lr.widthMultiplier = Mathf.Lerp(0.025f, 0.004f, t / 0.08f);
                yield return null;
            }
            Destroy(lr.gameObject);
        }

        IEnumerator Fogonazo(Vector3 punto)
        {
            var luz = new GameObject("Fogonazo").AddComponent<Light>();
            luz.transform.position = punto;
            luz.type = LightType.Point; luz.range = Equipada == TipoArma.Escopeta ? 7f : 5f;
            luz.color = new Color(1f, 0.75f, 0.35f);
            luz.intensity = Equipada == TipoArma.Escopeta ? 9f : 6f;
            luz.shadows = LightShadows.None;
            yield return new WaitForSeconds(0.06f);
            Destroy(luz.gameObject);
        }

        // ======================= HUD =======================
        void ActualizarHUD()
        {
            if (Equipada == TipoArma.Ninguna) { HUDNexus.Arma(TieneAlgunaArma() ? "1 · 2 · 3  ARMAS" : ""); return; }
            if (Equipada == TipoArma.Cuchillo) { HUDNexus.Arma("CUCHILLO"); return; }
            string estado = Recargando ? "  ·  RECARGANDO" : "";
            HUDNexus.Arma($"{NombreCorto(Equipada)}  {Cargador} / {Reserva}{estado}");
        }

        bool TieneAlgunaArma() => TieneArma(TipoArma.Cuchillo) || TieneArma(TipoArma.Pistola) || TieneArma(TipoArma.Escopeta);

        void OnDestroy()
        {
            if (laser != null) Destroy(laser.gameObject);
        }
    }
}
