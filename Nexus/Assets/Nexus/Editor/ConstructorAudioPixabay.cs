using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Pone en el banco de sonidos la música y los efectos descargados de Pixabay (bitácora 5.4).
    ///
    /// Los archivos están en Assets/Nexus/Audio/Pixabay, ya recortados y nivelados con
    /// Evidencias/herramientas/procesar_pixabay.sh (ffmpeg) a partir de los MP3 originales:
    ///  - Px_Musica_*.ogg, Px_Ambiente_Servidores.ogg y Px_Alarma_Sirena.ogg: bucles sin corte.
    ///  - Px_&lt;Sfx&gt;.wav: un archivo por efecto, con el nombre del enum Sfx.
    ///  - Px_JugadorPaso_1..4.wav: varias pisadas que se alternan al azar.
    ///  - Px_Victoria.ogg: la pieza orquestal de la pantalla de Victoria.
    /// Si a algún efecto le falta su archivo, se queda el sintetizado por código.
    /// </summary>
    public static class ConstructorAudioPixabay
    {
        const string Carpeta = "Assets/Nexus/Audio/Pixabay";
        const string RutaBanco = "Assets/Nexus/Resources/BancoSonidos.asset";

        [MenuItem("Nexus/Audio/Usar sonidos de Pixabay")]
        public static void Usar()
        {
            var banco = AssetDatabase.LoadAssetAtPath<BancoSonidos>(RutaBanco);
            if (banco == null) { Debug.LogError("[Nexus] Falta el banco de sonidos: primero Nexus/Audio/Generar sonidos."); return; }
            if (!Directory.Exists(Carpeta)) { Debug.LogError("[Nexus] No está la carpeta " + Carpeta); return; }
            Asignar(banco);
        }

        /// <summary>Asigna los archivos de Pixabay que existan; devuelve cuántos se usaron.</summary>
        public static int Asignar(BancoSonidos banco)
        {
            if (!Directory.Exists(Carpeta)) return 0;
            int usados = 0;
            var faltan = new List<string>();

            AudioClip Cargar(string nombre, Tipo tipo)
            {
                string ruta = null;
                foreach (var ext in new[] { ".wav", ".ogg" })
                    if (File.Exists($"{Carpeta}/{nombre}{ext}")) { ruta = $"{Carpeta}/{nombre}{ext}"; break; }
                if (ruta == null) { faltan.Add(nombre); return null; }
                Importacion(ruta, tipo);
                usados++;
                return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
            }

            banco.musicaMenu = Cargar("Px_Musica_Menu", Tipo.Musica) ?? banco.musicaMenu;
            banco.musicaExploracion = Cargar("Px_Musica_Exploracion", Tipo.Musica) ?? banco.musicaExploracion;
            banco.musicaTension = Cargar("Px_Musica_Tension", Tipo.Musica) ?? banco.musicaTension;
            banco.musicaJefe = Cargar("Px_Musica_Jefe", Tipo.Musica) ?? banco.musicaJefe;
            banco.ambienteVentiladores = Cargar("Px_Ambiente_Servidores", Tipo.Ambiente) ?? banco.ambienteVentiladores;
            banco.sirenaAlarma = Cargar("Px_Alarma_Sirena", Tipo.Efecto) ?? banco.sirenaAlarma;

            var nombres = Enum.GetNames(typeof(Sfx));
            var efectos = new AudioClip[nombres.Length];
            if (banco.efectos != null) Array.Copy(banco.efectos, efectos, Mathf.Min(banco.efectos.Length, efectos.Length));
            foreach (Sfx s in Enum.GetValues(typeof(Sfx)))
            {
                if (s == Sfx.JugadorPaso) continue;   // va en variantes
                var c = Cargar("Px_" + s, s == Sfx.Victoria ? Tipo.Pieza : Tipo.Efecto);
                if (c != null) efectos[(int)s] = c;
            }
            // Deisy: si no hubiera quejido propio, usa el del técnico
            if (efectos[(int)Sfx.JugadoraHerida] == null) efectos[(int)Sfx.JugadoraHerida] = efectos[(int)Sfx.JugadorHerido];

            var pasos = new List<AudioClip>();
            for (int i = 1; i <= 8; i++)
                if (File.Exists($"{Carpeta}/Px_JugadorPaso_{i}.wav")) pasos.Add(Cargar("Px_JugadorPaso_" + i, Tipo.Efecto));
            if (pasos.Count > 0) efectos[(int)Sfx.JugadorPaso] = pasos[0];
            banco.efectos = efectos;
            banco.variantes = pasos.Count > 0
                ? new[] { new BancoSonidos.Variantes { tipo = Sfx.JugadorPaso, clips = pasos.ToArray() } }
                : new BancoSonidos.Variantes[0];
            banco.origen = "Pixabay";

            EditorUtility.SetDirty(banco);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Nexus] Sonidos de Pixabay asignados: {usados} archivos." +
                      (faltan.Count > 0 ? " Se quedan los sintetizados en: " + string.Join(", ", faltan) : ""));
            return usados;
        }

        enum Tipo { Musica, Ambiente, Pieza, Efecto }

        /// <summary>
        /// Música y ambiente: se leen del disco mientras suenan (Streaming), la música en estéreo.
        /// Victoria: comprimida en memoria (se toca con PlayOneShot). Efectos: mono y descomprimidos al
        /// cargar, para que suenen al instante y en 3D.
        /// </summary>
        static void Importacion(string ruta, Tipo tipo)
        {
            var imp = (AudioImporter)AssetImporter.GetAtPath(ruta);
            if (imp == null) { AssetDatabase.ImportAsset(ruta); imp = (AudioImporter)AssetImporter.GetAtPath(ruta); }
            bool larga = tipo == Tipo.Musica || tipo == Tipo.Ambiente;
            var a = imp.defaultSampleSettings;
            a.loadType = larga ? AudioClipLoadType.Streaming
                       : tipo == Tipo.Pieza ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            a.compressionFormat = AudioCompressionFormat.Vorbis;
            a.quality = larga || tipo == Tipo.Pieza ? 0.5f : 0.75f;
            a.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            bool mono = tipo == Tipo.Efecto || tipo == Tipo.Ambiente;
            bool cambia = imp.forceToMono != mono || imp.loadInBackground != larga
                          || imp.defaultSampleSettings.loadType != a.loadType
                          || imp.defaultSampleSettings.compressionFormat != a.compressionFormat
                          || Mathf.Abs(imp.defaultSampleSettings.quality - a.quality) > 0.001f;
            if (!cambia) return;
            imp.forceToMono = mono;
            imp.loadInBackground = larga;
            imp.defaultSampleSettings = a;
            imp.SaveAndReimport();
        }
    }
}
