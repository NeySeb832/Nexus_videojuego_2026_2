using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Cámaras de interés: un plano cercano por cada objeto importante del nivel (terminales,
    /// notas, paneles, puertas, máquinas, montacargas, disco y objetos para recoger).
    ///
    /// En los survival horror la cámara también guía al jugador: al acercarse a algo que se puede
    /// usar, corta a un plano en 3/4 que muestra el objeto y al técnico frente a él. El constructor
    /// busca automáticamente una posición para cada plano:
    ///  - delante del objeto (por donde se acerca el jugador), girada unos 35° para el efecto 3/4;
    ///  - dentro de una celda caminable, sin chocar con muebles;
    ///  - con vista libre tanto al objeto como al lugar donde se para el jugador.
    /// Si no la encuentra, prueba otros ángulos y distancias.
    /// </summary>
    public static partial class ConstructorNiveles
    {
        class PuntoInteres
        {
            public Vector3 Pos, Normal;
            public Vector2Int Celda;
            public GameObject Obj;
            public float AlturaMirar, Radio;
        }

        static readonly List<PuntoInteres> PuntosInteres = new List<PuntoInteres>();
        const int MascaraSolidos = ~(1 << 2);

        /// <summary>Ángulos alternativos posibles de cada sala (zona, posición, punto al que mira, FOV).</summary>
        static readonly List<(char zona, Vector3 pos, Vector3 objetivo, float fov)> Candidatos = new List<(char, Vector3, Vector3, float)>();

        static Camera NuevaCamara(Transform gestor, string nombre, Vector3 pos, Vector3 mirar, float fov, string sala)
        {
            var go = new GameObject(nombre) { tag = "MainCamera" };
            go.transform.SetParent(gestor, false);
            go.transform.position = pos;
            go.transform.LookAt(mirar);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.1f; cam.farClipPlane = 45f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            cam.enabled = false;
            go.AddComponent<CamaraFija>().Sala = sala;
            return cam;
        }

        /// <summary>Puntos del suelo libre de una sala (centro y 4 cerca de los bordes de cada celda, sin puertas).</summary>
        static List<Vector3> PuntosDeSala(char zona)
        {
            var lista = new List<Vector3>();
            foreach (var c in CeldasDeZona(zona))
            {
                if ("RBXL".Contains(En(c))) continue;
                foreach (var o in new[] { Vector3.zero, new Vector3(0.9f, 0, 0.9f), new Vector3(-0.9f, 0, 0.9f), new Vector3(0.9f, 0, -0.9f), new Vector3(-0.9f, 0, -0.9f) })
                {
                    Vector3 p = Centro(c) + o;
                    if (!Physics.CheckCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.5f, 0.28f, MascaraSolidos, QueryTriggerInteraction.Ignore)) lista.Add(p);
                }
            }
            return lista;
        }

        /// <summary>
        /// Elige, de los ángulos candidatos de cada sala, solo los necesarios: empezando por la cámara
        /// principal, agrega uno a uno el candidato que cubre más puntos que nadie ve todavía, hasta
        /// que no quede ninguno (o los que queden sean sueltos, de eso se encarga el relleno).
        /// Así una sala pequeña que la principal ve entera se queda con una sola cámara.
        /// </summary>
        static void ElegirAngulos(Transform gestor)
        {
            if (gestor == null) return;
            Physics.SyncTransforms();
            var temporal = new GameObject("tmp_cam").AddComponent<Camera>();
            temporal.enabled = false; temporal.aspect = 16f / 9f; temporal.nearClipPlane = 0.1f;
            int elegidos = 0, descartados = 0;
            try
            {
                foreach (var kv in def.Salas)
                {
                    string sala = kv.Value.Nombre;
                    var propias = gestor.GetComponentsInChildren<CamaraFija>(true).Where(c => c.Sala == sala).Select(c => c.Camara).ToList();
                    foreach (var cam in propias) cam.aspect = 16f / 9f;
                    var pendientes = PuntosDeSala(kv.Key).Where(p => !propias.Any(cam => { var (d, v, _) = GestorCamaras.Evaluar(cam, p); return d && v; })).ToList();
                    var libres = Candidatos.Where(c => c.zona == kv.Key).ToList();
                    int n = 0;
                    while (pendientes.Count > 0 && libres.Count > 0)
                    {
                        int mejor = -1, mejorCubre = 0;
                        for (int i = 0; i < libres.Count; i++)
                        {
                            temporal.transform.position = libres[i].pos;
                            temporal.transform.LookAt(libres[i].objetivo);
                            temporal.fieldOfView = libres[i].fov;
                            int cubre = pendientes.Count(p => { var (d, v, _) = GestorCamaras.Evaluar(temporal, p); return d && v; });
                            if (cubre > mejorCubre) { mejorCubre = cubre; mejor = i; }
                        }
                        if (mejor < 0 || mejorCubre < 2) break;
                        var c = libres[mejor];
                        libres.RemoveAt(mejor);
                        var cam = NuevaCamara(gestor, $"Cam_{sala}_Alt{++n}", c.pos, c.objetivo, c.fov, sala);
                        cam.aspect = 16f / 9f;
                        pendientes.RemoveAll(p => { var (d, v, _) = GestorCamaras.Evaluar(cam, p); return d && v; });
                        elegidos++;
                    }
                    descartados += libres.Count;
                }
            }
            finally { Object.DestroyImmediate(temporal.gameObject); }
            foreach (var c in gestor.GetComponentsInChildren<Camera>(true)) c.ResetAspect();
            Debug.Log($"[Nexus] Ángulos alternativos: {elegidos} necesarios, {descartados} candidatos descartados por sobrar.");
        }

        /// <param name="normal">Dirección desde el objeto hacia donde se para el jugador (cero si se rodea)</param>
        static void RegistrarInteres(Vector3 pos, Vector3 normal, Vector2Int celda, GameObject obj, float alturaMirar, float radio)
        {
            if (obj == null) return;
            PuntosInteres.Add(new PuntoInteres
            {
                Pos = new Vector3(pos.x, 0f, pos.z), Normal = normal, Celda = celda, Obj = obj, AlturaMirar = alturaMirar, Radio = radio
            });
        }

        static Vector2Int CeldaDe(Vector3 w) => new Vector2Int(Mathf.FloorToInt(w.x / Celda), Mathf.FloorToInt(w.z / Celda));

        static void CamarasDeInteres(Transform gestor)
        {
            if (gestor == null) return;
            Physics.SyncTransforms();
            int creadas = 0, sinLugar = 0;
            foreach (var pi in PuntosInteres)
            {
                char zona = ZonaDe(pi.Celda);
                if (!def.Salas.TryGetValue(zona, out var sala)) continue;

                Vector3 mirar = pi.Pos + Vector3.up * pi.AlturaMirar;
                // Sin dirección propia (objeto en el centro de la celda): la cámara se pone del mismo
                // lado que la cámara principal de la sala, para no invertir la orientación del jugador
                Vector3 baseDir = pi.Normal;
                if (baseDir == Vector3.zero)
                {
                    baseDir = Vector3.back;
                    if (CamaraDeZona.TryGetValue(zona, out var cz))
                    {
                        var f = new Vector3(cz.adelante.x, 0f, cz.adelante.z);
                        if (f.sqrMagnitude > 0.01f) baseDir = -f.normalized;
                    }
                }
                Vector3 donde = pi.Normal != Vector3.zero ? pi.Pos + pi.Normal * 1.1f : pi.Pos + baseDir * 1.0f;

                bool encontrada = false;
                Vector3 elegida = default;
                foreach (float ang in new[] { 35f, -35f, 55f, -55f, 0f, 80f, -80f, 120f, -120f, 160f, -160f })
                {
                    foreach (float dist in new[] { 3.8f, 3.2f, 4.5f })
                    {
                        Vector3 p = pi.Pos + Quaternion.Euler(0f, ang, 0f) * baseDir * dist + Vector3.up * 2.35f;
                        if (!PosicionCamaraValida(p)) continue;
                        if (!LineaLibre(p, mirar, pi.Obj) || !LineaLibre(p, donde + Vector3.up * 1.1f, pi.Obj)) continue;
                        elegida = p; encontrada = true;
                        break;
                    }
                    if (encontrada) break;
                }
                if (!encontrada) { sinLugar++; continue; }

                var go = new GameObject($"Cam_{sala.Nombre}_Interes_{pi.Obj.name}") { tag = "MainCamera" };
                go.transform.SetParent(gestor, false);
                go.transform.position = elegida;
                // Encuadre: el objeto, con espacio para el técnico parado delante
                go.transform.LookAt(Vector3.Lerp(mirar, donde + Vector3.up * 0.8f, 0.45f));
                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = 56f;
                cam.nearClipPlane = 0.1f; cam.farClipPlane = 45f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                cam.enabled = false;
                var cf = go.AddComponent<CamaraFija>();
                cf.Sala = sala.Nombre; cf.Principal = false;
                cf.Interes = true; cf.Objetivo = pi.Obj; cf.Punto = pi.Pos; cf.Radio = pi.Radio;
                PosicionesCamaras.Add(elegida);
                creadas++;
            }
            Debug.Log($"[Nexus] Cámaras de interés: {creadas} creadas, {sinLugar} sin posición válida.");
        }

        /// <summary>
        /// Relleno de cobertura: busca puntos del suelo que ninguna cámara de su sala ve (detrás de
        /// una fila de racks, el fondo de un pasillo justo debajo de su cámara...) y crea una cámara
        /// extra que los encuadre. Las celdas de puerta no cuentan: las puertas se abren al pasar.
        /// </summary>
        static void RellenarCobertura(Transform gestor)
        {
            if (gestor == null) return;
            Physics.SyncTransforms();
            var temporal = new GameObject("tmp_cam").AddComponent<Camera>();
            temporal.enabled = false; temporal.aspect = 16f / 9f; temporal.fieldOfView = 62f; temporal.nearClipPlane = 0.1f;
            int creadas = 0;
            try
            {
                foreach (var kv in def.Salas)
                {
                    var sala = kv.Value.Nombre;
                    var celdas = CeldasDeZona(kv.Key).Where(c => !"RBXL".Contains(En(c))).ToList();
                    var propias = gestor.GetComponentsInChildren<CamaraFija>(true).Where(c => c.Sala == sala && !c.Interes).Select(c => c.Camara).ToList();
                    foreach (var cam in propias) cam.aspect = 16f / 9f;

                    bool Cubierto(Vector3 p) => propias.Any(cam => { var (d, v, _) = GestorCamaras.Evaluar(cam, p); return d && v; });
                    var pendientes = new List<Vector3>();
                    foreach (var c in celdas)
                        foreach (var o in new[] { Vector3.zero, new Vector3(0.9f, 0, 0.9f), new Vector3(-0.9f, 0, 0.9f), new Vector3(0.9f, 0, -0.9f), new Vector3(-0.9f, 0, -0.9f) })
                        {
                            Vector3 p = Centro(c) + o;
                            if (Physics.CheckCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.5f, 0.28f, MascaraSolidos, QueryTriggerInteraction.Ignore)) continue;
                            if (!Cubierto(p)) pendientes.Add(p);
                        }

                    int extra = 0;
                    while (pendientes.Count > 0 && extra < 4)
                    {
                        Vector3 objetivo = pendientes[0];
                        Vector3 mejorPos = default; float mejorPuntaje = float.MinValue; int mejorCubre = 0;
                        foreach (var q in celdas)
                        {
                            float dq = (Centro(q) - objetivo).magnitude;
                            if (dq > 12f) continue;
                            foreach (var o in new[] { new Vector3(1.05f, 0, 1.05f), new Vector3(-1.05f, 0, 1.05f), new Vector3(1.05f, 0, -1.05f), new Vector3(-1.05f, 0, -1.05f), Vector3.zero,
                                                      new Vector3(1.1f, 0, 0), new Vector3(-1.1f, 0, 0), new Vector3(0, 0, 1.1f), new Vector3(0, 0, -1.1f) })
                            {
                                Vector3 pos = Centro(q) + o + Vector3.up * kv.Value.AlturaCamara;
                                var plano = pos - objetivo; plano.y = 0f;
                                if (plano.magnitude < 2.2f) continue;   // demasiado encima del punto
                                if (!PosicionCamaraValida(pos)) continue;
                                temporal.transform.position = pos;
                                temporal.transform.LookAt(objetivo + Vector3.up * 0.8f);
                                var (d, v, puntaje) = GestorCamaras.Evaluar(temporal, objetivo);
                                if (!d || !v) continue;
                                // Se prefiere el ángulo que además cubra más puntos pendientes
                                int cubre = pendientes.Count(p2 => { var (d2, v2, _) = GestorCamaras.Evaluar(temporal, p2); return d2 && v2; });
                                float total = puntaje + cubre * 0.15f;
                                if (total > mejorPuntaje) { mejorPuntaje = total; mejorPos = pos; mejorCubre = cubre; }
                            }
                        }
                        if (mejorCubre == 0) { pendientes.RemoveAt(0); continue; }   // imposible de cubrir desde su sala

                        var go = new GameObject($"Cam_{sala}_Cob{++extra}") { tag = "MainCamera" };
                        go.transform.SetParent(gestor, false);
                        go.transform.position = mejorPos;
                        go.transform.LookAt(objetivo + Vector3.up * 0.8f);
                        var cam2 = go.AddComponent<Camera>();
                        cam2.fieldOfView = 62f; cam2.nearClipPlane = 0.1f; cam2.farClipPlane = 45f;
                        cam2.clearFlags = CameraClearFlags.SolidColor; cam2.backgroundColor = Color.black;
                        cam2.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                        cam2.enabled = false; cam2.aspect = 16f / 9f;
                        var cf = go.AddComponent<CamaraFija>();
                        cf.Sala = sala;
                        propias.Add(cam2);
                        PosicionesCamaras.Add(mejorPos);
                        creadas++;
                        pendientes.RemoveAll(p2 => Cubierto(p2));
                    }
                }
            }
            finally { Object.DestroyImmediate(temporal.gameObject); }
            foreach (var c in gestor.GetComponentsInChildren<Camera>(true)) c.ResetAspect();
            Debug.Log($"[Nexus] Relleno de cobertura: {creadas} cámaras extra.");
        }

        /// <summary>Dentro de una celda caminable (no puerta), lejos de las paredes y sin muebles alrededor.</summary>
        static bool PosicionCamaraValida(Vector3 p)
        {
            var c = CeldaDe(p);
            char ch = En(c);
            if (!Caminable(ch) || "RBXL".Contains(ch)) return false;
            float lx = p.x - c.x * Celda, lz = p.z - c.y * Celda;
            const float m = 0.35f;
            if (lx < m && Solido(En(c + Vector2Int.left))) return false;
            if (lx > Celda - m && Solido(En(c + Vector2Int.right))) return false;
            if (lz < m && Solido(En(c + Vector2Int.down))) return false;
            if (lz > Celda - m && Solido(En(c + Vector2Int.up))) return false;
            return !Physics.CheckSphere(p, 0.3f, MascaraSolidos, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Nada sólido entre a y b (salvo el propio objeto de interés).</summary>
        static bool LineaLibre(Vector3 a, Vector3 b, GameObject ignorar)
        {
            Vector3 d = b - a;
            foreach (var hit in Physics.RaycastAll(a, d.normalized, d.magnitude, MascaraSolidos, QueryTriggerInteraction.Ignore))
                if (ignorar == null || !hit.transform.IsChildOf(ignorar.transform)) return false;
            return true;
        }
    }
}
