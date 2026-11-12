using TMPro;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Terminal del centro de datos. Muestra un mensaje en su pantalla y, al usarla, abre un
    /// cuadro de diálogo con mensajes de error del sistema que guían la historia.
    /// </summary>
    public class TerminalNexus : Interactuable
    {
        [SerializeField] string titulo = "TERMINAL DE MANTENIMIENTO";
        [TextArea(2, 6)]
        [SerializeField] string[] mensajes =
        {
            "ALERTA: Temperatura al 80%.",
            "Protocolo de seguridad androide activado.",
            "Busque la tarjeta de acceso nivel 2."
        };
        [SerializeField] TMP_Text textoPantalla;

        public string Titulo => titulo;
        public string[] Mensajes => mensajes;

        void Reset() => textoAccion = "Usar terminal";

        public void Configurar(string nuevoTitulo, string[] nuevosMensajes, string objetivo)
        {
            titulo = nuevoTitulo; mensajes = nuevosMensajes; objetivoAlCompletar = objetivo;
            textoAccion = "Usar terminal";
        }

        void Start()
        {
            if (textoPantalla != null && mensajes.Length > 0)
                textoPantalla.text = $"NEXUS OS v3.2\n<color=#ff5a40>> {titulo}</color>\n> {mensajes[0]}\n> _";
        }

        public override void Interactuar(InteraccionJugador jugador)
        {
            HUDNexus.MostrarDialogo(titulo, mensajes, CompletarObjetivo);
        }
    }
}
