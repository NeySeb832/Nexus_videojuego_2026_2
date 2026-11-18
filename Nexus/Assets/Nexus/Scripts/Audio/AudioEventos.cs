using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Conecta los eventos del juego con sus sonidos sin tocar los scripts que los lanzan:
    /// recoger objetos (y alarma del Disco Maestro), puertas y detección de los androides.
    /// </summary>
    public static class AudioEventos
    {
        static float ultimaDeteccion = -10f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Iniciar()
        {
            ultimaDeteccion = -10f;
            // "-=" antes de "+=" evita suscripciones duplicadas si el editor no recarga el dominio
            Coleccionable.AlRecoger -= AlRecoger;
            Coleccionable.AlRecoger += AlRecoger;
            PuertaSeguridad.AlIntentarAbrir -= AlIntentarAbrir;
            PuertaSeguridad.AlIntentarAbrir += AlIntentarAbrir;
            AndroideIA.AlDetectar -= AlDetectar;
            AndroideIA.AlDetectar += AlDetectar;
        }

        static void AlRecoger(Coleccionable c)
        {
            GestorAudio.Sfx("recoger");
            // Al extraer un Disco Duro Maestro salta la alarma (dura hasta que cambia la escena)
            if (c.Tipo == TipoColeccionable.DiscoDuro) GestorAudio.Alarma(true);
        }

        static void AlIntentarAbrir(PuertaSeguridad puerta, bool concedido)
        {
            if (concedido) GestorAudio.SfxEn("puerta_abrir", puerta.transform.position);
            else GestorAudio.Sfx("acceso_denegado");
        }

        static void AlDetectar(AndroideIA androide)
        {
            // Evita que suene en cada androide si varios te ven a la vez
            if (Time.time - ultimaDeteccion < 3f) return;
            ultimaDeteccion = Time.time;
            GestorAudio.Sfx("androide_detecta");
        }
    }
}