using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Datos de los créditos del juego (materia, docente, integrantes, institución y logos).
    /// Es un archivo de datos (ScriptableObject): se edita desde el Inspector de Unity sin tocar código.
    /// Archivo: Assets/Nexus/Datos/Creditos.asset
    /// </summary>
    [CreateAssetMenu(fileName = "Creditos", menuName = "Nexus/Datos de créditos")]
    public class CreditosNexus : ScriptableObject
    {
        [System.Serializable]
        public class Integrante
        {
            public string nombreCompleto;
            public string rol;
        }

        [Header("Institución")]
        public string institucion = "[COMPLETAR: Nombre de la institución]";
        public string programa = "[COMPLETAR: Programa académico]";
        public string materia = "Modelado 3D y Videojuegos";
        public string docente = "[COMPLETAR: Nombre del docente]";
        public string periodo = "[COMPLETAR: Semestre] · 2026";
        [Tooltip("Logos institucionales (PNG con fondo transparente)")]
        public Sprite[] logos;

        [Header("Equipo")]
        public Integrante[] integrantes =
        {
            new Integrante { nombreCompleto = "Neyder [COMPLETAR: apellidos]", rol = "Desarrollo · Modelo A (Técnico de Redes)" },
            new Integrante { nombreCompleto = "Deisy [COMPLETAR: apellidos]", rol = "Desarrollo · Modelo B (Analista de Sistemas)" },
        };

        [Header("Herramientas y recursos")]
        [TextArea(3, 12)]
        public string recursos =
            "Motor: Unity 6 (Universal Render Pipeline)\n" +
            "Avatares de los integrantes: Avaturn\n" +
            "Animaciones y androide (Y Bot): Mixamo · Adobe\n" +
            "Conversión de modelos: Blender\n" +
            "Texturas, shaders, interfaz y efectos: generados por código para este proyecto\n" +
            "Asistencia de programación: Claude Code (Anthropic)";
    }
}
