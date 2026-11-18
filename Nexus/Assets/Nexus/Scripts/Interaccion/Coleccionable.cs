using System;
using UnityEngine;

namespace Nexus
{
    public enum TipoColeccionable
    {
        DiscoDuro,
        TarjetaRoja,
        TarjetaAzul,
        Refrigerante,
        Fusible,
        USB,
        BebidaEnergetica,
        BarritaProteina,
        // Armas y munición (al final: los números de los anteriores no cambian)
        Cuchillo,
        Pistola,
        Escopeta,
        BalasPistola,
        CartuchosEscopeta
    }

    /// <summary>
    /// Objeto que el jugador recoge al tocarlo (Collider en modo Trigger).
    /// Lo suma al inventario, muestra un aviso y, si corresponde, actualiza el objetivo.
    /// Avisa por un evento estático para que el audio o la IA reaccionen (por ejemplo, la alarma
    /// al extraer un Disco Duro).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Coleccionable : MonoBehaviour
    {
        public static event Action<Coleccionable> AlRecoger;

        [SerializeField] TipoColeccionable tipo = TipoColeccionable.DiscoDuro;
        [SerializeField] string nombreVisible = "Disco Duro Maestro";
        [Tooltip("Nuevo objetivo que se muestra en el HUD al recogerlo (vacío = no cambia)")]
        [SerializeField] string objetivoAlRecoger;
        [Tooltip("Mensaje de sistema que aparece como diálogo al recogerlo (vacío = solo aviso)")]
        [TextArea(2, 4)] [SerializeField] string[] dialogoAlRecoger;
        [SerializeField] AudioClip sonidoRecoger;
        [Tooltip("Unidades que da al recogerlo (munición)")]
        [SerializeField] int cantidad = 1;

        public const float SegundosPorRefrigerante = 20f;

        public TipoColeccionable Tipo => tipo;
        public string NombreVisible => nombreVisible;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public void Configurar(TipoColeccionable nuevoTipo, string nombre, string objetivo = null, string[] dialogo = null, int unidades = 1)
        {
            tipo = nuevoTipo; nombreVisible = nombre; objetivoAlRecoger = objetivo; dialogoAlRecoger = dialogo; cantidad = Mathf.Max(1, unidades);
        }

        void OnTriggerEnter(Collider otro)
        {
            if (!otro.CompareTag("Player")) return;

            switch (tipo)
            {
                case TipoColeccionable.Refrigerante:
                    EstadoPartida.Refrigerantes++;
                    if (NivelNexus.Instancia != null) NivelNexus.Instancia.SumarTiempo(SegundosPorRefrigerante);
                    HUDNexus.Aviso($"Cartucho de refrigerante: +{SegundosPorRefrigerante:0} s");
                    break;
                case TipoColeccionable.DiscoDuro:
                    EstadoPartida.Discos++;
                    if (NivelNexus.Instancia != null) NivelNexus.Instancia.DiscoObtenido = true;
                    HUDNexus.Aviso($"Obtuviste: {nombreVisible} ({EstadoPartida.Discos}/{EstadoPartida.DiscosTotales})");
                    break;
                default:
                    var inventario = otro.GetComponentInParent<InventarioJugador>();
                    if (inventario != null) inventario.Agregar(tipo, cantidad);
                    HUDNexus.Aviso(cantidad > 1 ? $"Obtuviste: {nombreVisible} x{cantidad}" : $"Obtuviste: {nombreVisible}");
                    // Un arma nueva se equipa sola si el técnico no tiene una de fuego en la mano
                    var arma = otro.GetComponentInParent<ArmaJugador>();
                    if (arma != null) arma.AlRecoger(tipo);
                    break;
            }
            EstadoPartida.Notificar();

            if (sonidoRecoger != null) AudioSource.PlayClipAtPoint(sonidoRecoger, transform.position);
            else AudioNexus.Interfaz(Sfx.Recoger);
            if (!string.IsNullOrEmpty(objetivoAlRecoger)) HUDNexus.Objetivo(objetivoAlRecoger);
            if (dialogoAlRecoger != null && dialogoAlRecoger.Length > 0)
                HUDNexus.MostrarDialogo("ALERTA DEL SISTEMA", dialogoAlRecoger, null);

            AlRecoger?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
