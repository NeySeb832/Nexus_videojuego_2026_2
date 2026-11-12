using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Nota con la historia del centro de datos (lore) o con pistas, como un código.
    /// Al leerla se guarda en el archivo de notas del jugador.
    /// </summary>
    public class NotaLore : Interactuable
    {
        [SerializeField] string titulo = "Registro del turno nocturno";
        [TextArea(4, 12)] [SerializeField] string texto = "...";

        public string Titulo => titulo;
        public string Texto => texto;

        void Reset() => textoAccion = "Leer nota";

        public void Configurar(string nuevoTitulo, string nuevoTexto, string objetivo)
        {
            titulo = nuevoTitulo; texto = nuevoTexto; objetivoAlCompletar = objetivo;
            textoAccion = "Leer nota";
        }

        public override void Interactuar(InteraccionJugador jugador)
        {
            if (!EstadoPartida.NotasLeidas.Contains(titulo)) EstadoPartida.NotasLeidas.Add(titulo);
            HUDNexus.MostrarDialogo("NOTA: " + titulo, new[] { texto }, CompletarObjetivo);
        }
    }
}
