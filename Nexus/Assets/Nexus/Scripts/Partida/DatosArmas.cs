using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Recursos de las armas: los modelos que se ponen en la mano, los efectos y la calibración de
    /// cómo se sujetan (posición y giro respecto al hueso de la mano derecha, calculados por la
    /// herramienta Nexus → Armas → 2 a partir de la pose de disparo de Mixamo).
    /// Archivo: Assets/Nexus/Datos/DatosArmas.asset
    /// </summary>
    [CreateAssetMenu(fileName = "DatosArmas", menuName = "Nexus/Datos de armas")]
    public class DatosArmas : ScriptableObject
    {
        [Header("Modelos en la mano (pivote = empuñadura, +Z = cañón)")]
        public GameObject cuchillo;
        public GameObject pistola;
        public GameObject escopeta;

        [Header("Sujeción respecto a la mano derecha")]
        public Vector3 posicionEnMano;
        public Vector3 rotacionEnMano;

        [Header("Efectos")]
        public GameObject impacto;       // chispas al golpear
        public Material laser;           // mira láser y trazadoras
        public Material trazadora;
    }
}
