using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus
{
    /// <summary>
    /// Cuerpo de alguien que entró antes que los técnicos (empleados de Nexus, operadores del equipo
    /// táctico). Al registrarlo, el técnico describe lo que ve, recibe lo que la víctima llevaba
    /// (arma o munición) y encuentra una nota que cuenta qué pasó antes de la misión.
    /// Los objetos se ven junto al cuerpo, en el suelo o en su mano, pero solo se obtienen al
    /// registrarlo. Un cuerpo ya registrado no vuelve a dar nada (también al cargar la partida).
    /// </summary>
    public class Cadaver : Interactuable
    {
        [SerializeField] string id = "cadaver";
        [SerializeField] string quien = "Empleado de Nexus";
        [Tooltip("Lo que el técnico ve y piensa al registrar el cuerpo (una línea por pantalla)")]
        [TextArea(2, 6)] [SerializeField] string[] alRegistrar;
        [SerializeField] bool daObjeto = true;
        [SerializeField] TipoColeccionable objeto = TipoColeccionable.BalasPistola;
        [SerializeField] int cantidad = 1;
        [SerializeField] string nombreObjeto = "Balas de pistola";
        [SerializeField] string tituloNota;
        [TextArea(4, 12)] [SerializeField] string textoNota;
        [Tooltip("Modelos del objeto junto al cuerpo: desaparecen al tomarlo")]
        [SerializeField] GameObject[] visuales;
        [Header("Segundo objeto (munición que llevaba encima)")]
        [SerializeField] bool conExtra;
        [SerializeField] TipoColeccionable extra = TipoColeccionable.BalasPistola;
        [SerializeField] int cantidadExtra = 1;
        [SerializeField] string nombreExtra = "Balas de pistola";

        public void ConfigurarExtra(TipoColeccionable tipo, int unidades, string nombre)
        {
            conExtra = true; extra = tipo; cantidadExtra = Mathf.Max(1, unidades); nombreExtra = nombre;
        }

        bool registrado;
        string Clave => SceneManager.GetActiveScene().name + "/" + id;

        public bool Registrado => registrado;
        public override string TextoAccion => registrado ? "Examinar" : textoAccion;

        void Reset() => textoAccion = "Registrar el cuerpo";

        public void Configurar(string nuevoId, string nombre, string[] lineas, bool conObjeto, TipoColeccionable tipo, int unidades,
                               string nombreDelObjeto, string titulo, string texto, string objetivo, GameObject[] modelos)
        {
            id = nuevoId; quien = nombre; alRegistrar = lineas; daObjeto = conObjeto; objeto = tipo; cantidad = Mathf.Max(1, unidades);
            nombreObjeto = nombreDelObjeto; tituloNota = titulo; textoNota = texto; objetivoAlCompletar = objetivo;
            visuales = modelos; textoAccion = "Registrar el cuerpo";
        }

        void Start()
        {
            if (!EstadoPartida.Registrados.Contains(Clave)) return;
            registrado = true;
            OcultarObjetos();
        }

        public override void Interactuar(InteraccionJugador jugador)
        {
            if (registrado)
            {
                HUDNexus.MostrarDialogo(quien, new[] { "Ya revisaste el cuerpo. No queda nada útil." }, null);
                return;
            }
            var lineas = new List<string>();
            if (alRegistrar != null) lineas.AddRange(alRegistrar);
            if (daObjeto)
            {
                string texto = cantidad > 1 ? $"{nombreObjeto} x{cantidad}" : nombreObjeto;
                if (conExtra) texto += cantidadExtra > 1 ? $" y {nombreExtra} x{cantidadExtra}" : $" y {nombreExtra}";
                lineas.Add($"<color=#ffd27f>Obtuviste: {texto}</color>");
            }
            if (lineas.Count == 0) lineas.Add("...");
            // El objeto se entrega al terminar de leer, como al recogerlo de la mano de alguien
            HUDNexus.MostrarDialogo(quien, lineas.ToArray(), () =>
            {
                Entregar(jugador);
                if (!string.IsNullOrEmpty(tituloNota))
                {
                    if (!EstadoPartida.NotasLeidas.Contains(tituloNota)) EstadoPartida.NotasLeidas.Add(tituloNota);
                    // Un párrafo por pantalla: así la nota completa cabe en el cuadro de diálogo
                    HUDNexus.MostrarDialogo("NOTA: " + tituloNota, textoNota.Split('\n'), CompletarObjetivo);
                }
                else CompletarObjetivo();
            });
        }

        void Entregar(InteraccionJugador jugador)
        {
            registrado = true;
            if (!EstadoPartida.Registrados.Contains(Clave)) EstadoPartida.Registrados.Add(Clave);
            if (daObjeto && jugador != null)
            {
                var inventario = jugador.GetComponentInParent<InventarioJugador>();
                if (inventario != null)
                {
                    // Primero la munición: así el arma nueva se equipa con el cargador lleno
                    if (conExtra) inventario.Agregar(extra, cantidadExtra);
                    inventario.Agregar(objeto, cantidad);
                }
                var arma = jugador.GetComponentInParent<ArmaJugador>();
                if (arma != null) arma.AlRecoger(objeto);
            }
            OcultarObjetos();
            if (daObjeto) AudioNexus.Interfaz(Sfx.Recoger);
            EstadoPartida.Notificar();
        }

        void OcultarObjetos()
        {
            if (visuales == null) return;
            foreach (var v in visuales) if (v != null) v.SetActive(false);
        }
    }
}
