using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>Tipo de sala: decide qué muebles, carteles y basura pone el decorador.</summary>
    public enum TemaSala { Pasillo, Oficina, Seguridad, Cafeteria, Almacen, Servidores, Electrico, Mantenimiento }

    /// <summary>
    /// Decorador automático de los niveles: amuebla cada sala según su tema y le da el aspecto de
    /// una empresa abandonada (muebles contra las paredes, carteles, basura en el suelo, polvo en
    /// el aire). Es determinista (misma semilla = misma decoración) para que reconstruir un nivel
    /// no lo cambie.
    ///
    /// Reglas para no romper el juego:
    ///  - Los muebles solo van pegados a las paredes, nunca en el centro de una celda (por ahí
    ///    pasan el jugador y las rutas de los androides). Lo del centro se pone a mano en el mapa
    ///    (M = mesa de cafetería, W = isla de escritorios).
    ///  - Nada junto a las puertas ni en las celdas con objetos del juego (terminales, notas...).
    ///  - Un solo mueble por celda, y nada alto cerca de una cámara fija (taparía la vista).
    ///  - La basura plana no tiene colisión: no estorba al caminar ni a la malla de navegación.
    /// </summary>
    public static partial class ConstructorNiveles
    {
        public static readonly List<Vector3> PosicionesCamaras = new List<Vector3>();
        static readonly Dictionary<char, (Vector3 pos, Vector3 adelante)> CamaraDeZona = new Dictionary<char, (Vector3, Vector3)>();

        class Mueble
        {
            public string Prefab;
            public int Peso = 1;
            public bool Alto;        // tapa la vista si queda cerca de una cámara
            public bool TapaPared;   // ocupa la pared por encima de 1,1 m (no van carteles encima)
            public bool Silla;       // lleva una silla de oficina delante
            public float Lateral = 0.3f;
            public Mueble(string p, int peso = 1) { Prefab = p; Peso = peso; }
        }

        class Tema
        {
            public float Densidad;                       // fracción de tramos de pared con mueble
            public Mueble[] Obligatorios = new Mueble[0];
            public Mueble[] Suelo = new Mueble[0];
            public string[] Pared = new string[0];
            public float ProbPared;                      // probabilidad de cartel/pizarra/gabinete por tramo libre
            public float Basura;                         // probabilidad de basura por celda
            public bool Cables;
            // Piezas pequeñas que rellenan el resto del tramo de pared junto a un mueble
            public string[] Acompanantes = { "Caneca", "Planta_Muerta", "Bolsa_Basura_0" };
        }

        /// <summary>Ancho aproximado (m) de cada mueble a lo largo de la pared, para repartir el tramo.</summary>
        static float Ancho(string prefab)
        {
            if (prefab.StartsWith("Escritorio")) return 1.4f;
            if (prefab.StartsWith("Casillero")) return 0.85f;
            if (prefab.StartsWith("Maquina")) return 1.0f;
            switch (prefab)
            {
                case "Archivador": return 0.5f;
                case "Monitores_Seguridad": return 1.95f;
                case "Meson_Cafeteria": return 1.65f;
                case "Nevera": return 0.75f;
                case "Estanteria": return 1.5f;
                case "Pila_Cajas_0": return 0.75f;
                case "Pila_Cajas_1": return 1.2f;
                case "Pila_Cajas_2": return 1.3f;
                case "Bobina_Cable": return 0.75f;
                case "Carrito_Herramientas": return 0.75f;
                case "Banco_Trabajo": return 1.8f;
                case "Conos_Precaucion": return 2.1f;
                case "Caneca": return 0.45f;
                case "Caneca_Volcada": return 0.8f;
                case "Planta_Muerta": return 0.6f;
                case "Dispensador_Agua": return 0.4f;
                case "Bolsa_Basura_0": return 0.6f;
                case "Bolsa_Basura_1": return 1.0f;
                default: return 0.8f;
            }
        }

        static Mueble Al(string p, int peso = 1, bool tapa = true) => new Mueble(p, peso) { Alto = true, TapaPared = tapa, Lateral = 0.2f };
        static Mueble Es(string p, int peso = 1) => new Mueble(p, peso) { Silla = true, Lateral = 0.1f };

        static readonly Dictionary<TemaSala, Tema> Temas = new Dictionary<TemaSala, Tema>
        {
            [TemaSala.Pasillo] = new Tema
            {
                Densidad = 0.32f, ProbPared = 0.25f, Basura = 0.55f,
                Suelo = new[] { new Mueble("Caneca", 3), new Mueble("Caneca_Volcada", 2), new Mueble("Planta_Muerta", 2), new Mueble("Dispensador_Agua"),
                                new Mueble("Bolsa_Basura_0", 2), new Mueble("Bolsa_Basura_1"), new Mueble("Pila_Cajas_0"), new Mueble("Conos_Precaucion") { Lateral = 0.1f } },
                Pared = new[] { "Cartel_Nexus", "Cartel_Seguridad", "Cartel_Empleado" },
            },
            [TemaSala.Oficina] = new Tema
            {
                Densidad = 0.7f, ProbPared = 0.35f, Basura = 0.6f, Acompanantes = new[] { "Planta_Muerta", "Caneca", "Dispensador_Agua" },
                Suelo = new[] { Es("Escritorio_PC", 3), Es("Escritorio_PC_Codigo", 2), Es("Escritorio_PC_Apagado", 2), new Mueble("Escritorio_Desordenado", 2) { Lateral = 0.1f },
                                new Mueble("Archivador", 2) { TapaPared = true }, Al("Casillero"), new Mueble("Planta_Muerta"), new Mueble("Dispensador_Agua"), new Mueble("Caneca") },
                Pared = new[] { "Pizarra", "Pizarra", "Cartel_Nexus", "Cartel_Empleado" },
            },
            [TemaSala.Seguridad] = new Tema
            {
                Densidad = 0.7f, ProbPared = 0.35f, Basura = 0.5f,
                Obligatorios = new[] { new Mueble("Monitores_Seguridad") { TapaPared = true, Lateral = 0f, Silla = true } },
                Suelo = new[] { Al("Casillero", 2), Al("Casillero_Abierto", 2), new Mueble("Archivador", 2) { TapaPared = true }, Es("Escritorio_PC_Apagado"), new Mueble("Caneca_Volcada") },
                Pared = new[] { "Cartel_Seguridad", "Cartel_Seguridad", "Pizarra" },
            },
            [TemaSala.Cafeteria] = new Tema
            {
                Densidad = 0.65f, ProbPared = 0.35f, Basura = 0.7f,
                Obligatorios = new[] { new Mueble("Meson_Cafeteria") { Lateral = 0.1f }, Al("Nevera") },
                Suelo = new[] { Al("Maquina_Rota"), new Mueble("Dispensador_Agua"), new Mueble("Planta_Muerta"), new Mueble("Caneca"), new Mueble("Caneca_Volcada"), new Mueble("Bolsa_Basura_1") },
                Pared = new[] { "Cartel_Empleado", "Cartel_Nexus" },
            },
            [TemaSala.Almacen] = new Tema
            {
                Densidad = 0.9f, ProbPared = 0f, Basura = 0.4f, Acompanantes = new[] { "Pila_Cajas_0", "Bolsa_Basura_0" },
                Suelo = new[] { Al("Estanteria", 4), new Mueble("Pila_Cajas_0", 2), new Mueble("Pila_Cajas_1", 2), new Mueble("Pila_Cajas_2", 2), new Mueble("Bobina_Cable"), new Mueble("Carrito_Herramientas") },
            },
            [TemaSala.Servidores] = new Tema
            {
                Densidad = 0.5f, ProbPared = 0.3f, Basura = 0.4f, Cables = true, Acompanantes = new[] { "Caneca", "Bobina_Cable" },
                Suelo = new[] { new Mueble("Bobina_Cable", 2), new Mueble("Carrito_Herramientas"), new Mueble("Pila_Cajas_1"), new Mueble("Conos_Precaucion") { Lateral = 0.1f }, new Mueble("Caneca") },
                Pared = new[] { "Gabinete_Electrico", "Cartel_Seguridad" },
            },
            [TemaSala.Electrico] = new Tema
            {
                Densidad = 0.6f, ProbPared = 0.55f, Basura = 0.4f, Cables = true, Acompanantes = new[] { "Caneca", "Bobina_Cable" },
                Suelo = new[] { new Mueble("Bobina_Cable", 2), new Mueble("Carrito_Herramientas"), new Mueble("Pila_Cajas_0"), new Mueble("Conos_Precaucion") { Lateral = 0.1f } },
                Pared = new[] { "Gabinete_Electrico", "Gabinete_Electrico", "Gabinete_Electrico_Abierto", "Cartel_Seguridad" },
            },
            [TemaSala.Mantenimiento] = new Tema
            {
                Densidad = 0.75f, ProbPared = 0.35f, Basura = 0.5f, Cables = true, Acompanantes = new[] { "Caneca", "Bobina_Cable", "Bolsa_Basura_0" },
                Suelo = new[] { new Mueble("Banco_Trabajo", 3) { TapaPared = true, Lateral = 0.1f }, new Mueble("Carrito_Herramientas", 2), Al("Casillero"), Al("Casillero_Abierto"),
                                Al("Estanteria"), new Mueble("Bobina_Cable"), new Mueble("Pila_Cajas_2") },
                Pared = new[] { "Pizarra", "Cartel_Seguridad", "Gabinete_Electrico" },
            },
        };

        static readonly (string prefab, int peso)[] BasuraPlana =
        {
            ("Basura_Papeles_0", 4), ("Basura_Papeles_1", 3), ("Basura_Papeles_2", 2),
            ("Basura_Latas_0", 2), ("Basura_Latas_1", 2), ("Mancha", 3), ("Charco", 1), ("Escombros_0", 1), ("Escombros_1", 1),
        };

        static bool EsPuerta(char ch) => ch == 'R' || ch == 'B' || ch == 'X' || ch == 'L' || ch == 'I';
        /// <summary>Celda libre, inicio o con un objeto que flota en el centro (tarjeta, fusible, disco...).</summary>
        static bool Amueblable(char ch) => ch == '.' || ch == 'S' || ch == 'D' || "kafumcgCHZhz".IndexOf(ch) >= 0;
        static bool JuntoAPuerta(Vector2Int c) => Dirs.Any(d => EsPuerta(En(c + d)));
        static Vector3 Perpendicular(Vector2Int d) => new Vector3(d.y, 0f, -d.x);

        static T Elegir<T>(IList<T> lista, System.Func<T, int> peso, System.Random rnd)
        {
            int total = lista.Sum(peso);
            int r = rnd.Next(total);
            foreach (var x in lista) { r -= peso(x); if (r < 0) return x; }
            return lista[lista.Count - 1];
        }

        static void Decorar(Transform raiz)
        {
            var deco = Grupo("Decoracion", raiz);
            int semilla = def.Escena.Aggregate(17, (h, ch) => h * 31 + ch);
            var rnd = new System.Random(semilla);
            var polvo = AssetDatabase.LoadAssetAtPath<GameObject>(Pre + "Efectos/FX_Polvo.prefab");
            int muebles = 0, basura = 0;

            foreach (var kv in def.Salas)
            {
                var tema = Temas[kv.Value.Tema];
                var grupo = Grupo("Deco_" + kv.Value.Nombre, deco);
                var celdas = CeldasDeZona(kv.Key);
                if (celdas.Count == 0) continue;

                // Tramos de pared disponibles: celdas libres o con un objeto flotante en el centro
                // (los objetos de pared del juego —terminales, notas, paneles— ocupan su celda).
                // Primero las paredes que ve la cámara de la sala; la pared que queda justo detrás
                // de la cámara no se decora (nunca se vería).
                var vista = CamaraDeZona.TryGetValue(kv.Key, out var cz) ? new Vector3(cz.adelante.x, 0f, cz.adelante.z).normalized : Vector3.zero;
                var tramos = new List<(Vector2Int c, Vector2Int d)>();
                foreach (var c in celdas)
                {
                    if (!Amueblable(En(c))) continue;
                    foreach (var d in Dirs)
                        if (Solido(En(c + d)) && Vector3.Dot(Dir3(d), vista) > -0.6f) tramos.Add((c, d));
                }
                tramos = tramos.OrderBy(t => Vector3.Dot(Dir3(t.d), vista) < -0.2f ? 1 : 0).ThenBy(_ => rnd.Next()).ToList();
                var celdaOcupada = new HashSet<Vector2Int>();
                var paredTapada = new HashSet<(Vector2Int, Vector2Int)>();

                bool Colocar(Mueble m, (Vector2Int c, Vector2Int d) t)
                {
                    Vector3 pos = Centro(t.c) + Dir3(t.d) * (Celda / 2f - 0.1f);
                    if (m.Alto && PosicionesCamaras.Any(cam => Vector2.Distance(new Vector2(cam.x, cam.z), new Vector2(pos.x, pos.z)) < 2.6f)) return false;
                    if (m.Alto && JuntoAPuerta(t.c)) return false;   // nada alto pegado a una puerta
                    Vector3 pared = pos;
                    float o = ((float)rnd.NextDouble() * 2f - 1f) * m.Lateral;
                    pos += Perpendicular(t.d) * o;
                    var go = Poner("Utileria/" + m.Prefab, pos, Yaw(t.d), grupo);
                    if (go == null) return false;

                    // Acompañante: algo pequeño en el hueco de pared que queda al lado del mueble
                    float w = Ancho(m.Prefab), izq = (o - w / 2f) + 1.4f, der = 1.4f - (o + w / 2f);
                    float hueco = Mathf.Max(izq, der);
                    var opciones = tema.Acompanantes.Where(a => Ancho(a) + 0.15f <= hueco).ToList();
                    if (opciones.Count > 0 && rnd.NextDouble() < 0.7)
                    {
                        float centro = izq > der ? -1.4f + izq / 2f : 1.4f - der / 2f;
                        if (Poner("Utileria/" + opciones[rnd.Next(opciones.Count)], pared + Perpendicular(t.d) * centro, Yaw(t.d), grupo) != null) muebles++;
                    }
                    if (m.Silla)
                    {
                        // Silla de oficina delante, mirando al escritorio; a veces corrida o volcada
                        bool volcada = rnd.NextDouble() < 0.25;
                        var silla = Poner("Utileria/Silla_Oficina", pos - Dir3(t.d) * (volcada ? 1.05f : 0.85f) + Perpendicular(t.d) * (((float)rnd.NextDouble() - 0.5f) * 0.5f),
                                          Yaw(t.d) + rnd.Next(-35, 35), grupo);
                        if (volcada && silla != null)
                        {
                            silla.transform.rotation *= Quaternion.Euler(0f, 0f, 90f);
                            silla.transform.position += Vector3.up * 0.28f;
                        }
                    }
                    celdaOcupada.Add(t.c);
                    if (m.TapaPared || m.Alto) paredTapada.Add(t);
                    muebles++;
                    return true;
                }

                // 1) Muebles obligatorios del tema (los monitores en Seguridad, el mesón en la cafetería...)
                foreach (var m in tema.Obligatorios)
                    foreach (var t in tramos)
                        if (!celdaOcupada.Contains(t.c) && Colocar(m, t)) break;

                // 2) Muebles contra las paredes hasta llegar a la densidad del tema
                int objetivo = Mathf.RoundToInt(tramos.Count * tema.Densidad);
                if (tema.Suelo.Length > 0)
                    foreach (var t in tramos)
                    {
                        if (celdaOcupada.Count >= objetivo) break;
                        if (celdaOcupada.Contains(t.c)) continue;
                        var m = Elegir(tema.Suelo, x => x.Peso, rnd);
                        if (!Colocar(m, t) && m.Alto)
                        {
                            var bajos = tema.Suelo.Where(x => !x.Alto).ToList();
                            if (bajos.Count > 0) Colocar(Elegir(bajos, x => x.Peso, rnd), t);
                        }
                    }

                // 3) Carteles, pizarras y gabinetes en paredes libres
                if (tema.Pared.Length > 0)
                    foreach (var t in tramos)
                    {
                        if (paredTapada.Contains(t) || rnd.NextDouble() > tema.ProbPared) continue;
                        string p = tema.Pared[rnd.Next(tema.Pared.Length)];
                        if (p.StartsWith("Gabinete") && celdaOcupada.Contains(t.c)) continue;   // el gabinete baja hasta 0,8 m
                        Vector3 pos = Centro(t.c) + Dir3(t.d) * (Celda / 2f - 0.1f) + Perpendicular(t.d) * (((float)rnd.NextDouble() * 2f - 1f) * 0.6f);
                        if (Poner("Utileria/" + p, pos, Yaw(t.d), grupo) != null) { paredTapada.Add(t); muebles++; }
                    }

                // 4) Basura en el suelo (papeles, latas, manchas, escombros) y cables sueltos
                foreach (var c in celdas)
                {
                    char ch = En(c);
                    if (EsPuerta(ch) || (ch != '.' && ch != 'S' && ch != 'M' && ch != 'W' && ch != 'b')) continue;
                    int n = rnd.NextDouble() < tema.Basura ? (rnd.NextDouble() < 0.3 ? 2 : 1) : 0;
                    for (int i = 0; i < n; i++)
                    {
                        var (prefab, _) = Elegir(BasuraPlana, x => x.peso, rnd);
                        var off = new Vector3((float)rnd.NextDouble() * 2f - 1f, 0f, (float)rnd.NextDouble() * 2f - 1f) * 0.95f;
                        if (Poner("Utileria/" + prefab, Centro(c) + off, rnd.Next(0, 360), grupo) != null) basura++;
                    }
                    if (tema.Cables && rnd.NextDouble() < 0.18)
                    {
                        var muro = Dirs.Where(d => Solido(En(c + d))).ToList();
                        if (muro.Count > 0)
                        {
                            var d = muro[rnd.Next(muro.Count)];
                            Poner("Utileria/Cables_Suelo_" + rnd.Next(2), Centro(c) + Dir3(d) * (Celda / 2f - 0.12f), Yaw(d), grupo);
                        }
                    }
                }

                // 5) Polvo flotando en el aire, del tamaño de la sala
                if (polvo != null)
                {
                    float xMin = celdas.Min(c => c.x) * Celda, xMax = (celdas.Max(c => c.x) + 1) * Celda;
                    float zMin = celdas.Min(c => c.y) * Celda, zMax = (celdas.Max(c => c.y) + 1) * Celda;
                    var fx = (GameObject)PrefabUtility.InstantiatePrefab(polvo, grupo);
                    fx.transform.position = new Vector3((xMin + xMax) / 2f, 1.3f, (zMin + zMax) / 2f);
                    var ps = fx.GetComponent<ParticleSystem>();
                    var forma = ps.shape;
                    forma.scale = new Vector3(xMax - xMin, 2.4f, zMax - zMin);
                    float area = celdas.Count * Celda * Celda;
                    var main = ps.main;
                    main.maxParticles = Mathf.Clamp(Mathf.RoundToInt(area * 0.6f), 15, 90);
                    var em = ps.emission;
                    em.rateOverTime = Mathf.Clamp(area / 14f, 1.5f, 9f);
                }
            }
            Debug.Log($"[Nexus] Decoración: {muebles} muebles y carteles, {basura} grupos de basura.");
        }
    }
}
