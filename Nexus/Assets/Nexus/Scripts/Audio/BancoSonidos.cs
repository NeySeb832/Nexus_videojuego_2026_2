using UnityEngine;

namespace Nexus
{
    /// <summary>Efectos de sonido del juego (el número es la posición en BancoSonidos.efectos).</summary>
    public enum Sfx
    {
        DisparoPistola, DisparoEscopeta, EscopetaBombeo, Recarga, CartuchoCarga, SinMunicion, Cuchillo, ImpactoMetal,
        AndroideDetecta, AndroidePaso, AndroideDespierta, AndroideDestruido,
        JugadorPaso, JugadorHerido, Puerta, Recoger, UIBlip, TerminalBeep,
        ValvulaGiro, Vapor, JefeCarga, JefeDescarga, Expendedora, Senuelo, Victoria, GameOver,
        JugadoraHerida   // quejido de Deisy (al final para no mover los números de los demás)
    }

    /// <summary>
    /// Todos los sonidos del juego, guardados en Resources para que el gestor de audio los cargue en
    /// cualquier escena. Los efectos y la música vienen de Pixabay (menú Nexus → Audio → Usar sonidos de
    /// Pixabay); los sintetizados por código (Nexus → Audio → Generar sonidos) quedan de respaldo para
    /// cualquier hueco.
    /// </summary>
    [CreateAssetMenu(menuName = "Nexus/Banco de sonidos")]
    public class BancoSonidos : ScriptableObject
    {
        [Header("Música y ambiente (bucles)")]
        public AudioClip musicaMenu;
        public AudioClip musicaExploracion;
        public AudioClip musicaTension;
        public AudioClip musicaJefe;
        public AudioClip ambienteVentiladores;
        public AudioClip sirenaAlarma;

        [Header("Efectos (en el orden del enum Sfx)")]
        public AudioClip[] efectos;

        /// <summary>Varias tomas de un mismo efecto (las pisadas): se elige una al azar cada vez.</summary>
        [System.Serializable]
        public class Variantes
        {
            public Sfx tipo;
            public AudioClip[] clips;
        }
        public Variantes[] variantes;

        [Tooltip("De dónde vienen los sonidos asignados (se muestra en las pruebas)")]
        public string origen;

        public AudioClip Clip(Sfx s)
        {
            if (variantes != null)
                foreach (var v in variantes)
                    if (v != null && v.tipo == s && v.clips != null && v.clips.Length > 0)
                        return v.clips[Random.Range(0, v.clips.Length)];
            return efectos != null && (int)s < efectos.Length ? efectos[(int)s] : null;
        }
    }
}
