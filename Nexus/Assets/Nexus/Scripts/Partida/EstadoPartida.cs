using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Datos de la partida que sobreviven al cambio de nivel: personaje elegido, salud,
    /// discos recuperados, inventario y notas leídas. También guarda y carga la partida
    /// (en PlayerPrefs, que funciona igual en Windows, WebGL y Android).
    /// </summary>
    public static class EstadoPartida
    {
        public const int SaludMaxima = 3;
        public const int DiscosTotales = 3;
        const string ClaveGuardado = "Nexus_Guardado";

        public static int PersonajeElegido = 0;      // 0 = Neyder, 1 = Deisy
        public static int Salud = SaludMaxima;
        public static int Discos = 0;
        public static int Refrigerantes = 0;          // total recogidos (coleccionable con contador)
        public static readonly Dictionary<TipoColeccionable, int> Inventario = new Dictionary<TipoColeccionable, int>();
        public static readonly List<string> NotasLeidas = new List<string>();
        // Armas: la que lleva en la mano (TipoArma) y las balas que quedan en cada cargador
        public static int ArmaEquipada = 0;
        public static int CargadorPistola = 0, CargadorEscopeta = 0;

        /// <summary>Si no es null, el nivel debe colocar al jugador en esta posición (partida cargada).</summary>
        public static Vector3? PosicionCargada;
        public static float? TiempoCargado;

        public static event Action AlCambiar;
        public static void Notificar() => AlCambiar?.Invoke();

        public static string NombrePersonaje => PersonajeElegido == 0 ? "NEYDER" : "DEISY";

        /// <summary>
        /// Reinicia los datos al entrar en modo Play. Si el editor tiene desactivada la recarga de
        /// dominio, los campos estáticos conservarían los valores de la prueba anterior.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarAlArrancar()
        {
            PersonajeElegido = 0; Salud = SaludMaxima; Discos = 0; Refrigerantes = 0;
            Inventario.Clear(); NotasLeidas.Clear();
            PosicionCargada = null; TiempoCargado = null;
            ArmaEquipada = 0; CargadorPistola = 0; CargadorEscopeta = 0;
            AlCambiar = null; inicioNivel = null;
        }

        public static void NuevaPartida(int personaje)
        {
            PersonajeElegido = personaje;
            ArmaEquipada = 0; CargadorPistola = 0; CargadorEscopeta = 0;
            Salud = SaludMaxima;
            Discos = 0;
            Refrigerantes = 0;
            Inventario.Clear();
            NotasLeidas.Clear();
            PosicionCargada = null;
            TiempoCargado = null;
            Notificar();
        }

        public static int Cantidad(TipoColeccionable t) => Inventario.TryGetValue(t, out int n) ? n : 0;

        // ---------- Instantánea al empezar un nivel (para "Reintentar nivel") ----------
        [Serializable]
        class Datos
        {
            public string escena;
            public int personaje, salud, discos, refrigerantes;
            public int arma, cargadorPistola, cargadorEscopeta;
            public List<int> tipos = new List<int>();
            public List<int> cantidades = new List<int>();
            public List<string> notas = new List<string>();
            public bool conPosicion;
            public Vector3 posicion;
            public float tiempo;
        }

        static Datos inicioNivel;

        static Datos Capturar(string escena)
        {
            var d = new Datos { escena = escena, personaje = PersonajeElegido, salud = Salud, discos = Discos, refrigerantes = Refrigerantes,
                              arma = ArmaEquipada, cargadorPistola = CargadorPistola, cargadorEscopeta = CargadorEscopeta };
            foreach (var kv in Inventario) { d.tipos.Add((int)kv.Key); d.cantidades.Add(kv.Value); }
            d.notas.AddRange(NotasLeidas);
            return d;
        }

        static void Aplicar(Datos d)
        {
            PersonajeElegido = d.personaje; Salud = d.salud; Discos = d.discos; Refrigerantes = d.refrigerantes;
            ArmaEquipada = d.arma; CargadorPistola = d.cargadorPistola; CargadorEscopeta = d.cargadorEscopeta;
            Inventario.Clear();
            for (int i = 0; i < d.tipos.Count; i++) Inventario[(TipoColeccionable)d.tipos[i]] = d.cantidades[i];
            NotasLeidas.Clear(); NotasLeidas.AddRange(d.notas);
            Notificar();
        }

        public static void GuardarInicioNivel(string escena) => inicioNivel = Capturar(escena);

        public static void RestaurarInicioNivel()
        {
            if (inicioNivel != null) Aplicar(inicioNivel);
            PosicionCargada = null; TiempoCargado = null;
        }

        // ---------- Guardado con USB ----------
        public static void Guardar(string escena, Vector3 posicion, float tiempoRestante)
        {
            var d = Capturar(escena);
            d.conPosicion = true; d.posicion = posicion; d.tiempo = tiempoRestante;
            PlayerPrefs.SetString(ClaveGuardado, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }

        public static bool HayGuardado => PlayerPrefs.HasKey(ClaveGuardado);

        /// <summary>Carga la partida guardada y devuelve la escena en la que se guardó.</summary>
        public static string Cargar()
        {
            if (!HayGuardado) return null;
            var d = JsonUtility.FromJson<Datos>(PlayerPrefs.GetString(ClaveGuardado));
            Aplicar(d);
            PosicionCargada = d.conPosicion ? d.posicion : (Vector3?)null;
            TiempoCargado = d.conPosicion ? d.tiempo : (float?)null;
            return d.escena;
        }
    }
}
