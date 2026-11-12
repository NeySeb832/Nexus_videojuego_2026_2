using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Cámaras fijas estilo survival horror clásico: todas las cámaras del nivel son hijas
    /// de este objeto y solo una está encendida a la vez. Las <see cref="ZonaCamara"/> del
    /// suelo piden el cambio cuando el jugador las pisa.
    /// Además de crear tensión, renderizar con una sola cámara fija es más barato en móvil y WebGL.
    /// </summary>
    public class GestorCamaras : MonoBehaviour
    {
        public static GestorCamaras Instancia { get; private set; }
        public static event Action<Camera> AlCambiarCamara;

        [SerializeField] Camera camaraInicial;

        readonly List<Camera> camaras = new List<Camera>();

        public Camera Activa { get; private set; }

        void Awake()
        {
            Instancia = this;
            camaras.AddRange(GetComponentsInChildren<Camera>(true));
            foreach (var c in camaras) c.enabled = false;
            Activar(camaraInicial != null ? camaraInicial : (camaras.Count > 0 ? camaras[0] : null));
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        public void Activar(Camera camara)
        {
            if (camara == null || camara == Activa) return;
            if (Activa != null) Activa.enabled = false;
            Activa = camara;
            Activa.enabled = true;
            AlCambiarCamara?.Invoke(camara);
        }
    }
}
