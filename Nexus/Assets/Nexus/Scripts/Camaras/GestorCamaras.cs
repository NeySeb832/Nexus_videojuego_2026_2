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
    /// Cada sala tiene su cámara principal y, solo si hacen falta, ángulos alternativos para los
    /// rincones que la principal no ve (<see cref="CamaraFija"/>). Reglas de cambio, pensadas para
    /// que la imagen no salte:
    ///  - Al entrar a una sala se usa el ángulo de esa sala que mejor encuadra al jugador.
    ///  - Dentro de la sala la cámara NO cambia mientras el jugador siga en cuadro. Solo cambia si se
    ///    sale de la imagen o si algo lo tapa durante más de medio segundo; el ángulo nuevo debe
    ///    tenerlo bien adentro del cuadro, para no volver a perderlo enseguida.
    ///  - Plano de interés (guía): al acercarse a un objeto importante MIRÁNDOLO, si la cámara de la
    ///    sala no lo muestra ya de cerca, se corta a su plano cercano. Cada corte dura un mínimo.
    /// </summary>
    public class GestorCamaras : MonoBehaviour
    {
        public static GestorCamaras Instancia { get; private set; }
        public static event Action<Camera> AlCambiarCamara;

        [SerializeField] Camera camaraInicial;
        [Tooltip("Segundos que el jugador puede estar tapado (detrás de un rack) antes de cambiar")]
        [SerializeField] float toleranciaOculto = 0.6f;
        [Tooltip("Tiempo mínimo en un plano de interés o al volver de él, antes de otro corte")]
        [SerializeField] float duracionMinimaPlano = 1.2f;
        [Tooltip("Si la cámara de la sala está a menos de esta distancia del objeto, ya se ve bien: no hace falta plano cercano")]
        [SerializeField] float distanciaYaCercana = 5.5f;

        const int MascaraVista = ~(1 << 2);   // ignora triggers ("Ignore Raycast")
        const float MargenSalida = 0.02f;      // se considera fuera de cuadro al cruzar este borde
        const float MargenEntrada = 0.10f;     // el ángulo nuevo debe tenerlo así de adentro
        /// <summary>
        /// Altura mínima del técnico en pantalla (fracción del alto de la imagen, de los pies a la
        /// cabeza). Por debajo de esto se ve demasiado lejos y cuesta controlarlo: se busca otro
        /// ángulo. El nuevo debe mostrarlo más grande (TamanoEntrada) para no ir y volver.
        /// El constructor de niveles usa TamanoEntrada para decidir cuántas cámaras pone.
        /// </summary>
        public const float TamanoSalida = 0.09f, TamanoEntrada = 0.11f;

        readonly List<CamaraFija> camaras = new List<CamaraFija>();
        CamaraFija activa;
        Transform jugador;
        string salaActual;
        float proximaRevision, ocultoDesde = -1f, ultimoCambio;

        public Camera Activa { get; private set; }
        public string SalaActual => salaActual;
        /// <summary>Cantidad de cortes de cámara desde que empezó el nivel (para pruebas).</summary>
        public int Cambios { get; private set; }

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
            var mejor = Mejor(sala);
            Activar(mejor != null ? mejor.Camara : principal);
        }

        /// <summary>Una cinemática toma el control de la cámara.</summary>
        public void Pausar()
        {
            enabled = false;
            if (Activa != null) Activa.enabled = false;
        }

        /// <summary>Vuelve a las cámaras fijas al terminar la cinemática, con el mejor ángulo para donde quedó el jugador.</summary>
        public void Reanudar()
        {
            enabled = true;
            if (Activa != null) Activa.enabled = true;
            if (jugador == null) return;
            // Sala en la que quedó el jugador (las zonas de cámara son triggers de 3 x 3 m)
            foreach (var z in Physics.OverlapSphere(jugador.position + Vector3.up, 0.2f, 1 << 2, QueryTriggerInteraction.Collide))
            {
                var zc = z.GetComponent<ZonaCamara>();
                if (zc != null && !string.IsNullOrEmpty(zc.Sala)) { salaActual = zc.Sala; break; }
            }
            var mejor = Mejor(salaActual) ?? Mejor(null);
            if (mejor != null) Activar(mejor.Camara);
            Revisar(true);
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
            Cambios++;
            AlCambiarCamara?.Invoke(camara);
        }

        void LateUpdate()
        {
            if (Time.unscaledTime < proximaRevision) return;
            proximaRevision = Time.unscaledTime + 0.06f;
            Revisar(false);
        }

        float aspectoAplicado = -1f;

        /// <summary>
        /// Los encuadres se diseñaron para 16:9. En pantallas más estrechas (4:3, ventanas casi
        /// cuadradas) se abre el ángulo vertical para conservar el mismo campo horizontal, así nada
        /// de lo que se ve en 16:9 queda fuera por los lados. En pantallas más anchas no se toca.
        /// </summary>
        void AjustarAPantalla()
        {
            float aspecto = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            if (Mathf.Abs(aspecto - aspectoAplicado) < 0.01f) return;
            aspectoAplicado = aspecto;
            const float referencia = 16f / 9f;
            foreach (var c in camaras)
            {
                if (c.FovDiseno < 0f) c.FovDiseno = c.Camara.fieldOfView;
                float fov = c.FovDiseno;
                if (aspecto < referencia)
                {
                    float horizontal = 2f * Mathf.Atan(Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * referencia);
                    fov = 2f * Mathf.Atan(Mathf.Tan(horizontal * 0.5f) / aspecto) * Mathf.Rad2Deg;
                }
                c.Camara.fieldOfView = fov;
            }
        }

        void Revisar(bool inmediato)
        {
            AjustarAPantalla();
            if (jugador == null || Activa == null) return;
            bool puedeCortar = inmediato || Time.unscaledTime - ultimoCambio >= duracionMinimaPlano;

            // 1) Plano de interés: la cámara como guía
            var interes = MejorInteres();
            if (interes != null)
            {
                if (interes != activa && puedeCortar) Activar(interes.Camara);
                ocultoDesde = -1f;
                return;
            }

            var (dentro, visible, _) = Evaluar(Activa, jugador.position, MargenSalida, 0.15f, activa != null && activa.Interes ? 0f : TamanoSalida);
            if (activa != null && activa.Interes)
            {
                // Se alejó del objeto: espera el tiempo mínimo (si aún se ve) y vuelve a la sala
                if (!puedeCortar && dentro && visible) return;
                var normal = Mejor(salaActual) ?? Mejor(null);
                if (normal != null) Activar(normal.Camara);
                return;
            }

            // 2) Ángulos de la sala: solo se cambia si el jugador sale del cuadro o queda tapado
            if (dentro && visible) { ocultoDesde = -1f; return; }
            if (dentro && !visible && !inmediato)
            {
                if (ocultoDesde < 0f) ocultoDesde = Time.unscaledTime;
                if (Time.unscaledTime - ocultoDesde < toleranciaOculto) return;
            }
            var mejor = Mejor(salaActual) ?? Mejor(null);
            if (mejor != null && mejor != activa) Activar(mejor.Camara);
        }

        /// <summary>La cámara de sala (de la sala dada, o cualquiera cercana si es null) que mejor encuadra al jugador.</summary>
        CamaraFija Mejor(string sala)
        {
            // Primero ángulos que lo tengan bien adentro y de buen tamaño; si no hay, cualquiera que lo vea
            return MejorConMargen(sala, MargenEntrada, TamanoEntrada) ?? MejorConMargen(sala, MargenSalida, TamanoSalida)
                   ?? MejorConMargen(sala, MargenSalida, 0f);
        }

        CamaraFija MejorConMargen(string sala, float margen, float tamano)
        {
            CamaraFija mejor = null;
            float mejorPuntaje = float.MinValue;
            foreach (var c in camaras)
            {
                if (c.Interes) continue;   // los planos de interés solo se usan cerca de su objeto
                if (sala != null && c.Sala != sala) continue;
                if (sala == null && (c.transform.position - jugador.position).sqrMagnitude > 20f * 20f) continue;
                var (dentro, visible, puntaje) = Evaluar(c.Camara, jugador.position, margen, 0.15f, tamano);
                if (!dentro || !visible) continue;
                if (c.Principal) puntaje += 0.25f;        // a igualdad, el ángulo principal de la sala
                if (c == activa) puntaje += 0.3f;          // continuidad: no cambiar sin necesidad
                if (puntaje > mejorPuntaje) { mejorPuntaje = puntaje; mejor = c; }
            }
            return mejor;
        }

        /// <summary>
        /// Plano de interés aplicable: el jugador está cerca del objeto y lo está mirando (o ya está
        /// en ese plano), la cámara de la sala no lo muestra ya de cerca y el plano lo encuadra.
        /// </summary>
        CamaraFija MejorInteres()
        {
            CamaraFija mejor = null;
            float mejorDist = float.MaxValue;
            Vector3 adelante = jugador.forward; adelante.y = 0f;
            foreach (var c in camaras)
            {
                if (!c.InteresVigente) continue;
                if (c.Sala != salaActual) continue;   // solo los objetos de la sala en la que está
                bool yaActiva = c == activa;
                float radio = Mathf.Min(c.Radio, 2.6f) + (yaActiva ? 0.8f : 0f);
                Vector3 haciaObjeto = c.Punto - jugador.position; haciaObjeto.y = 0f;
                float dist = haciaObjeto.magnitude;
                if (dist > radio || dist >= mejorDist) continue;
                if (!yaActiva)
                {
                    // Solo si el técnico se dirige al objeto (no al pasar de largo)
                    if (dist > 0.6f && Vector3.Angle(adelante, haciaObjeto) > 70f) continue;
                    // Si la cámara de la sala ya está cerca del objeto, se ve bien sin cortar
                    if (!c.Prioritario && activa != null && !activa.Interes && Vector3.Distance(activa.transform.position, c.Punto + Vector3.up) < distanciaYaCercana) continue;
                }
                // En un plano cercano basta con ver al técnico de las rodillas a la cabeza
                var (dentro, visible, _) = Evaluar(c.Camara, jugador.position, 0.02f, 0.5f);
                if (!dentro || !visible) continue;
                mejor = c; mejorDist = dist;
            }
            return mejor;
        }

        /// <summary>
        /// ¿El jugador (de los pies —o la altura indicada— a la cabeza) entra en el cuadro de la cámara
        /// con el margen dado, y nada lo tapa? El puntaje premia que quede centrado y a distancia cómoda.
        /// </summary>
        public static (bool dentro, bool visible, float puntaje) Evaluar(Camera cam, Vector3 posJugador, float margen = 0.05f, float alturaInferior = 0.15f, float tamanoMinimo = 0f)
        {
            Vector3 pies = posJugador + Vector3.up * alturaInferior, pecho = posJugador + Vector3.up * 1.1f, cabeza = posJugador + Vector3.up * 1.7f;
            foreach (var p in new[] { pies, cabeza })
            {
                var v = cam.WorldToViewportPoint(p);
                if (v.z < cam.nearClipPlane || v.x < margen || v.x > 1f - margen || v.y < margen || v.y > 1f - margen)
                    return (false, false, 0f);
            }
            // Demasiado lejos: el técnico ocuparía muy poco de la pantalla
            if (tamanoMinimo > 0f && TamanoEnPantalla(cam, posJugador) < tamanoMinimo) return (false, false, 0f);
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

        /// <summary>Altura del técnico en la imagen (fracción del alto), de los pies a la cabeza.</summary>
        public static float TamanoEnPantalla(Camera cam, Vector3 posJugador)
        {
            var a = cam.WorldToViewportPoint(posJugador + Vector3.up * 0.05f);
            var b = cam.WorldToViewportPoint(posJugador + Vector3.up * 1.75f);
            return Mathf.Abs(b.y - a.y);
        }
    }
}
