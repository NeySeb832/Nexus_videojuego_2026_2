using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Nexus
{
    public enum LlaveRequerida
    {
        Ninguna,
        Roja,
        Azul
    }

    /// <summary>
    /// Puerta corredera de seguridad. Al acercarse el jugador, comprueba si lleva la tarjeta
    /// necesaria: si la tiene, las dos hojas se deslizan hacia los lados; si no, avisa
    /// (el HUD mostrará "Se requiere tarjeta roja"). La luz del lector indica el estado:
    /// color de la tarjeta = cerrada, verde = abierta.
    /// </summary>
    public class PuertaSeguridad : MonoBehaviour
    {
        /// <summary>(puerta, accesoConcedido) — para mensajes en pantalla y sonidos.</summary>
        public static event Action<PuertaSeguridad, bool> AlIntentarAbrir;

        [SerializeField] LlaveRequerida llave = LlaveRequerida.Roja;
        [Tooltip("Si está activo, la puerta no abre hasta que un PanelFusibles le devuelva la energía")]
        [SerializeField] bool requiereEnergia;
        [SerializeField] Transform hojaIzquierda;
        [SerializeField] Transform hojaDerecha;
        [SerializeField] float recorrido = 1.0f;
        [SerializeField] float duracion = 0.8f;
        [SerializeField] Renderer luzLector;
        [Tooltip("Obstáculo que impide a los androides cruzar mientras está cerrada")]
        [SerializeField] NavMeshObstacle bloqueoNavegacion;
        [SerializeField] AudioSource audioPuerta;

        static readonly int IdColor = Shader.PropertyToID("_Color");

        bool abierta;
        Vector3 izquierdaCerrada, derechaCerrada;
        MaterialPropertyBlock bloque;

        public LlaveRequerida Llave => llave;
        public bool Abierta => abierta;

        void Awake()
        {
            izquierdaCerrada = hojaIzquierda.localPosition;
            derechaCerrada = hojaDerecha.localPosition;
            bloque = new MaterialPropertyBlock();
            ActualizarLuz();
        }

        public bool RequiereEnergia
        {
            get => requiereEnergia;
            set { requiereEnergia = value; ActualizarLuz(); }
        }

        /// <summary>Lo llama el panel de fusibles al restablecer la energía.</summary>
        public void Energizar()
        {
            requiereEnergia = false;
            ActualizarLuz();
        }

        void OnTriggerEnter(Collider otro)
        {
            if (abierta || !otro.CompareTag("Player")) return;
            if (Cinematica.EnCurso) return;

            if (sellada)
            {
                HUDNexus.Aviso("PUERTA SELLADA. El protocolo CERBERUS bloqueó la salida.");
                AlIntentarAbrir?.Invoke(this, false);
                return;
            }
            if (requiereEnergia)
            {
                HUDNexus.Aviso("SIN ENERGÍA. Restablece el suministro en el Cuarto Eléctrico.");
                AlIntentarAbrir?.Invoke(this, false);
                return;
            }

            var inventario = otro.GetComponentInParent<InventarioJugador>();
            bool concedido = llave == LlaveRequerida.Ninguna ||
                             (inventario != null && inventario.Tiene(TarjetaNecesaria()));

            if (!concedido)
                HUDNexus.Aviso(llave == LlaveRequerida.Roja ? "ACCESO DENEGADO. Se requiere la tarjeta de acceso ROJA."
                                                            : "ACCESO DENEGADO. Se requiere la tarjeta de acceso AZUL.");
            else if (llave != LlaveRequerida.Ninguna)
                HUDNexus.Aviso("Acceso concedido");

            AlIntentarAbrir?.Invoke(this, concedido);
            if (concedido) Abrir();
        }

        TipoColeccionable TarjetaNecesaria() =>
            llave == LlaveRequerida.Roja ? TipoColeccionable.TarjetaRoja : TipoColeccionable.TarjetaAzul;

        public void Abrir()
        {
            if (abierta) return;
            abierta = true;
            ActualizarLuz();
            if (bloqueoNavegacion != null) bloqueoNavegacion.enabled = false;
            if (audioPuerta != null) audioPuerta.Play();
            StartCoroutine(Deslizar());
        }

        bool sellada;

        /// <summary>
        /// Cierra la puerta (la de servicio, al entrar los técnicos). Si se sella, ya no se abre:
        /// el protocolo CERBERUS bloqueó la salida.
        /// </summary>
        public void Cerrar(bool sellar)
        {
            sellada = sellar;
            if (sellar) { llave = LlaveRequerida.Roja; requiereEnergia = true; }
            if (!abierta) { ActualizarLuz(); return; }
            abierta = false;
            ActualizarLuz();
            if (bloqueoNavegacion != null) bloqueoNavegacion.enabled = true;
            if (audioPuerta != null) audioPuerta.Play();
            StopAllCoroutines();
            StartCoroutine(Deslizar(1f, 0f));
        }

        public bool Sellada => sellada;

        IEnumerator Deslizar() => Deslizar(0f, 1f);

        IEnumerator Deslizar(float desde, float hasta)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / duracion)
            {
                float k = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t));
                hojaIzquierda.localPosition = izquierdaCerrada + Vector3.left * (recorrido * k);
                hojaDerecha.localPosition = derechaCerrada + Vector3.right * (recorrido * k);
                yield return null;
            }
            hojaIzquierda.localPosition = izquierdaCerrada + Vector3.left * (recorrido * hasta);
            hojaDerecha.localPosition = derechaCerrada + Vector3.right * (recorrido * hasta);
        }

        void ActualizarLuz()
        {
            if (luzLector == null || bloque == null) return;
            Color color = sellada ? new Color(1f, 0.02f, 0.02f)
                : requiereEnergia ? new Color(0.02f, 0.02f, 0.02f)
                : abierta ? new Color(0.1f, 1f, 0.25f)
                : llave == LlaveRequerida.Roja ? new Color(1f, 0.08f, 0.05f)
                : llave == LlaveRequerida.Azul ? new Color(0.15f, 0.45f, 1f)
                : new Color(1f, 0.6f, 0.1f);
            luzLector.GetPropertyBlock(bloque);
            bloque.SetColor(IdColor, color * 4f);
            luzLector.SetPropertyBlock(bloque);
        }
    }
}
