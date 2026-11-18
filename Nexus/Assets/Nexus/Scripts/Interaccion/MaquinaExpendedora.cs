using TMPro;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Máquina expendedora de la cafetería abandonada. Todavía tiene algunos productos:
    /// bebidas (Speedmax, recupera 1 corazón) o ponqués (Chocorramo, recupera toda la salud).
    /// Riesgo: al soltar el producto hace ruido y los androides cercanos vienen a revisar.
    /// </summary>
    public class MaquinaExpendedora : Interactuable
    {
        [SerializeField] TipoColeccionable producto = TipoColeccionable.Speedmax;
        [SerializeField] int existencias = 2;
        [SerializeField] float radioRuido = 11f;
        [Tooltip("Productos visibles detrás del vidrio: se ocultan a medida que se venden")]
        [SerializeField] GameObject[] productosVisibles;
        [SerializeField] TMP_Text letrero;

        string NombreProducto => producto == TipoColeccionable.Chocorramo ? "Chocorramo" : "Speedmax";

        public override string TextoAccion => existencias > 0
            ? $"Sacar {NombreProducto} de la máquina"
            : "Máquina expendedora (agotada)";

        public void Configurar(TipoColeccionable tipo, int cantidad)
        {
            producto = tipo; existencias = cantidad;
        }

        int existenciasIniciales;

        void Start()
        {
            existenciasIniciales = Mathf.Max(1, existencias);
            ActualizarVisual();
        }

        public override void Interactuar(InteraccionJugador jugador)
        {
            if (existencias <= 0)
            {
                HUDNexus.Aviso("AGOTADO. Solo quedan envoltorios vacíos.");
                return;
            }
            existencias--;
            AudioNexus.Efecto(Sfx.Expendedora, transform.position + Vector3.up * 0.6f, 1f);
            if (jugador.Inventario != null) jugador.Inventario.Agregar(producto);
            HUDNexus.Aviso($"La máquina soltó un {NombreProducto}… y el golpe se oyó por todo el pasillo");
            foreach (var a in AndroideIA.Todos)
                if (a != null && (a.transform.position - transform.position).sqrMagnitude < radioRuido * radioRuido)
                    a.Atraer(transform.position + transform.forward * 1.2f, 2f);
            ActualizarVisual();
            CompletarObjetivo();
        }

        void ActualizarVisual()
        {
            if (productosVisibles != null)
            {
                // Se reparten los productos visibles entre las existencias que quedan
                int visibles = existencias <= 0 ? 0 : Mathf.CeilToInt(productosVisibles.Length * existencias / (float)existenciasIniciales);
                for (int i = 0; i < productosVisibles.Length; i++)
                    if (productosVisibles[i] != null) productosVisibles[i].SetActive(i < visibles);
            }
            if (letrero != null)
            {
                letrero.text = existencias > 0 ? "DISPONIBLE" : "AGOTADO";
                letrero.color = existencias > 0 ? new Color(0.4f, 1f, 0.55f) : new Color(1f, 0.3f, 0.25f);
            }
        }
    }
}
