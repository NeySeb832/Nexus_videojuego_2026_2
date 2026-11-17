using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nexus.Herramientas
{
    /// <summary>Configuración de una sala (zona de cámara): luz y esquina donde va su cámara fija.</summary>
    public class DefZona
    {
        public string Nombre;
        public string Esquina = "NE";          // NE, NO, SE, SO
        public Color Luz = new Color(0.7f, 0.8f, 1f);
        public float Intensidad = 8f;
        public bool SinEnergia;                 // luces apagadas hasta reparar el panel de fusibles
        public TemaSala Tema = TemaSala.Pasillo; // define los muebles y la basura de la sala
        public bool Parpadeo;                   // fluorescentes en mal estado que titilan
        public float AlturaCamara = 2.85f;
        // Posición manual de la cámara, en coordenadas del mapa de texto (columna, fila).
        // (6, 12) es la esquina superior izquierda de la celda de la columna 6, fila 12.
        public Vector2? Camara;
        public Vector2? Mirar;
    }

    /// <summary>Un androide: ruta de patrulla en coordenadas del mapa (columna, fila de la celda) y velocidades.</summary>
    public class DefAndroide
    {
        public Vector2Int[] Ruta;
        public float VelPatrulla = 1.5f, VelPersecucion = 4.0f;
        public bool ConAlarma;      // apagado hasta que se extrae el Disco Maestro
    }

    /// <summary>Definición completa de un nivel: mapa, salas y textos.</summary>
    public class DefNivel
    {
        public List<DefAndroide> Androides = new List<DefAndroide>();
        public string Escena, Nombre, ObjetivoInicial, EscenaSiguiente;
        public float Tiempo = 420f, TemperaturaInicial = 80f;
        public string[] Mapa, Zonas;
        public Dictionary<char, DefZona> Salas = new Dictionary<char, DefZona>();
        public List<(string titulo, string[] mensajes, string objetivo)> Terminales = new List<(string, string[], string)>();
        public List<(string titulo, string texto, string objetivo)> Notas = new List<(string, string, string)>();
        public Dictionary<char, string> ObjetivoAlRecoger = new Dictionary<char, string>();
        public string[] DialogoDisco;
        public string ObjetivoPanel;
        public string TextoSalida = "Usar montacargas";
        public float Niebla = 0.03f;
        public Color Ambiente = new Color(0.15f, 0.16f, 0.2f);
        public List<DefCadaver> Cadaveres = new List<DefCadaver>();   // 'K' del mapa, en orden de lectura
        public bool Intro;                                             // cinemática de entrada por la puerta 'I'
        public float VelAlarma = 3.6f;                                 // velocidad de caza de los androides de las estaciones 'Q'
    }

    /// <summary>
    /// Construye un nivel a partir de su mapa de texto (cada carácter = celda de 3 x 3 m).
    ///
    /// Mapa:  #  muro           .  suelo          =  fila de racks (X)   |  fila de racks (Z)
    ///        S  inicio         T  terminal       G  terminal de guardado
    ///        P  panel fusibles n  nota           D  Disco Duro en pedestal
    ///        R  puerta roja    B  puerta azul    X  puerta sin energía  L  puerta libre
    ///        E  montacargas    A  androide
    ///        k  tarjeta roja   a  tarjeta azul   f  fusible   u  USB   m  Speedmax
    ///        c  Chocorramo     g  cartucho de refrigerante
    ///        V  máquina de bebidas (Speedmax)    Y  máquina de snacks (Chocorramo)
    ///        M  mesa de cafetería con sillas     W  isla de escritorios     b  estiba de cajas
    ///        C  cuchillo   H  pistola   Z  escopeta   h  balas de pistola (12)   z  cartuchos (4)
    ///        K  cadáver que se registra (ver DefNivel.Cadaveres: lleva un arma o munición y una nota)
    ///        Q  estación de carga con un androide anclado que despierta con la alarma del disco
    ///        I  puerta de servicio por la que entran los técnicos (cinemática de entrada; luego se sella)
    /// Zonas: cada letra agrupa celdas en una sala con su cámara fija y sus luces.
    /// El resto de muebles, basura y efectos los pone el decorador según el tema de cada sala
    /// (ver ConstructorNiveles_Decoracion.cs).
    /// </summary>
    public static partial class ConstructorNiveles
    {
        const float Celda = 3f;
        const string Pre = "Assets/Nexus/Prefabs/";
        static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left }; // N, E, S, O

        static DefNivel def;
        static int ancho, alto;

        public static readonly List<Vector3> SpawnsAndroides = new List<Vector3>();

        // ---------- Utilidades del mapa ----------
        static char En(Vector2Int c) => (c.x < 0 || c.y < 0 || c.x >= ancho || c.y >= alto) ? '#' : def.Mapa[alto - 1 - c.y][c.x];
        static char ZonaDe(Vector2Int c) => (c.x < 0 || c.y < 0 || c.x >= ancho || c.y >= alto) ? '.' : def.Zonas[alto - 1 - c.y][c.x];
        static bool Solido(char ch) => ch == '#';
        static bool Racks(char ch) => ch == '=' || ch == '|';
        static bool Caminable(char ch) => !Solido(ch) && !Racks(ch);
        static Vector3 Centro(Vector2Int c) => new Vector3(c.x * Celda + Celda / 2f, 0f, c.y * Celda + Celda / 2f);
        static Vector3 Dir3(Vector2Int d) => new Vector3(d.x, 0f, d.y);
        /// <summary>Convierte (columna, fila) del mapa de texto a una posición en el mundo.</summary>
        static Vector3 DesdeTexto(Vector2 colFila) => new Vector3(colFila.x * Celda, 0f, (alto - colFila.y) * Celda);
        static float Yaw(Vector2Int d) => Quaternion.LookRotation(Dir3(d)).eulerAngles.y;

        static GameObject Poner(string prefab, Vector3 pos, float rotY, Transform padre)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Pre + prefab + ".prefab");
            if (asset == null) { Debug.LogError("[Nexus] Falta el prefab " + prefab); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, padre);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
            return go;
        }

        static Transform Grupo(string nombre, Transform padre)
        {
            var t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        /// <summary>Primera dirección (N, E, S, O) donde hay un muro: para apoyar objetos en la pared.</summary>
        static Vector2Int? MuroVecino(Vector2Int c)
        {
            foreach (var d in Dirs) if (Solido(En(c + d))) return d;
            return null;
        }

        // ==================== Construcción ====================
        public static void Construir(DefNivel nivel)
        {
            def = nivel;
            alto = def.Mapa.Length;
            ancho = def.Mapa[0].Length;
            SpawnsAndroides.Clear();
            PuntosInteres.Clear();
            ReiniciarHistoria();
            // Si hay androides que despierta la alarma, la extracción del disco se cuenta con una cinemática
            conCinematicaAlarma = def.Mapa.Any(f => f.Contains('Q')) || def.Androides.Any(a => a.ConAlarma);

            var escena = EditorSceneManager.OpenScene($"Assets/Nexus/Escenas/{def.Escena}.unity", OpenSceneMode.Single);
            foreach (var go in escena.GetRootGameObjects()) Object.DestroyImmediate(go);

            var raiz = new GameObject("Nivel");
            Transform geo = Grupo("Geometria", raiz.transform), racks = Grupo("Racks", raiz.transform),
                      objetos = Grupo("Objetos", raiz.transform), luces = Grupo("Iluminacion", raiz.transform),
                      zonas = Grupo("Zonas_Camara", raiz.transform);

            var puertasSinEnergia = new List<PuertaSeguridad>();
            var lucesSinEnergia = new List<GameObject>();
            var paneles = new List<PanelFusibles>();
            int iTerminal = 0, iNota = 0;
            Vector3 inicio = Vector3.zero; float rotInicio = 0f;

            // Se recorre en orden de lectura (de arriba abajo, de izquierda a derecha) para que las
            // terminales y notas reciban sus textos en el mismo orden en que aparecen en el mapa
            for (int fila = 0; fila < alto; fila++)
                for (int x = 0; x < ancho; x++)
                {
                    int y = alto - 1 - fila;
                    var c = new Vector2Int(x, y);
                    char ch = En(c);
                    if (Solido(ch)) continue;
                    Vector3 p = Centro(c);

                    // Suelo y paredes hacia las celdas sólidas
                    Poner("Entorno/Modulo_Piso_3x3", p, 0f, geo);
                    foreach (var d in Dirs)
                        if (Solido(En(c + d)))
                            Poner("Entorno/Modulo_Pared_3x3", p + Dir3(d) * (Celda / 2f), d.x != 0 ? 90f : 0f, geo);

                    switch (ch)
                    {
                        case '=': Poner("Entorno/Fila_Racks", p, 0f, racks); break;
                        case '|': Poner("Entorno/Fila_Racks", p, 90f, racks); break;
                        case 'S':
                            inicio = p;
                            foreach (var d in Dirs) if (Caminable(En(c + d))) { rotInicio = Yaw(d); break; }
                            break;
                        case 'R': case 'B': case 'X': case 'L':
                        {
                            bool ejeX = Caminable(En(c + Vector2Int.up)) && Caminable(En(c + Vector2Int.down));
                            float rot = ejeX ? 0f : 90f;
                            string pf = ch == 'R' ? "Puerta_Seguridad_Roja" : ch == 'B' ? "Puerta_Seguridad_Azul" : "Puerta_Seguridad_Libre";
                            var puerta = Poner("Interactivos/" + pf, p, rot, objetos);
                            foreach (var dv in ejeX ? new[] { Vector2Int.up, Vector2Int.down } : new[] { Vector2Int.left, Vector2Int.right })
                                // Plano cercano solo en puertas con cerradura (hay que ver el lector); las libres se ven bien
                                if (ch != 'L' && Caminable(En(c + dv)) && !"RBXL".Contains(En(c + dv))) RegistrarInteres(p, Dir3(dv), c + dv, puerta, 1.3f, 3.2f);
                            if (ch == 'X')
                            {
                                var ps = puerta.GetComponent<PuertaSeguridad>();
                                var so = new SerializedObject(ps);
                                so.FindProperty("requiereEnergia").boolValue = true;
                                so.ApplyModifiedPropertiesWithoutUndo();
                                puertasSinEnergia.Add(ps);
                            }
                            // Rellenos de pared a los lados del marco
                            Vector3 lado = ejeX ? Vector3.right : Vector3.forward;
                            foreach (int s in new[] { -1, 1 })
                            {
                                var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                r.name = "Relleno_Puerta";
                                r.transform.SetParent(geo, false);
                                r.transform.position = p + lado * (s * 1.375f) + Vector3.up * 1.5f;
                                r.transform.rotation = Quaternion.Euler(0f, rot, 0f);
                                r.transform.localScale = new Vector3(0.25f, 3f, 0.45f);
                                r.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Nexus/Materiales/Entorno/M_Pared.mat");
                            }
                            break;
                        }
                        case 'T':
                        {
                            var d = MuroVecino(c) ?? Vector2Int.up;
                            var t = Poner("Interactivos/Terminal_Nexus", p + Dir3(d) * (Celda / 2f - 0.45f), Yaw(d), objetos);
                            RegistrarInteres(t.transform.position, -Dir3(d), c, t, 1.1f, 2.9f);
                            if (iTerminal < def.Terminales.Count)
                            {
                                var (titulo, mensajes, objetivo) = def.Terminales[iTerminal++];
                                var term = t.GetComponent<TerminalNexus>();
                                term.Configurar(titulo, mensajes, objetivo);
                                EditorUtility.SetDirty(term);
                            }
                            break;
                        }
                        case 'G':
                        {
                            var d = MuroVecino(c) ?? Vector2Int.up;
                            var tr = Poner("Interactivos/Terminal_Respaldo", p + Dir3(d) * (Celda / 2f - 0.35f), Yaw(d), objetos);
                            RegistrarInteres(tr.transform.position, -Dir3(d), c, tr, 1.1f, 2.9f);
                            break;
                        }
                        case 'P':
                        {
                            var d = MuroVecino(c) ?? Vector2Int.up;
                            var panel = Poner("Interactivos/Panel_Fusibles", p + Dir3(d) * (Celda / 2f - 0.1f), Yaw(d), objetos);
                            paneles.Add(panel.GetComponent<PanelFusibles>());
                            RegistrarInteres(panel.transform.position, -Dir3(d), c, panel, 1.3f, 2.9f);
                            break;
                        }
                        case 'E':
                        {
                            var d = MuroVecino(c) ?? Vector2Int.up;
                            var salida = Poner("Interactivos/Montacargas_Salida", p + Dir3(d) * (Celda / 2f - 0.12f), Yaw(d), objetos);
                            RegistrarInteres(salida.transform.position, -Dir3(d), c, salida, 1.3f, 3.2f);
                            var s = salida.GetComponent<SalidaNivel>();
                            s.Configurar(def.TextoSalida, "No responde. Primero extrae el Disco Duro Maestro de este nivel.");
                            EditorUtility.SetDirty(s);
                            break;
                        }
                        case 'n':
                        {
                            var d = MuroVecino(c);
                            Vector3 pos = d.HasValue ? p + Dir3(d.Value) * 0.9f : p;
                            var nota = Poner("Interactivos/Nota_Mesa", pos, d.HasValue ? Yaw(d.Value) : 0f, objetos);
                            RegistrarInteres(pos, d.HasValue ? -Dir3(d.Value) : Vector3.zero, c, nota, 0.8f, 2.7f);
                            if (iNota < def.Notas.Count)
                            {
                                var (titulo, texto, objetivo) = def.Notas[iNota++];
                                var nl = nota.GetComponent<NotaLore>();
                                nl.Configurar(titulo, texto, objetivo);
                                EditorUtility.SetDirty(nl);
                            }
                            break;
                        }
                        case 'D':
                        {
                            var ped = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            ped.name = "Pedestal_Disco";
                            ped.transform.SetParent(objetos, false);
                            ped.transform.position = p + Vector3.up * 0.45f;
                            ped.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                            ped.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Nexus/Materiales/Entorno/M_Rack_Cuerpo.mat");
                            var disco = Poner("Coleccionables/Disco_Duro_Maestro", p + Vector3.up * 1.25f, 0f, objetos);
                            RegistrarInteres(p, Vector3.zero, c, disco, 1.1f, 3.0f);
                            var col = disco.GetComponent<Coleccionable>();
                            col.Configurar(TipoColeccionable.DiscoDuro, "Disco Duro Maestro", conCinematicaAlarma ? null : ObjetivoDe('D'), conCinematicaAlarma ? null : def.DialogoDisco);
                            EditorUtility.SetDirty(col);
                            break;
                        }
                        case 'V': case 'Y':
                        {
                            var d = MuroVecino(c) ?? Vector2Int.up;
                            var maquina = Poner(ch == 'V' ? "Utileria/Maquina_Bebidas" : "Utileria/Maquina_Snacks", p + Dir3(d) * (Celda / 2f - 0.1f), Yaw(d), objetos);
                            RegistrarInteres(maquina.transform.position, -Dir3(d), c, maquina, 1.2f, 3.0f);
                            break;
                        }
                        case 'M':
                            Poner("Utileria/Mesa_Cafeteria", p, (x * 47 + y * 13) % 360, objetos);
                            break;
                        case 'W':
                            Poner("Utileria/Isla_Escritorios", p, (x + y) % 2 == 0 ? 0f : 90f, objetos);
                            break;
                        case 'b':
                        {
                            // Estiba de cajas en medio de la sala (el prefab tiene el respaldo en z = 0)
                            float rot = (x * 53 + y * 29) % 360;
                            Poner($"Utileria/Pila_Cajas_{(x + y) % 3}", p + Quaternion.Euler(0f, rot, 0f) * new Vector3(0f, 0f, 0.35f), rot, objetos);
                            break;
                        }
                        case 'A': SpawnsAndroides.Add(p); break;
                        case 'K': CeldasCadaver.Add((c, p)); break;
                        case 'Q': CeldasEstacion.Add((c, p)); break;
                        case 'I': PonerEntrada(c, p, objetos, geo); break;
                        default:
                            PonerItem(ch, c, p, objetos);
                            break;
                    }
                }

            ColocarCadaveres(objetos);
            ColocarEstaciones(objetos);

            foreach (var panel in paneles)
            {
                ConstruirLuces(luces, lucesSinEnergia);
                panel.Configurar(puertasSinEnergia.ToArray(), lucesSinEnergia.ToArray(), def.ObjetivoPanel);
                EditorUtility.SetDirty(panel);
            }
            if (paneles.Count == 0) ConstruirLuces(luces, lucesSinEnergia);

            ConstruirCamaras(raiz.transform, zonas);
            Decorar(raiz.transform);
            ElegirAngulos(raiz.transform.Find("Camaras"));
            CamarasDeInteres(raiz.transform.Find("Camaras"));
            RellenarCobertura(raiz.transform.Find("Camaras"));
            Ambiente();
            GenerarMapa(raiz);

            // Malla de navegación (se calcula al cargar el nivel; ver NivelNexus)
            var nav = raiz.AddComponent<NavMeshSurface>();
            nav.collectObjects = CollectObjects.Children;
            nav.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            nav.layerMask = ~(1 << 2);   // ignora la capa "Ignore Raycast" (triggers)

            // Controlador del nivel
            var nivelComp = raiz.AddComponent<NivelNexus>();
            var sn = new SerializedObject(nivelComp);
            sn.FindProperty("nombreNivel").stringValue = def.Nombre;
            sn.FindProperty("tiempoInicial").floatValue = def.Tiempo;
            sn.FindProperty("temperaturaInicial").floatValue = def.TemperaturaInicial;
            sn.FindProperty("objetivoInicial").stringValue = def.ObjetivoInicial;
            sn.FindProperty("escenaSiguiente").stringValue = def.EscenaSiguiente;
            sn.ApplyModifiedPropertiesWithoutUndo();

            CrearJugador(inicio, rotInicio);
            ColocarAndroides();
            ColocarAndroidesEstacion();
            CrearCinematicas(raiz.transform, null);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nexus/Prefabs/UI/UI_Nivel.prefab"));

            // Triggers en la capa "Ignore Raycast" para que no bloqueen la visión de los androides
            foreach (var col in raiz.GetComponentsInChildren<Collider>(true))
                if (col.isTrigger) col.gameObject.layer = 2;

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log($"[Nexus] {def.Nombre} construido: {ancho}x{alto} celdas, {def.Salas.Count} salas, {SpawnsAndroides.Count} androides.");
        }

        static string ObjetivoDe(char ch) => def.ObjetivoAlRecoger.TryGetValue(ch, out var o) ? o : null;
        static bool conCinematicaAlarma;

        static void PonerItem(char ch, Vector2Int c, Vector3 p, Transform padre)
        {
            (string prefab, TipoColeccionable tipo, string nombre)? item = ch switch
            {
                'k' => ("Coleccionables/Tarjeta_Acceso_Roja", TipoColeccionable.TarjetaRoja, "Tarjeta de acceso roja"),
                'a' => ("Coleccionables/Tarjeta_Acceso_Azul", TipoColeccionable.TarjetaAzul, "Tarjeta de acceso azul"),
                'f' => ("Coleccionables/Fusible", TipoColeccionable.Fusible, "Fusible de repuesto"),
                'u' => ("Coleccionables/Memoria_USB", TipoColeccionable.USB, "Memoria USB de respaldo"),
                'm' => ("Coleccionables/Speedmax", TipoColeccionable.Speedmax, "Speedmax"),
                'c' => ("Coleccionables/Chocorramo", TipoColeccionable.Chocorramo, "Chocorramo"),
                'g' => ("Coleccionables/Cartucho_Refrigerante", TipoColeccionable.Refrigerante, "Cartucho de refrigerante"),
                'C' => ("Coleccionables/Cuchillo", TipoColeccionable.Cuchillo, "Bayoneta M9"),
                'H' => ("Coleccionables/Pistola", TipoColeccionable.Pistola, "Pistola M9"),
                'Z' => ("Coleccionables/Escopeta", TipoColeccionable.Escopeta, "Escopeta Remington 870"),
                'h' => ("Coleccionables/Balas_Pistola", TipoColeccionable.BalasPistola, "Balas de pistola"),
                'z' => ("Coleccionables/Cartuchos_Escopeta", TipoColeccionable.CartuchosEscopeta, "Cartuchos de escopeta"),
                _ => null
            };
            if (item == null) return;
            var go = Poner(item.Value.prefab, p + Vector3.up * 1.0f, 0f, padre);
            RegistrarInteres(p, Vector3.zero, c, go, 0.9f, 2.6f);
            var col = go.GetComponent<Coleccionable>();
            int unidades = ch == 'h' ? 12 : ch == 'z' ? 4 : 1;   // munición por caja
            col.Configurar(item.Value.tipo, item.Value.nombre, ObjetivoDe(ch), null, unidades);
            EditorUtility.SetDirty(col);
        }

        // ==================== Luces por sala ====================
        static void ConstruirLuces(Transform padre, List<GameObject> sinEnergia)
        {
            if (padre.childCount > 0) return;
            foreach (var kv in def.Salas)
            {
                var celdas = CeldasDeZona(kv.Key);
                if (celdas.Count == 0) continue;
                var grupo = Grupo("Luces_" + kv.Value.Nombre, padre);
                if (kv.Value.SinEnergia) sinEnergia.Add(grupo.gameObject);

                // Muestreo por "punto más lejano": una luz cada ~5 celdas, repartidas por la sala
                int cantidad = Mathf.Max(1, Mathf.CeilToInt(celdas.Count / 5f));
                var centroide = celdas.Aggregate(Vector3.zero, (s, c) => s + Centro(c)) / celdas.Count;
                var elegidas = new List<Vector2Int> { celdas.OrderBy(c => (Centro(c) - centroide).sqrMagnitude).First() };
                while (elegidas.Count < cantidad)
                    elegidas.Add(celdas.OrderByDescending(c => elegidas.Min(e => (Centro(c) - Centro(e)).sqrMagnitude)).First());

                for (int i = 0; i < elegidas.Count; i++)
                {
                    var l = new GameObject($"Luz_{kv.Value.Nombre}_{i}").AddComponent<Light>();
                    l.transform.SetParent(grupo, false);
                    l.transform.position = Centro(elegidas[i]) + Vector3.up * 2.7f;
                    l.type = LightType.Point;
                    l.color = kv.Value.Luz; l.intensity = kv.Value.Intensidad; l.range = 11f;
                    l.shadows = i == 0 ? LightShadows.Soft : LightShadows.None;
                    // Fluorescentes en mal estado: en las salas marcadas titila más o menos la mitad
                    if (kv.Value.Parpadeo && (elegidas.Count == 1 || i % 2 == 1)) l.gameObject.AddComponent<LuzParpadeante>();
                }
            }
        }

        static List<Vector2Int> CeldasDeZona(char zona)
        {
            var lista = new List<Vector2Int>();
            for (int y = 0; y < alto; y++)
                for (int x = 0; x < ancho; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (ZonaDe(c) == zona && Caminable(En(c))) lista.Add(c);
                }
            return lista;
        }

        // ==================== Cámaras fijas por sala ====================
        static void ConstruirCamaras(Transform raiz, Transform zonas)
        {
            var gestorGo = new GameObject("Camaras");
            gestorGo.transform.SetParent(raiz, false);
            PosicionesCamaras.Clear();
            CamaraDeZona.Clear();
            Candidatos.Clear();
            var gestor = gestorGo.AddComponent<GestorCamaras>();
            Camera primera = null;
            Vector3 inicio = Vector3.zero;
            for (int y = 0; y < alto; y++)
                for (int x = 0; x < ancho; x++)
                    if (En(new Vector2Int(x, y)) == 'S') inicio = Centro(new Vector2Int(x, y));

            foreach (var kv in def.Salas)
            {
                var celdas = CeldasDeZona(kv.Key);
                if (celdas.Count == 0) continue;
                float xMin = celdas.Min(c => c.x) * Celda, xMax = (celdas.Max(c => c.x) + 1) * Celda;
                float zMin = celdas.Min(c => c.y) * Celda, zMax = (celdas.Max(c => c.y) + 1) * Celda;
                string e = kv.Value.Esquina;
                var esquina = new Vector3(e.Contains("E") ? xMax : xMin, 0f, e.Contains("N") ? zMax : zMin);
                var celda = celdas.OrderBy(c => (Centro(c) - esquina).sqrMagnitude).First();
                Vector3 haciaEsquina = esquina - Centro(celda);
                Vector3 desplazamiento = new Vector3(Mathf.Sign(haciaEsquina.x), 0f, Mathf.Sign(haciaEsquina.z)) * (Celda / 2f - 0.35f);
                Vector3 pos = Centro(celda) + desplazamiento + Vector3.up * kv.Value.AlturaCamara;
                var centroide = celdas.Aggregate(Vector3.zero, (s, c) => s + Centro(c)) / celdas.Count;
                Vector3 mirar = centroide + Vector3.up * 0.6f;
                if (kv.Value.Camara.HasValue) pos = DesdeTexto(kv.Value.Camara.Value) + Vector3.up * kv.Value.AlturaCamara;
                if (kv.Value.Mirar.HasValue) mirar = DesdeTexto(kv.Value.Mirar.Value) + Vector3.up * 0.6f;

                float diag = new Vector2(xMax - xMin, zMax - zMin).magnitude;
                float fov = diag > 20f ? 72f : diag > 12f ? 66f : 60f;
                var colocadas = new List<Vector3>();
                Camera CrearCamara(string nombre, Vector3 p, Vector3 objetivo, bool principal)
                {
                    var go = new GameObject(nombre) { tag = "MainCamera" };
                    go.transform.SetParent(gestorGo.transform, false);
                    go.transform.position = p;
                    go.transform.LookAt(objetivo);
                    var c = go.AddComponent<Camera>();
                    c.fieldOfView = fov;
                    c.nearClipPlane = 0.1f; c.farClipPlane = 45f;
                    c.clearFlags = CameraClearFlags.SolidColor;
                    c.backgroundColor = Color.black;
                    c.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                    var cf = go.AddComponent<CamaraFija>();
                    cf.Sala = kv.Value.Nombre; cf.Principal = principal;
                    PosicionesCamaras.Add(p);
                    colocadas.Add(p);
                    return c;
                }

                // Cámara principal de la sala (la que se usa al entrar)
                var cam = CrearCamara("Cam_" + kv.Value.Nombre, pos, mirar, true);
                CamaraDeZona[kv.Key] = (pos, cam.transform.forward);

                // Ángulos alternativos CANDIDATOS: esquinas de la sala y, si es larga, el centro de sus
                // lados. No se crean todavía: después de decorar se eligen solo los que hagan falta
                // para cubrir lo que la principal no ve (ver ElegirAngulos).
                int anchoCeldas = celdas.Max(c => c.x) - celdas.Min(c => c.x) + 1;
                int largoCeldas = celdas.Max(c => c.y) - celdas.Min(c => c.y) + 1;
                var puntos = new List<Vector3>
                {
                    new Vector3(xMin, 0f, zMin), new Vector3(xMax, 0f, zMin), new Vector3(xMin, 0f, zMax), new Vector3(xMax, 0f, zMax)
                };
                if (anchoCeldas >= 5) { puntos.Add(new Vector3((xMin + xMax) / 2f, 0f, zMin)); puntos.Add(new Vector3((xMin + xMax) / 2f, 0f, zMax)); }
                if (largoCeldas >= 5) { puntos.Add(new Vector3(xMin, 0f, (zMin + zMax) / 2f)); puntos.Add(new Vector3(xMax, 0f, (zMin + zMax) / 2f)); }
                var celdasCamara = celdas.Where(c => !"RBXL".Contains(En(c))).ToList();
                int n = 0;
                foreach (var punto in puntos)
                {
                    if (celdasCamara.Count == 0) break;
                    var cel = celdasCamara.OrderBy(c => (Centro(c) - punto).sqrMagnitude).First();
                    Vector3 hacia = punto - Centro(cel);
                    Vector3 off = new Vector3(Mathf.Abs(hacia.x) > 0.5f ? Mathf.Sign(hacia.x) : 0f, 0f, Mathf.Abs(hacia.z) > 0.5f ? Mathf.Sign(hacia.z) : 0f) * (Celda / 2f - 0.35f);
                    Vector3 p = Centro(cel) + off + Vector3.up * kv.Value.AlturaCamara;
                    if (colocadas.Any(q => (q - p).sqrMagnitude < 3.5f * 3.5f)) continue;
                    // Mira hacia el lado opuesto de la sala (el centro, un poco más allá)
                    Vector3 objetivo = centroide + (centroide - new Vector3(p.x, 0f, p.z)) * 0.15f + Vector3.up * 0.6f;
                    Candidatos.Add((kv.Key, p, objetivo, fov));
                    colocadas.Add(p);
                    PosicionesCamaras.Add(p);   // el decorador deja libre ese rincón por si se usa
                    n++;
                }

                foreach (var c in celdas)
                {
                    var z = new GameObject($"Zona_{kv.Value.Nombre}_{c.x}_{c.y}");
                    z.transform.SetParent(zonas, false);
                    z.transform.position = Centro(c) + Vector3.up * 1.5f;
                    var caja = z.AddComponent<BoxCollider>();
                    caja.isTrigger = true; caja.size = new Vector3(Celda, 3f, Celda);
                    var zc = z.AddComponent<ZonaCamara>();
                    zc.Camara = cam;
                    zc.Sala = kv.Value.Nombre;
                    if ((Centro(c) - inicio).sqrMagnitude < 0.01f) primera = cam;
                }
            }
            var so = new SerializedObject(gestor);
            so.FindProperty("camaraInicial").objectReferenceValue = primera;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ==================== Androides ====================
        /// <summary>
        /// Los androides van fuera del objeto "Nivel" para que sus colliders no se incluyan como
        /// obstáculos al calcular la malla de navegación.
        /// </summary>
        static void ColocarAndroides()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nexus/Prefabs/Enemigos/Androide_Sentinel.prefab");
            if (prefab == null || def.Androides.Count == 0) return;
            var grupo = (GameObject.Find("Androides") ?? new GameObject("Androides")).transform;
            int n = 0;
            foreach (var a in def.Androides)
            {
                // Columna/fila del mapa de texto → centro de la celda en el mundo
                var puntos = a.Ruta.Select(c => Centro(new Vector2Int(c.x, alto - 1 - c.y))).ToArray();
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grupo);
                go.name = $"Androide_{++n}" + (a.ConAlarma ? "_Alarma" : "");
                Vector3 hacia = puntos.Length > 1 ? puntos[1] - puntos[0] : Vector3.forward;
                go.transform.SetPositionAndRotation(puntos[0], Quaternion.LookRotation(hacia.sqrMagnitude > 0.01f ? hacia : Vector3.forward));
                var ia = go.GetComponent<AndroideIA>();
                ia.Configurar(puntos, a.VelPatrulla, a.VelPersecucion, a.ConAlarma);
                EditorUtility.SetDirty(ia);
            }
        }

        // ==================== Mapa esquemático (pestaña MAPA del inventario) ====================
        static void GenerarMapa(GameObject raiz)
        {
            const int px = 32;
            int w = ancho * px, h = alto * px;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pix = new Color[w * h];
            void Rect(int x0, int y0, int x1, int y1, Color c)
            {
                for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y1); y++)
                    for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x++) pix[y * w + x] = c;
            }
            var zonasOrden = def.Salas.Keys.ToList();
            Color borde = new Color(0.45f, 0.9f, 1f, 1f);

            for (int y = 0; y < alto; y++)
                for (int x = 0; x < ancho; x++)
                {
                    var c = new Vector2Int(x, y);
                    char ch = En(c);
                    if (Solido(ch)) continue;
                    int x0 = x * px, y0 = y * px;
                    int iz = zonasOrden.IndexOf(ZonaDe(c));
                    Color suelo = iz < 0 ? new Color(0.08f, 0.22f, 0.26f) : Color.HSVToRGB(0.5f + (iz % 4) * 0.025f, 0.6f, 0.3f + (iz % 3) * 0.05f);
                    Rect(x0, y0, x0 + px, y0 + px, Racks(ch) ? new Color(0.03f, 0.07f, 0.08f) : suelo);
                    // Bordes: muros (línea clara) y límites entre salas (línea oscura)
                    foreach (var d in Dirs)
                    {
                        var v = c + d;
                        bool muro = Solido(En(v));
                        bool otraSala = !muro && Caminable(ch) && Caminable(En(v)) && ZonaDe(v) != ZonaDe(c) && !"RBXL".Contains(ch) && !"RBXL".Contains(En(v));
                        if (!muro && !otraSala) continue;
                        Color col = muro ? borde : new Color(0.02f, 0.05f, 0.06f);
                        int g = muro ? 3 : 2;
                        if (d == Vector2Int.up) Rect(x0, y0 + px - g, x0 + px, y0 + px, col);
                        if (d == Vector2Int.down) Rect(x0, y0, x0 + px, y0 + g, col);
                        if (d == Vector2Int.right) Rect(x0 + px - g, y0, x0 + px, y0 + px, col);
                        if (d == Vector2Int.left) Rect(x0, y0, x0 + g, y0 + px, col);
                    }
                    // Puertas y puntos de interés
                    Color? puerta = ch == 'R' ? new Color(0.95f, 0.2f, 0.15f) : ch == 'B' ? new Color(0.25f, 0.55f, 1f)
                                  : ch == 'X' ? new Color(1f, 0.6f, 0.1f) : ch == 'L' ? new Color(0.65f, 0.75f, 0.8f) : (Color?)null;
                    if (puerta.HasValue)
                    {
                        bool ejeX = Caminable(En(c + Vector2Int.up)) && Caminable(En(c + Vector2Int.down));
                        if (ejeX) Rect(x0 + 5, y0 + px / 2 - 3, x0 + px - 5, y0 + px / 2 + 3, puerta.Value);
                        else Rect(x0 + px / 2 - 3, y0 + 5, x0 + px / 2 + 3, y0 + px - 5, puerta.Value);
                    }
                    Color? punto = ch == 'T' ? new Color(0.3f, 1f, 0.5f) : ch == 'G' ? new Color(0.3f, 0.85f, 1f)
                                 : ch == 'P' ? new Color(1f, 0.7f, 0.2f) : ch == 'D' ? new Color(0.4f, 0.7f, 1f) : (Color?)null;
                    if (punto.HasValue) Rect(x0 + px / 2 - 5, y0 + px / 2 - 5, x0 + px / 2 + 5, y0 + px / 2 + 5, punto.Value);
                    if (ch == 'V' || ch == 'Y') Rect(x0 + px / 2 - 4, y0 + px / 2 - 4, x0 + px / 2 + 4, y0 + px / 2 + 4, new Color(1f, 0.85f, 0.2f));   // máquinas expendedoras
                    if (ch == 'E') Rect(x0 + 6, y0 + 6, x0 + px - 6, y0 + px - 6, new Color(0.3f, 1f, 0.45f));
                }
            tex.SetPixels(pix);
            tex.Apply();
            string carpeta = "Assets/Nexus/UI/Mapas";
            System.IO.Directory.CreateDirectory(carpeta);
            string ruta = $"{carpeta}/Mapa_{def.Escena}.png";
            System.IO.File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(ruta);
            var ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
            ti.textureType = TextureImporterType.Default;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Point;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();

            var mapa = raiz.AddComponent<MapaNivel>();
            mapa.textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            mapa.tamanoMundo = new Vector2(ancho * Celda, alto * Celda);
            var nombres = new List<string>(); var posiciones = new List<Vector2>();
            foreach (var kv in def.Salas)
            {
                var celdas = CeldasDeZona(kv.Key);
                if (celdas.Count == 0) continue;
                var cen = celdas.Aggregate(Vector3.zero, (s, c) => s + Centro(c)) / celdas.Count;
                nombres.Add(kv.Value.Nombre.Replace('_', ' '));
                posiciones.Add(new Vector2(cen.x, cen.z));
            }
            mapa.nombresSalas = nombres.ToArray();
            mapa.posicionesSalas = posiciones.ToArray();
        }

        // ==================== Ambiente ====================
        static void Ambiente()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = def.Ambiente;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = def.Niebla;
            RenderSettings.fogColor = new Color(0.01f, 0.012f, 0.018f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            var vol = new GameObject("Volumen_Global").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = PerfilPostProceso();
        }

        public static VolumeProfile PerfilPostProceso()
        {
            const string ruta = "Assets/Nexus/Escenas/PP_Nexus.asset";
            var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
            if (perfil != null) return perfil;
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(perfil, ruta);
            T Agregar<T>() where T : VolumeComponent
            {
                var c = perfil.Add<T>(true);
                c.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(c, perfil);
                return c;
            }
            var bloom = Agregar<Bloom>();
            bloom.threshold.Override(1f); bloom.intensity.Override(1.4f); bloom.scatter.Override(0.65f);
            Agregar<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var vig = Agregar<Vignette>();
            vig.intensity.Override(0.38f); vig.smoothness.Override(0.45f);
            var color = Agregar<ColorAdjustments>();
            color.postExposure.Override(0.4f); color.contrast.Override(12f); color.saturation.Override(-15f);
            var grano = Agregar<FilmGrain>();
            grano.type.Override(FilmGrainLookup.Medium1); grano.intensity.Override(0.25f);
            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            return perfil;
        }

        // ==================== Jugador ====================
        public static GameObject CrearJugador(Vector3 pos, float rotY)
        {
            var jugador = new GameObject("Jugador") { tag = "Player" };
            jugador.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
            var cc = jugador.AddComponent<CharacterController>();
            cc.height = 1.75f; cc.radius = 0.28f; cc.center = new Vector3(0f, 0.9f, 0f);

            var controlador = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Nexus/Animaciones/AC_Tecnico.controller");
            var neyder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nexus/Modelos/Personajes/Neyder.fbx");
            var deisy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nexus/Modelos/Personajes/Deisy.fbx");
            var mj = jugador.AddComponent<ModeloJugador>();
            var so = new SerializedObject(mj);
            var arr = so.FindProperty("modelos");
            arr.arraySize = 2;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = neyder;
            arr.GetArrayElementAtIndex(1).objectReferenceValue = deisy;
            so.FindProperty("controlador").objectReferenceValue = controlador;
            so.ApplyModifiedPropertiesWithoutUndo();

            jugador.AddComponent<InventarioJugador>();
            jugador.AddComponent<ControladorJugador>();
            jugador.AddComponent<VidaJugador>();
            jugador.AddComponent<InteraccionJugador>();
            jugador.AddComponent<AudioListener>();

            // Habilidad especial (señuelo de red / KILLSWITCH según el técnico elegido)
            var hab = jugador.AddComponent<HabilidadJugador>();
            var sh = new SerializedObject(hab);
            sh.FindProperty("materialRayo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Nexus/Materiales/Entorno/M_Neon_Cian.mat");
            sh.FindProperty("efectoChispas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Pre + "Efectos/FX_Chispas.prefab");
            sh.FindProperty("prefabSenuelo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Pre + "Efectos/Senuelo_Red.prefab");
            sh.ApplyModifiedPropertiesWithoutUndo();

            // Armas (cuchillo, pistola, escopeta): se equipan al recogerlas o desde el inventario
            var arma = jugador.AddComponent<ArmaJugador>();
            var sa = new SerializedObject(arma);
            sa.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosArmas>("Assets/Nexus/Datos/DatosArmas.asset");
            sa.ApplyModifiedPropertiesWithoutUndo();

            // Modelo de vista previa en el editor (en juego lo reemplaza ModeloJugador)
            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(neyder, jugador.transform);
            modelo.name = "Modelo";
            var animator = modelo.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = controlador;
            return jugador;
        }
    }
}
