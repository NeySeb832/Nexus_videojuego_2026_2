using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus
{
    /// <summary>
    /// Detecta los objetos interactuables cercanos, muestra el aviso "[E] Leer terminal" en el HUD
    /// y los activa al pulsar el botón de interactuar.
    /// </summary>
    public class InteraccionJugador : MonoBehaviour
    {
        readonly List<Interactuable> cercanos = new List<Interactuable>();
        InputAction accionInteractuar;
        Interactuable actual;

        public InventarioJugador Inventario { get; private set; }
        public VidaJugador Vida { get; private set; }

        /// <summary>Permite activar la interacción desde un botón táctil o desde una prueba automática.</summary>
        public bool PulsacionSimulada { get; set; }

        void Awake()
        {
            Inventario = GetComponent<InventarioJugador>();
            Vida = GetComponent<VidaJugador>();
            accionInteractuar = InputSystem.actions.FindAction("Player/Interact");
        }

        // Igual que en ControladorJugador: la acción del proyecto hay que habilitarla a mano
        void OnEnable() => accionInteractuar?.Enable();

        public void Entrar(Interactuable i) { if (!cercanos.Contains(i)) cercanos.Add(i); }
        public void Salir(Interactuable i) => cercanos.Remove(i);

        void Update()
        {
            cercanos.RemoveAll(i => i == null || !i.isActiveAndEnabled);
            actual = MasCercano();
            HUDNexus.Prompt(actual != null && !HUDNexus.DialogoAbierto ? $"[E] {actual.TextoAccion}" : null);

            bool pulsado = PulsacionSimulada || (accionInteractuar != null && accionInteractuar.WasPressedThisFrame());
            PulsacionSimulada = false;
            if (pulsado && actual != null && !HUDNexus.DialogoAbierto && Time.timeScale > 0f)
                actual.Interactuar(this);
        }

        const float AlcanceMaximo = 4f;

        Interactuable MasCercano()
        {
            Interactuable mejor = null;
            float mejorDist = AlcanceMaximo * AlcanceMaximo;
            // El filtro de distancia descarta entradas viejas: si el jugador aparece de golpe en otro
            // lugar (al cargar partida o reaparecer), Unity no avisa de que salió del trigger anterior.
            foreach (var i in cercanos)
            {
                float d = (i.transform.position - transform.position).sqrMagnitude;
                if (d < mejorDist) { mejorDist = d; mejor = i; }
            }
            return mejor;
        }
    }
}
