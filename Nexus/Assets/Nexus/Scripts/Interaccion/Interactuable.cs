using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Base de todo lo que el jugador puede usar con el botón de interactuar (E en teclado,
    /// botón en pantalla en Android): terminales, notas, paneles, terminales de guardado, salidas.
    /// Necesita un Collider en modo Trigger que marca el área desde la que se puede usar.
    /// </summary>
    public abstract class Interactuable : MonoBehaviour
    {
        [SerializeField] protected string textoAccion = "Examinar";
        [Tooltip("Nuevo objetivo que se muestra en el HUD al completarlo (vacío = no cambia)")]
        [SerializeField] protected string objetivoAlCompletar;

        public virtual string TextoAccion => textoAccion;

        public abstract void Interactuar(InteraccionJugador jugador);

        public void ConfigurarObjetivo(string objetivo) => objetivoAlCompletar = objetivo;

        protected void CompletarObjetivo()
        {
            if (!string.IsNullOrEmpty(objetivoAlCompletar)) HUDNexus.Objetivo(objetivoAlCompletar);
        }

        protected virtual void OnTriggerEnter(Collider otro)
        {
            var j = otro.GetComponentInParent<InteraccionJugador>();
            if (j != null) j.Entrar(this);
        }

        protected virtual void OnTriggerExit(Collider otro)
        {
            var j = otro.GetComponentInParent<InteraccionJugador>();
            if (j != null) j.Salir(this);
        }

        protected virtual void OnDisable()
        {
            var j = FindFirstObjectByType<InteraccionJugador>();
            if (j != null) j.Salir(this);
        }
    }
}
