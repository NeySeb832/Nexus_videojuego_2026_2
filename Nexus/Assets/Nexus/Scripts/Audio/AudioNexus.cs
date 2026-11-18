using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus
{
    /// <summary>
    /// Gestor de audio (requisito 1.4: música de fondo con componentes de audio). Se crea solo al
    /// arrancar el juego y sobrevive a los cambios de escena.
    ///
    /// MÚSICA POR CAPAS (cada capa es un AudioSource en bucle; el volumen se funde según el juego):
    ///  - Menú: tema propio.
    ///  - Niveles: música de exploración + zumbido de ventiladores del centro de datos.
    ///  - Tensión: entra cuando algún androide persigue al técnico.
    ///  - Alarma: la sirena, mientras dura la alarma del disco.
    ///  - Jefe: la música de combate contra SENTINEL-Ω (baja la de exploración).
    ///  - Al terminar (victoria o game over) suena su remate y la música se apaga.
    ///
    /// EFECTOS: AudioNexus.Efecto(Sfx, posición) los reproduce en 3D desde un grupo de fuentes
    /// reutilizables. El "oído" (AudioListener) va en el técnico y no en la cámara: con cámaras fijas
    /// que cambian de golpe, el sonido no salta de un lado a otro con cada corte.
    /// </summary>
    public class AudioNexus : MonoBehaviour
    {
        static AudioNexus inst;
        BancoSonidos banco;
        AudioSource musica, tension, jefe, ambiente, alarma, interfaz;
        readonly List<AudioSource> fuentes = new List<AudioSource>();
        int siguiente;
        AudioListener oidoPropio;
        bool final;

        public static bool Activo => inst != null;
        /// <summary>Para pruebas: nombre del clip de música que suena y volúmenes de las capas.</summary>
        public static string Estado => inst == null ? "sin audio" :
            $"musica={inst.musica.clip?.name}:{inst.musica.volume:0.00} tension={inst.tension.volume:0.00} jefe={inst.jefe.volume:0.00} alarma={inst.alarma.volume:0.00} ambiente={inst.ambiente.volume:0.00}";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (inst != null) return;
            var banco = Resources.Load<BancoSonidos>("BancoSonidos");
            if (banco == null) { Debug.LogWarning("[Nexus] Falta Resources/BancoSonidos (menú Nexus/Audio/Generar sonidos)."); return; }
            var go = new GameObject("AudioNexus");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<AudioNexus>();
            inst.banco = banco;
            inst.Preparar();
        }

        AudioSource Capa(string nombre, AudioClip clip, bool bucle)
        {
            var s = new GameObject(nombre).AddComponent<AudioSource>();
            s.transform.SetParent(transform, false);
            s.clip = clip; s.loop = bucle; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f;
            s.ignoreListenerPause = true;
            return s;
        }

        void Preparar()
        {
            musica = Capa("Musica", null, true);
            tension = Capa("Tension", banco.musicaTension, true);
            jefe = Capa("Jefe", banco.musicaJefe, true);
            ambiente = Capa("Ambiente", banco.ambienteVentiladores, true);
            alarma = Capa("Alarma", banco.sirenaAlarma, true);
            interfaz = Capa("Interfaz", null, false);
            interfaz.volume = 1f;
            for (int i = 0; i < 20; i++)
            {
                var s = new GameObject("Efecto_" + i).AddComponent<AudioSource>();
                s.transform.SetParent(transform, false);
                s.playOnAwake = false; s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 2.5f; s.maxDistance = 30f; s.dopplerLevel = 0f;
                fuentes.Add(s);
            }
            SceneManager.sceneLoaded += (_, __) => AlCargarEscena();
            AlCargarEscena();
        }

        void AlCargarEscena()
        {
            final = false;
            bool menu = SceneManager.GetActiveScene().name.Contains("Menu");
            var clip = menu ? banco.musicaMenu : banco.musicaExploracion;
            if (musica.clip != clip) { musica.clip = clip; musica.volume = 0f; }
            foreach (var s in new[] { musica, tension, jefe, ambiente, alarma })
                if (s.clip != null && !s.isPlaying) s.Play();
            tension.volume = jefe.volume = alarma.volume = 0f;
            OidoEnElJugador();
        }

        /// <summary>Un solo AudioListener: en el técnico si existe; si no (menú), en este objeto.</summary>
        void OidoEnElJugador()
        {
            var j = GameObject.FindGameObjectWithTag("Player");
            AudioListener elegido;
            if (j != null)
            {
                elegido = j.GetComponent<AudioListener>();
                if (elegido == null) elegido = j.AddComponent<AudioListener>();
                elegido.enabled = true;
                if (oidoPropio != null) oidoPropio.enabled = false;
            }
            else
            {
                if (oidoPropio == null) oidoPropio = gameObject.AddComponent<AudioListener>();
                oidoPropio.enabled = true;
                elegido = oidoPropio;
            }
            foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (l != elegido) l.enabled = false;
        }

        GameObject seleccionPrevia;

        void Update()
        {
            // Navegar por los botones (menú, inventario, pantalla final) hace un pitido corto
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null && es.currentSelectedGameObject != seleccionPrevia)
            {
                if (seleccionPrevia != null && es.currentSelectedGameObject != null) Interfaz(Sfx.UIBlip, 0.3f);
                seleccionPrevia = es.currentSelectedGameObject;
            }
            if (final) { Fundir(musica, 0f, 0.4f); Fundir(tension, 0f, 0.6f); Fundir(jefe, 0f, 0.6f); Fundir(alarma, 0f, 1f); Fundir(ambiente, 0f, 0.3f); return; }
            bool menu = SceneManager.GetActiveScene().name.Contains("Menu");
            if (menu)
            {
                Fundir(musica, 0.55f, 0.5f); Fundir(tension, 0f, 1f); Fundir(jefe, 0f, 1f); Fundir(alarma, 0f, 2f); Fundir(ambiente, 0f, 1f);
                return;
            }
            bool persecucion = false;
            foreach (var a in AndroideIA.Todos)
                if (a != null && !a.Destruido && a.EstadoActual == AndroideIA.Estado.Persecucion) { persecucion = true; break; }
            var j = JefeSentinel.Instancia;
            bool combateJefe = j != null && !JefeSentinel.Derrotado && j.GetComponent<AndroideIA>().EstadoActual != AndroideIA.Estado.Inactivo;
            Fundir(musica, combateJefe ? 0.12f : 0.42f, 0.4f);
            Fundir(ambiente, 0.32f, 0.5f);
            Fundir(tension, persecucion && !combateJefe ? 0.5f : 0f, persecucion ? 1.5f : 0.4f);
            Fundir(jefe, combateJefe ? 0.6f : 0f, 1f);
            Fundir(alarma, AlarmaNivel.Activa ? 0.22f : 0f, 1.5f);
        }

        static void Fundir(AudioSource s, float objetivo, float velocidad)
        {
            if (s == null) return;
            s.volume = Mathf.MoveTowards(s.volume, objetivo, velocidad * Time.unscaledDeltaTime);
        }

        // ======================= API =======================

        /// <summary>Efecto en 3D desde un punto del mundo (o en 2D si no se da posición).</summary>
        public static void Efecto(Sfx tipo, Vector3? posicion = null, float volumen = 1f, float variacionTono = 0.06f)
        {
            if (inst == null) return;
            var clip = inst.banco.Clip(tipo);
            if (clip == null) return;
            if (posicion == null)
            {
                inst.interfaz.pitch = 1f + Random.Range(-variacionTono, variacionTono);
                inst.interfaz.PlayOneShot(clip, volumen);
                return;
            }
            var s = inst.fuentes[inst.siguiente];
            inst.siguiente = (inst.siguiente + 1) % inst.fuentes.Count;
            s.transform.position = posicion.Value;
            s.pitch = 1f + Random.Range(-variacionTono, variacionTono);
            s.clip = clip; s.volume = volumen;
            s.Play();
        }

        /// <summary>Efecto que suena un momento después (el bombeo tras el disparo, cartuchos de la recarga).</summary>
        public static void EfectoRetrasado(Sfx tipo, float segundos, Transform siguiendo, float volumen = 1f)
        {
            if (inst == null) return;
            inst.StartCoroutine(inst.Retrasado(tipo, segundos, siguiendo, volumen));
        }

        IEnumerator Retrasado(Sfx tipo, float segundos, Transform siguiendo, float volumen)
        {
            yield return new WaitForSeconds(segundos);
            Efecto(tipo, siguiendo != null ? siguiendo.position : (Vector3?)null, volumen);
        }

        /// <summary>Remate del final: victoria o game over; la música se apaga.</summary>
        public static void Final(bool victoria)
        {
            if (inst == null) return;
            inst.final = true;
            Efecto(victoria ? Sfx.Victoria : Sfx.GameOver, null, 0.9f, 0f);
        }

        public static void Interfaz(Sfx tipo, float volumen = 0.7f) => Efecto(tipo, null, volumen, 0.02f);
    }
}
