using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Cámaras fijas estilo survival horror clásico: todas las cámaras del nivel son hijas de este
    /// objeto y solo una está encendida a la vez (renderizar con una sola cámara también es más
    /// barato en móvil y WebGL).
    ///
    /// Cada sala tiene una cámara principal y varios ángulos alternativos (<see cref="CamaraFija"/>).
    ///  - Al entrar a una sala (<see cref="ZonaCamara"/> del suelo) se elige el ángulo de esa sala que
    ///    mejor encuadra al jugador.
    ///  - Mientras tanto se vigila el encuadre: en cuanto el jugador se sale del cuadro (o algo lo
    ///    tapa por un momento), se cambia al ángulo que lo vea mejor. Así el personaje nunca
    ///    camina fuera de la pantalla.
    /// </summary>
    public class GestorCamaras : MonoBehaviour
    {
        public static GestorCamaras Instancia { get; private set; }
        public static event Action<Camera> AlCambiarCamara;

        [SerializeField] Camera camaraInicial;
        [Tooltip("Margen del borde de la pantalla (0-0,5): si el jugador lo cruza, se cambia de cámara")]
        [SerializeField] float margen = 0.05f;
        [Tooltip("Segundos que el jugador puede estar tapado (detrás de un rack) antes de cambiar")]
        [SerializeField] float toleranciaOculto = 0.3f;

        const int MascaraVista = ~(1 << 2);   // ignora triggers ("Ignore Raycast")

        readonly List<CamaraFija> camaras = new List<CamaraFija>();
        CamaraFija activa;
        Transform jugador;
        string salaActual;
        float proximaRevision, ocultoDesde = -1f, ultimoCambio;

        public Camera Activa { get; private set; }
        public string SalaActual => salaActual;

        void Awake()
        {
            Instancia = this;
            foreach (var c in GetComponentsInChildren<Camera>(true))
            {
                var cf = c.GetComponent<CamaraFija>();
                if (cf == null) { cf = c.gameObject.AddComponent<CamaraFija>(); cf.Principal = true; }
                camaras.Add(cf);
                c.enabled = false;
            }
            Activar(camaraInicial != null ? camaraInicial : (camaras.Count > 0 ? camaras[0].Camara : null));
            if (activa != null) salaActual = activa.Sala;
        }

        void Start()
        {
            var j = GameObject.FindGameObjectWithTag("Player");
            if (j != null) jugador = j.transform;
            Revisar(true);
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        /// <summary>El jugador pisó una zona de otra sala: se usa el mejor ángulo de esa sala.</summary>
        public void EntrarSala(string sala, Camera principal)
        {
            if (string.IsNullOrEmpty(sala)) { Activar(principal); return; }
            if (sala == salaActual) return;
            salaActual = sala;
            var interes = MejorInteres();
            if (interes != null) { Activar(interes.Camara); return; }
            var mejor = Mejor(sala);
            Activar(mejor != null ? mejor.Camara : principal);
        }

        public void Activar(Camera camara)
        {
            if (camara == null || camara == Activa) return;
            if (Activa != null) Activa.enabled = false;
            Activa = camara;
            activa = camara.GetComponent<CamaraFija>();
            Activa.enabled = true;
            ultimoCambio = Time.unscaledTime;
            ocultoDesde = -1f;
            AlCambiarCamara?.Invoke(camara);
        }

        void LateUpdate()
        {
            if (Time.unscaledTime < proximaRevision) return;
            proximaRevision = Time.unscaledTime + 0.06f;
            Revisar(false);
        }

        void Revisar(bool inmediato)
        {
            if (jugador == null || Activa == null) return;

            // 1) Cerca de algo importante: plano cercano de ese objeto (la cámara como guía)
            var interes = MejorInteres();
            if (interes != null)
            {
                if (interes != activa) Activar(interes.Camara);
                ocultoDesde = -1f;
                return;
            }
            if (activa != null && activa.Interes)
            {
                // Se alejó del objeto: vuelve al mejor ángulo de la sala
                var normal = Mejor(salaActual) ?? Mejor(null);
                if (normal != null) Activar(normal.Camara);
                return;
            }

            // 2) Ángulos de la sala: cambiar si el jugador se sale del cuadro o queda mal encuadrado
            var (dentro, visible, puntajeActual) = Evaluar(Activa, jugador.position);
            if (dentro && visible)
            {
                ocultoDesde = -1f;
                // Sigue en cuadro, pero si quedó lejos o en una esquina y otro ángulo de la sala lo
                // encuadra claramente mejor, se cambia (como en los survival horror clásicos)
                if (!inmediato && Time.unscaledTime - ultimoCambio > 1f)
                {
                    float actual = puntajeActual + (activa != null && activa.Principal ? 0.15f : 0f) + 0.1f;
                    var otra = Mejor(salaActual, out float puntajeOtra);
                    if (otra != null && otra != activa && puntajeOtra > actual + 0.45f) Activar(otra.Camara);
                }
                return;
            }

            if (dentro && !visible && !inmediato)
            {
                // Tapado (p. ej. pasa detrás de una fila de racks): se espera un instante
                if (ocultoDesde < 0f) ocultoDesde = Time.unscaledTime;
                if (Time.unscaledTime - ocultoDesde < toleranciaOculto) return;
            }
            if (!inmediato && dentro && Time.unscaledTime - ultimoCambio < 0.35f) return;

            var mejor = Mejor(salaActual) ?? Mejor(null);
            if (mejor != null && mejor != activa) Activar(mejor.Camara);
        }

        /// <summary>La cámara (de la sala dada, o cualquiera cercana si es null) que mejor encuadra al jugador.</summary>
        CamaraFija Mejor(string sala) => Mejor(sala, out _);

        CamaraFija Mejor(string sala, out float mejorPuntaje)
        {
            CamaraFija mejor = null;
            mejorPuntaje = float.MinValue;
            foreach (var c in camaras)
            {
                if (c.Interes) continue;   // los planos de interés solo se usan cerca de su objeto
                if (sala != null && c.Sala != sala) continue;
                if (sala == null && (c.transform.position - jugador.position).sqrMagnitude > 20f * 20f) continue;
                var (dentro, visible, puntaje) = Evaluar(c.Camara, jugador.position);
                if (!dentro || !visible) continue;
                if (c.Principal) puntaje += 0.15f;        // a igualdad, el ángulo principal de la sala
                if (c == activa) puntaje += 0.1f;          // evita saltos innecesarios
                if (puntaje > mejorPuntaje) { mejorPuntaje = puntaje; mejor = c; }
            }
            return mejor;
        }

        /// <summary>
        /// Plano de interés más cercano: el jugador está dentro de su radio (un poco más amplio si
        /// ya es el activo, para que no parpadee en el borde) y la cámara lo ve.
        /// </summary>
        CamaraFija MejorInteres()
        {
            CamaraFija mejor = null;
            float mejorDist = float.MaxValue;
            foreach (var c in camaras)
            {
                if (!c.InteresVigente) continue;
                float radio = c == activa ? c.Radio + 0.7f : c.Radio;
                Vector3 d = jugador.position - c.Punto; d.y = 0f;
                float dist = d.magnitude;
                if (dist > radio || dist >= mejorDist) continue;
                // En un plano cercano basta con ver al técnico de las rodillas a la cabeza
                var (dentro, visible, _) = Evaluar(c.Camara, jugador.position, 0.02f, 0.5f);
                if (!dentro || !visible) continue;
                mejor = c; mejorDist = dist;
            }
            return mejor;
        }

        /// <summary>
        /// ¿El jugador (de los pies a la cabeza) entra en el cuadro de la cámara y nada lo tapa?
        /// El puntaje premia que quede cerca del centro y a una distancia cómoda.
        /// </summary>
        public static (bool dentro, bool visible, float puntaje) Evaluar(Camera cam, Vector3 posJugador, float margen = 0.05f, float alturaInferior = 0.15f)
        {
            Vector3 pies = posJugador + Vector3.up * alturaInferior, pecho = posJugador + Vector3.up * 1.1f, cabeza = posJugador + Vector3.up * 1.7f;
            foreach (var p in new[] { pies, cabeza })
            {
                var v = cam.WorldToViewportPoint(p);
                if (v.z < cam.nearClipPlane || v.x < margen || v.x > 1f - margen || v.y < margen || v.y > 1f - margen)
                    return (false, false, 0f);
            }
            Vector3 origen = cam.transform.position, dir = pecho - origen;
            bool visible = !Physics.Raycast(origen, dir.normalized, out RaycastHit hit, dir.magnitude - 0.35f, MascaraVista, QueryTriggerInteraction.Ignore)
                           || hit.collider.CompareTag("Player");
            var vc = cam.WorldToViewportPoint(pecho);
            float centrado = 1f - Vector2.Distance(new Vector2(vc.x, vc.y), new Vector2(0.5f, 0.45f)) * 1.6f;
            float d = dir.magnitude;
            // Distancia cómoda: entre 2,5 y 9 m; más lejos el personaje se ve cada vez más pequeño
            float porDistancia = d < 2.5f ? -0.6f : d > 9f ? -(d - 9f) * 0.12f : 0f;
            return (true, visible, centrado + porDistancia);
        }
    }
}
