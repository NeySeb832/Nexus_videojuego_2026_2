using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus
{
    /// <summary>Música y efectos. Los clips se cargan por nombre desde Assets/Resources/Audio.</summary>
    public class GestorAudio : MonoBehaviour
    {
        static GestorAudio instancia;
        AudioSource ambiente, tension, efectos, alarma;
        readonly Dictionary<string, AudioClip[]> cache = new Dictionary<string, AudioClip[]>();
        readonly HashSet<Object> perseguidores = new HashSet<Object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (instancia != null) return;
            var go = new GameObject("GestorAudio");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<GestorAudio>();
            instancia.AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        AudioSource Fuente(string nombre, bool bucle)
        {
            var s = new GameObject(nombre).AddComponent<AudioSource>();
            s.transform.SetParent(transform, false);
            s.loop = bucle; s.playOnAwake = false; s.spatialBlend = 0f;
            return s;
        }

        void Awake()
        {
            ambiente = Fuente("Ambiente", true);
            tension = Fuente("Persecucion", true);
            efectos = Fuente("Efectos", false);
            alarma = Fuente("Alarma", true);
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        void OnDestroy() => SceneManager.sceneLoaded -= AlCargarEscena;

        void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            perseguidores.Clear();
            alarma.Stop();
            string pista = escena.name == "00_Menu" ? "musica_menu" : "musica_nivel";
            Poner(ambiente, Clip(pista));
            Poner(tension, escena.name == "00_Menu" ? null : Clip("musica_persecucion"));
        }

        static void Poner(AudioSource fuente, AudioClip clip)
        {
            if (clip == null) { fuente.Stop(); return; }
            if (fuente.clip == clip && fuente.isPlaying) return;
            fuente.clip = clip; fuente.volume = 0f; fuente.Play();
        }

        void Update()
        {
            // Fundido entre la música de ambiente y la de persecución (funciona con el juego en pausa)
            float dt = Time.unscaledDeltaTime;
            perseguidores.RemoveWhere(o => o == null);
            bool persecucion = perseguidores.Count > 0;
            ambiente.volume = Mathf.MoveTowards(ambiente.volume, persecucion ? 0.15f : 0.6f, dt * 0.5f);
            tension.volume = Mathf.MoveTowards(tension.volume, persecucion ? 0.7f : 0f, dt * 0.8f);
        }

        AudioClip Clip(string nombre)
        {
            if (!cache.TryGetValue(nombre, out var clips))
            {
                var uno = Resources.Load<AudioClip>("Audio/" + nombre);
                clips = uno != null ? new[] { uno } : Resources.LoadAll<AudioClip>("Audio/" + nombre);
                cache[nombre] = clips;
            }
            return clips.Length == 0 ? null : clips[Random.Range(0, clips.Length)];
        }

        // ---------- API estática: se llama desde cualquier script ----------
        public static AudioClip Obtener(string nombre) => instancia != null ? instancia.Clip(nombre) : null;

        public static void Sfx(string nombre, float volumen = 1f)
        {
            if (instancia == null) return;
            var c = instancia.Clip(nombre);
            if (c != null) instancia.efectos.PlayOneShot(c, volumen);
        }

        /// <summary>Sonido 3D en un punto del mundo (por ejemplo, la máquina expendedora).</summary>
        public static void SfxEn(string nombre, Vector3 pos, float volumen = 1f)
        {
            if (instancia == null) return;
            var c = instancia.Clip(nombre);
            if (c != null) AudioSource.PlayClipAtPoint(c, pos, volumen);
        }

        /// <summary>
        /// Cada androide avisa cuando empieza o deja de perseguir. La música de persecución
        /// suena mientras al menos uno lo haga. "quien" es el propio androide (this).
        /// </summary>
        public static void Perseguir(Object quien, bool activo)
        {
            if (instancia == null || quien == null) return;
            if (activo) instancia.perseguidores.Add(quien); else instancia.perseguidores.Remove(quien);
        }

        public static void Alarma(bool activa)
        {
            if (instancia == null) return;
            if (!activa) { instancia.alarma.Stop(); return; }
            var clip = instancia.Clip("alarma");
            if (clip == null) return;
            instancia.alarma.clip = clip;
            instancia.alarma.volume = 0.5f;
            instancia.alarma.Play();
        }
    }
}