using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Puzle del Nivel 1: el panel tiene un fusible fundido. Si el jugador trae un fusible del
    /// almacén, lo instala: se encienden las luces de la zona sin energía y se activan sus puertas.
    /// </summary>
    public class PanelFusibles : Interactuable
    {
        [SerializeField] PuertaSeguridad[] puertasQueEnergiza;
        [SerializeField] GameObject[] lucesQueEnciende;
        [SerializeField] Renderer luzEstado;

        static readonly int IdColor = Shader.PropertyToID("_Color");
        bool reparado;
        MaterialPropertyBlock bloque;

        void Reset() => textoAccion = "Revisar panel de fusibles";

        public void Configurar(PuertaSeguridad[] puertas, GameObject[] luces, string objetivo)
        {
            puertasQueEnergiza = puertas; lucesQueEnciende = luces; objetivoAlCompletar = objetivo;
        }

        void Start()
        {
            bloque = new MaterialPropertyBlock();
            foreach (var l in lucesQueEnciende) if (l != null) l.SetActive(false);
            PintarLuz();
        }

        public override string TextoAccion => reparado ? "Panel de fusibles (operativo)" : textoAccion;

        public override void Interactuar(InteraccionJugador jugador)
        {
            if (reparado)
            {
                HUDNexus.Aviso("El panel funciona correctamente.");
                return;
            }
            if (!jugador.Inventario.Consumir(TipoColeccionable.Fusible))
            {
                HUDNexus.MostrarDialogo("PANEL DE FUSIBLES — CUARTO ELÉCTRICO", new[]
                {
                    "ERROR: Fusible F-3 fundido. Circuito de la Oficina de Seguridad sin energía.",
                    "Reemplace el fusible para restablecer el suministro. Hay repuestos en el Almacén."
                }, null);
                HUDNexus.Objetivo("Busca un fusible de repuesto en el Almacén");
                return;
            }

            reparado = true;
            foreach (var p in puertasQueEnergiza) if (p != null) p.Energizar();
            foreach (var l in lucesQueEnciende) if (l != null) l.SetActive(true);
            PintarLuz();
            HUDNexus.MostrarDialogo("PANEL DE FUSIBLES — CUARTO ELÉCTRICO", new[]
            {
                "Fusible F-3 instalado. Suministro eléctrico restablecido.",
                "Oficina de Seguridad: cerraduras en línea."
            }, CompletarObjetivo);
        }

        void PintarLuz()
        {
            if (luzEstado == null || bloque == null) return;
            luzEstado.GetPropertyBlock(bloque);
            bloque.SetColor(IdColor, (reparado ? new Color(0.1f, 1f, 0.25f) : new Color(1f, 0.08f, 0.05f)) * 4f);
            luzEstado.SetPropertyBlock(bloque);
        }
    }
}
