using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Datos de una cámara fija del nivel:
    ///  - Sala a la que pertenece y si es la principal (la que se usa al entrar).
    ///  - Cámara de INTERÉS: plano cercano de un objeto importante (terminal, nota, puerta,
    ///    máquina, objeto para recoger...). Cuando el jugador se acerca a menos de "Radio" metros,
    ///    el <see cref="GestorCamaras"/> corta a este plano: la cámara funciona como guía y muestra
    ///    con claridad dónde está lo que hay que usar. Si el objeto ya se recogió, deja de usarse.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CamaraFija : MonoBehaviour
    {
        public string Sala;
        public bool Principal;

        [Header("Cámara de interés")]
        public bool Interes;
        public GameObject Objetivo;
        public Vector3 Punto;
        public float Radio = 3f;

        Camera camara;
        public Camera Camara => camara != null ? camara : (camara = GetComponent<Camera>());

        /// <summary>El objeto sigue en el nivel (no se recogió).</summary>
        public bool InteresVigente => Interes && Objetivo != null && Objetivo.activeInHierarchy;
    }
}
