using UnityEngine;

namespace Nexus
{
    /// <summary>Efectos de sonido del juego (el número es la posición en BancoSonidos.efectos).</summary>
    public enum Sfx
    {
        DisparoPistola, DisparoEscopeta, EscopetaBombeo, Recarga, CartuchoCarga, SinMunicion, Cuchillo, ImpactoMetal,
        AndroideDetecta, AndroidePaso, AndroideDespierta, AndroideDestruido,
        JugadorPaso, JugadorHerido, Puerta, Recoger, UIBlip, TerminalBeep,
        ValvulaGiro, Vapor, JefeCarga, JefeDescarga, Expendedora, Senuelo, Victoria, GameOver
    }

    /// <summary>
    /// Todos los sonidos del juego. Se generan por síntesis (menú Nexus → Audio → Generar sonidos)
    /// y se guardan en Resources para que el gestor de audio los cargue en cualquier escena.
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

        public AudioClip Clip(Sfx s) => efectos != null && (int)s < efectos.Length ? efectos[(int)s] : null;
    }
}
