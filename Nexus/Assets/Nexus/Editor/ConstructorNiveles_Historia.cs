using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>Un cadáver del mapa ('K', en orden de lectura): quién era, qué lleva y su nota.</summary>
    public class DefCadaver
    {
        public string Modelo = "Empleado";      // Swat_A, Swat_B, Empleado
        public string Pose = "Tendido";         // Tendido, Pared
        public string Quien = "Empleado de Nexus";
        public string[] Lineas;
        public TipoColeccionable? Objeto;
        public int Cantidad = 1;
        public string NombreObjeto;
        public TipoColeccionable? Extra;
        public int CantidadExtra;
        public string NombreExtra;
        public string TituloNota, TextoNota, Objetivo;
    }

    /// <summary>
    /// Historia del nivel: cadáveres que se registran, estaciones de carga de los androides que
    /// despierta la alarma, la puerta de servicio por la que entran los técnicos y las cinemáticas.
    /// </summary>
    public static partial class ConstructorNiveles
    {
        static readonly List<(Vector2Int celda, Vector3 pos)> CeldasCadaver = new List<(Vector2Int, Vector3)>();
        static readonly List<(Vector2Int celda, Vector3 pos)> CeldasEstacion = new List<(Vector2Int, Vector3)>();
        static (Vector2Int celda, Vector3 pos, Vector3 haciaDentro, PuertaSeguridad puerta)? Entrada;
        static readonly List<AndroideIA> AndroidesEstacion = new List<AndroideIA>();

        static void ReiniciarHistoria()
        {
            CeldasCadaver.Clear(); CeldasEstacion.Clear(); Entrada = null; AndroidesEstacion.Clear(); EstacionesPendientes.Clear(); CeldasValvula.Clear();
            CeldaJefe = null; JefePendiente = null; Jefe = null;
        }

        /// <summary>Distancia desde el centro de la celda hasta la cara interior de la pared en esa dirección.</summary>
        static float CaraPared(Vector3 centro, Vector3 dir)
        {
            Physics.SyncTransforms();
            return Physics.Raycast(centro + Vector3.up * 1.0f, dir, out RaycastHit hit, 2.5f, MascaraSolidos, QueryTriggerInteraction.Ignore)
                ? hit.distance : Celda / 2f - 0.15f;
        }

        // ======================= Puerta de servicio ('I') =======================
        static void PonerEntrada(Vector2Int c, Vector3 p, Transform objetos, Transform geo)
        {
            // Hacia la sala: la celda vecina que se puede caminar
            Vector2Int dentro = Dirs.FirstOrDefault(d => Caminable(En(c + d)) && En(c + d) != 'I');
            bool ejeX = dentro.y != 0;   // la puerta mira hacia la sala
            var puerta = Poner("Interactivos/Puerta_Seguridad_Libre", p, ejeX ? 0f : 90f, objetos);
            puerta.name = "Puerta_Servicio";
            Vector3 lado = ejeX ? Vector3.right : Vector3.forward;
            foreach (int s in new[] { -1, 1 })
            {
                var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                r.name = "Relleno_Puerta";
                r.transform.SetParent(geo, false);
                r.transform.position = p + lado * (s * 1.375f) + Vector3.up * 1.5f;
                r.transform.rotation = Quaternion.Euler(0f, ejeX ? 0f : 90f, 0f);
                r.transform.localScale = new Vector3(0.25f, 3f, 0.45f);
                r.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Nexus/Materiales/Entorno/M_Pared.mat");
            }
            Entrada = (c, p, Dir3(dentro), puerta.GetComponent<PuertaSeguridad>());
        }

        // ======================= Cadáveres ('K') =======================
        static void ColocarCadaveres(Transform objetos)
        {
            var datosArmas = AssetDatabase.LoadAssetAtPath<DatosArmas>("Assets/Nexus/Datos/DatosArmas.asset");
            for (int i = 0; i < CeldasCadaver.Count; i++)
            {
                var (c, p) = CeldasCadaver[i];
                var dc = i < def.Cadaveres.Count ? def.Cadaveres[i] : new DefCadaver { Lineas = new[] { "Nadie debería haber estado aquí esa noche." } };
                var muro = MuroVecino(c);
                string pose = dc.Pose == "Pared" && muro.HasValue ? "Pared" : "Tendido";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Nexus/Prefabs/Cadaveres/Cadaver_{dc.Modelo}_{pose}.prefab");
                if (prefab == null) { Debug.LogError($"[Nexus] Falta el cadáver {dc.Modelo} {pose} (menú Nexus/Personajes/Crear cadáveres)"); continue; }
                Vector3 pos; Quaternion rot;
                if (pose == "Pared")
                {
                    Vector3 d = Dir3(muro.Value);
                    pos = p + d * (CaraPared(p, d) - 0.02f);
                    rot = Quaternion.LookRotation(-d);
                }
                else
                {
                    pos = p;
                    rot = Quaternion.Euler(0f, (c.x * 71 + c.y * 37) % 360, 0f);
                }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, objetos);
                go.name = $"Cadaver_{i + 1}_{dc.Modelo}";
                go.transform.SetPositionAndRotation(pos, rot);

                var visuales = new List<GameObject>();
                if (dc.Objeto.HasValue) { var v = VisualObjeto(go, dc.Objeto.Value, pose, datosArmas); if (v != null) visuales.Add(v); }
                if (dc.Extra.HasValue) { var v = VisualObjeto(go, dc.Extra.Value, pose, datosArmas, true); if (v != null) visuales.Add(v); }

                // Luz de emergencia tenue sobre el cuerpo: se distingue aunque la sala esté a oscuras
                var luz = new GameObject("Luz_Cadaver").AddComponent<Light>();
                luz.transform.SetParent(go.transform, false);
                var mcl = go.GetComponent<MarcadorCadaver>();
                luz.transform.localPosition = (mcl.Min + mcl.Max) / 2f + Vector3.up * 1.6f;
                luz.type = LightType.Point; luz.range = 3.2f; luz.intensity = 1.6f; luz.shadows = LightShadows.None;
                luz.color = new Color(1f, 0.82f, 0.62f);

                var cad = go.AddComponent<Cadaver>();
                cad.Configurar($"cadaver_{c.x}_{c.y}", dc.Quien, dc.Lineas, dc.Objeto.HasValue, dc.Objeto ?? TipoColeccionable.BalasPistola,
                               dc.Cantidad, dc.NombreObjeto, dc.TituloNota, dc.TextoNota, dc.Objetivo, visuales.ToArray());
                if (dc.Extra.HasValue) cad.ConfigurarExtra(dc.Extra.Value, dc.CantidadExtra, dc.NombreExtra);
                EditorUtility.SetDirty(cad);
                var mc = go.GetComponent<MarcadorCadaver>();
                Vector3 centro = go.transform.TransformPoint((mc.Min + mc.Max) / 2f);
                RegistrarInteres(new Vector3(centro.x, 0f, centro.z), pose == "Pared" ? go.transform.forward : Vector3.zero, c, go, 0.5f, 2.8f);
            }
        }

        /// <summary>El objeto que lleva el cadáver: el arma en su mano o en el suelo, la munición junto a la cadera.</summary>
        static GameObject VisualObjeto(GameObject cadaver, TipoColeccionable tipo, string pose, DatosArmas datos, bool segundo = false)
        {
            var mc = cadaver.GetComponent<MarcadorCadaver>();
            Vector3 min = mc.Min, max = mc.Max, centro = (min + max) / 2f;
            GameObject v = null;
            switch (tipo)
            {
                case TipoColeccionable.Pistola:
                    if (pose == "Pared" && mc.ManoDerecha != null && datos != null)
                    {
                        // Muerto con la pistola todavía en la mano
                        v = (GameObject)PrefabUtility.InstantiatePrefab(datos.pistola, mc.ManoDerecha);
                        v.transform.localPosition = datos.posicionEnMano;
                        v.transform.localRotation = Quaternion.Euler(datos.rotacionEnMano);
                        var s = mc.ManoDerecha.lossyScale;
                        v.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
                    }
                    else v = EnSuelo(cadaver, datos?.pistola, new Vector3(max.x + 0.15f, 0.02f, centro.z + 0.2f), new Vector3(0f, 70f, 90f));
                    break;
                case TipoColeccionable.Escopeta:
                    v = EnSuelo(cadaver, datos?.escopeta, new Vector3(max.x + 0.25f, 0.035f, centro.z), new Vector3(0f, 12f, 90f));
                    break;
                case TipoColeccionable.Cuchillo:
                    Vector3 mano = mc.ManoDerecha != null ? cadaver.transform.InverseTransformPoint(mc.ManoDerecha.position) : new Vector3(max.x, 0f, centro.z);
                    v = EnSuelo(cadaver, datos?.cuchillo, new Vector3(mano.x + 0.18f, 0.012f, mano.z + 0.05f), new Vector3(0f, 55f, 90f));
                    break;
                case TipoColeccionable.BalasPistola:
                case TipoColeccionable.CartuchosEscopeta:
                    var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Pre + (tipo == TipoColeccionable.BalasPistola ? "Coleccionables/Balas_Pistola" : "Coleccionables/Cartuchos_Escopeta") + ".prefab");
                    var tmp = (GameObject)PrefabUtility.InstantiatePrefab(pf);
                    PrefabUtility.UnpackPrefabInstance(tmp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    v = tmp.transform.Find("Visual").gameObject;
                    v.transform.SetParent(cadaver.transform, false);
                    Object.DestroyImmediate(tmp);
                    Object.DestroyImmediate(v.GetComponent<FlotarGirar>());
                    v.name = "Municion";
                    v.transform.localScale = Vector3.one * 0.75f;
                    v.transform.localPosition = new Vector3(min.x - 0.15f, 0.045f, centro.z + (segundo ? -0.25f : 0.15f));
                    v.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
                    break;
            }
            return v;
        }

        static GameObject EnSuelo(GameObject padre, GameObject prefab, Vector3 local, Vector3 giro)
        {
            if (prefab == null) return null;
            var v = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre.transform);
            v.transform.localPosition = local;
            v.transform.localRotation = Quaternion.Euler(giro);
            return v;
        }

        // ======================= Estaciones de carga ('Q') =======================
        static void ColocarEstaciones(Transform objetos)
        {
            var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Nexus/Materiales/Entorno/M_Rack_Cuerpo.mat");
            var led = AssetDatabase.LoadAssetAtPath<Material>("Assets/Nexus/Modelos/Armas/M_Arma_Laser.mat");
            var ledCian = LedEstacion();
            int n = 0;
            var lista = CeldasEstacion.Select(e => (e.celda, e.pos, jefe: false)).ToList();
            if (CeldaJefe.HasValue) lista.Add((CeldaJefe.Value.celda, CeldaJefe.Value.pos, true));
            foreach (var (c, p, esJefe) in lista)
            {
                var muro = MuroVecino(c) ?? Vector2Int.up;
                Vector3 d = Dir3(muro);
                float cara = CaraPared(p, d);
                var raiz = new GameObject(esJefe ? "Capsula_SENTINEL_Omega" : $"Estacion_Carga_{++n}");
                raiz.transform.SetParent(objetos, false);
                raiz.transform.SetPositionAndRotation(p + d * cara, Quaternion.LookRotation(-d));
                if (esJefe) raiz.transform.localScale = Vector3.one * 1.45f;   // la cápsula del prototipo es más grande
                // Panel trasero, marco, base de carga y franjas de luz
                Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0f, 1.25f, 0.06f), new Vector3(1.15f, 2.5f, 0.12f), metal);
                Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(-0.6f, 1.25f, 0.2f), new Vector3(0.1f, 2.5f, 0.35f), metal);
                Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0.6f, 1.25f, 0.2f), new Vector3(0.1f, 2.5f, 0.35f), metal);
                Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0f, 2.45f, 0.2f), new Vector3(1.3f, 0.1f, 0.4f), metal);
                Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0.6f), new Vector3(1.0f, 0.03f, 1.0f), metal);
                Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.045f, 0.6f), new Vector3(0.85f, 0.01f, 0.85f), ledCian, false);
                foreach (float x in new[] { -0.52f, 0.52f })
                    Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(x, 1.25f, 0.385f), new Vector3(0.03f, 2.2f, 0.01f), ledCian, false);
                Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0f, 2.3f, 0.13f), new Vector3(0.5f, 0.06f, 0.01f), led, false);
                var luz = new GameObject("Luz_Estacion").AddComponent<Light>();
                luz.transform.SetParent(raiz.transform, false);
                luz.transform.localPosition = new Vector3(0f, 2.2f, 0.7f);
                luz.type = LightType.Point; luz.color = new Color(0.3f, 0.85f, 1f); luz.intensity = 1.6f; luz.range = 3f; luz.shadows = LightShadows.None;
                RegistrarInteres(new Vector3(raiz.transform.position.x, 0f, raiz.transform.position.z) - d * 0.35f, -d, c, raiz, esJefe ? 2.0f : 1.4f, esJefe ? 3.2f : 2.6f);
                if (esJefe) JefePendiente = (raiz.transform.position - d * 0.9f, Quaternion.LookRotation(-d));
                else EstacionesPendientes.Add((raiz.transform.position - d * 0.6f, Quaternion.LookRotation(-d)));
            }
        }

        static readonly List<(Vector3 pos, Quaternion rot)> EstacionesPendientes = new List<(Vector3, Quaternion)>();
        static (Vector2Int celda, Vector3 pos)? CeldaJefe;
        static (Vector3 pos, Quaternion rot)? JefePendiente;
        static AndroideIA Jefe;

        /// <summary>
        /// Jefe final ('J'): SENTINEL-Ω, un androide más grande, con 60 de vida y otros colores, anclado
        /// en su cápsula hasta que se extrae el disco. Las unidades de las estaciones quedan de reserva:
        /// no despiertan con la alarma, sino cuando el jefe las llama a la mitad del combate.
        /// </summary>
        static void ColocarJefe()
        {
            Jefe = null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nexus/Prefabs/Enemigos/Androide_Sentinel.prefab");
            if (prefab == null || !JefePendiente.HasValue) return;
            var grupo = (GameObject.Find("Androides") ?? new GameObject("Androides")).transform;
            var (pos, rot) = JefePendiente.Value;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grupo);
            go.name = "SENTINEL_Omega";
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = Vector3.one * 1.45f;
            // Juntas y visor dorados en lugar de rojos: materiales propios del jefe
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.shader.name.StartsWith("Nexus/") || !m.HasProperty("_Color")) continue;
                    string ruta = $"Assets/Nexus/Materiales/Entorno/{m.name}_Jefe.mat";
                    var copia = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                    if (copia == null) { copia = new Material(m); AssetDatabase.CreateAsset(copia, ruta); }
                    else copia.CopyPropertiesFromMaterial(m);
                    Color c = m.GetColor("_Color");
                    float brillo = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    copia.SetColor("_Color", new Color(1f, 0.86f, 0.4f) * brillo);
                    EditorUtility.SetDirty(copia);
                    mats[i] = copia;
                }
                r.sharedMaterials = mats;
            }
            var ia = go.GetComponent<AndroideIA>();
            ia.Configurar(new[] { pos }, 1.2f, 3.2f, true, true);
            var so = new SerializedObject(ia);
            so.FindProperty("saludMaxima").floatValue = 60f;
            so.FindProperty("distanciaAtrapar").floatValue = 1.9f;
            so.FindProperty("radioOido").floatValue = 9f;
            so.ApplyModifiedPropertiesWithoutUndo();
            foreach (var reserva in AndroidesEstacion) { reserva.ConfigurarReserva(true); EditorUtility.SetDirty(reserva); }
            var jefe = go.AddComponent<JefeSentinel>();
            jefe.Configurar("SENTINEL-Ω", new List<AndroideIA>(AndroidesEstacion), ObjetivoDe('J'));
            EditorUtility.SetDirty(ia);
            Jefe = ia;
        }

        /// <summary>Los androides de las estaciones se ponen al final (no deben tapar la vista al calcular las cámaras).</summary>
        static void ColocarAndroidesEstacion()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nexus/Prefabs/Enemigos/Androide_Sentinel.prefab");
            if (prefab == null || EstacionesPendientes.Count == 0) return;
            var grupo = (GameObject.Find("Androides") ?? new GameObject("Androides")).transform;
            int n = 0;
            foreach (var (pos, rot) in EstacionesPendientes)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grupo);
                go.name = $"Androide_Estacion_{++n}";
                go.transform.SetPositionAndRotation(pos, rot);
                var ia = go.GetComponent<AndroideIA>();
                ia.Configurar(new[] { pos }, 1.5f, def.VelAlarma, true, true);
                EditorUtility.SetDirty(ia);
                AndroidesEstacion.Add(ia);
            }
        }

        static void Pieza(Transform padre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat, bool colision = true)
        {
            var go = GameObject.CreatePrimitive(tipo);
            if (!colision) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos; go.transform.localScale = escala;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Material LedEstacion()
        {
            const string ruta = "Assets/Nexus/Materiales/Entorno/M_LED_Estacion.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(Shader.Find("Nexus/LED Parpadeo")); AssetDatabase.CreateAsset(m, ruta); }
            m.SetColor("_Color", new Color(0.25f, 0.85f, 1f) * 1.4f);
            if (m.HasProperty("_UsarColorVertice")) m.SetFloat("_UsarColorVertice", 0f);
            if (m.HasProperty("_Parpadeo")) m.SetFloat("_Parpadeo", 0f);
            if (m.HasProperty("_Pulso")) m.SetFloat("_Pulso", 0.6f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ======================= Válvulas de refrigeración ('O') y escudo térmico =======================
        static readonly List<(Vector2Int celda, Vector3 pos)> CeldasValvula = new List<(Vector2Int, Vector3)>();

        static string NombreValvula(Vector2Int c)
        {
            // Por su posición respecto al centro del mapa: norte, sur, este u oeste
            float dx = c.x - (ancho - 1) / 2f, dy = c.y - (alto - 1) / 2f;
            return Mathf.Abs(dx) > Mathf.Abs(dy) ? (dx > 0 ? "este" : "oeste") : (dy > 0 ? "norte" : "sur");
        }

        static void ColocarValvulas(Transform objetos)
        {
            var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Nexus/Materiales/Entorno/M_Rack_Cuerpo.mat");
            var rojo = MaterialSimple("M_Valvula_Rueda", new Color(0.7f, 0.06f, 0.04f), 0.4f, 0.5f);
            var tubo = MaterialSimple("M_Valvula_Tubo", new Color(0.32f, 0.36f, 0.4f), 0.8f, 0.55f);
            foreach (var (c, p) in CeldasValvula)
            {
                var muro = MuroVecino(c) ?? Vector2Int.up;
                Vector3 d = Dir3(muro);
                float cara = CaraPared(p, d);
                var raiz = new GameObject($"Valvula_{NombreValvula(c)}");
                raiz.transform.SetParent(objetos, false);
                raiz.transform.SetPositionAndRotation(p + d * cara, Quaternion.LookRotation(-d));
                // Tubería que baja por la pared y entra al suelo, y la toma con la rueda
                Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(-0.45f, 1.5f, 0.16f), new Vector3(0.22f, 1.5f, 0.22f), tubo);
                Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0.45f, 1.5f, 0.16f), new Vector3(0.22f, 1.5f, 0.22f), tubo);
                var toma = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                toma.transform.SetParent(raiz.transform, false);
                toma.transform.localPosition = new Vector3(0f, 1.2f, 0.25f); toma.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                toma.transform.localScale = new Vector3(0.16f, 0.25f, 0.16f);
                toma.GetComponent<MeshRenderer>().sharedMaterial = tubo;
                Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0f, 1.2f, 0.12f), new Vector3(1.1f, 0.18f, 0.18f), metal);
                var rueda = new GameObject("Rueda").transform;
                rueda.SetParent(raiz.transform, false);
                rueda.localPosition = new Vector3(0f, 1.2f, 0.52f);
                var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(disco.GetComponent<Collider>());
                disco.transform.SetParent(rueda, false);
                disco.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                disco.transform.localScale = new Vector3(0.7f, 0.03f, 0.7f);
                disco.GetComponent<MeshRenderer>().sharedMaterial = rojo;
                for (int i = 0; i < 4; i++)
                {
                    var rayo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(rayo.GetComponent<Collider>());
                    rayo.transform.SetParent(rueda, false);
                    rayo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f * i);
                    rayo.transform.localPosition = new Vector3(0f, 0f, 0.05f);
                    rayo.transform.localScale = new Vector3(0.75f, 0.06f, 0.06f);
                    rayo.GetComponent<MeshRenderer>().sharedMaterial = rojo;
                }
                var luz = new GameObject("Luz_Valvula").AddComponent<Light>();
                luz.transform.SetParent(raiz.transform, false);
                luz.transform.localPosition = new Vector3(0f, 2.3f, 0.6f);
                luz.type = LightType.Point; luz.range = 4f; luz.intensity = 2.5f; luz.shadows = LightShadows.None;
                var area = raiz.AddComponent<BoxCollider>();
                area.isTrigger = true; area.center = new Vector3(0f, 1f, 1.1f); area.size = new Vector3(2.2f, 2f, 2f);
                var v = raiz.AddComponent<ValvulaRefrigeracion>();
                v.Configurar(NombreValvula(c), def.SegundosPorValvula, rueda, luz, ObjetivoDe('O'),
                             AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nexus/Prefabs/Efectos/FX_Chispas.prefab"));
                EditorUtility.SetDirty(v);
                RegistrarInteres(new Vector3(raiz.transform.position.x, 0f, raiz.transform.position.z) - d * 0.35f, -d, c, raiz, 1.2f, 2.8f);
            }
        }

        /// <summary>Escudo térmico alrededor del pedestal del disco: tres anillos de energía y una barrera sólida.</summary>
        static void CrearEscudo(Vector3 pedestal, Transform objetos)
        {
            var raiz = new GameObject("Escudo_Termico");
            raiz.transform.SetParent(objetos, false);
            raiz.transform.position = pedestal;
            var barrera = raiz.AddComponent<CapsuleCollider>();
            barrera.radius = 1.15f; barrera.height = 3.2f; barrera.center = new Vector3(0f, 1.6f, 0f);
            var anillosRaiz = new GameObject("Anillos").transform;
            anillosRaiz.SetParent(raiz.transform, false);
            anillosRaiz.localScale = Vector3.one * 2.1f;
            var mat = MaterialEscudo();
            var anillos = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                var a = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(a.GetComponent<Collider>());
                a.name = $"Anillo_{i + 1}";
                a.transform.SetParent(anillosRaiz, false);
                a.transform.localPosition = new Vector3(0f, 0.72f, 0f);
                a.transform.localScale = new Vector3(1f + i * 0.25f, 0.7f - i * 0.08f, 1f + i * 0.25f);
                var r = a.GetComponent<MeshRenderer>();
                r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                anillos[i] = a;
            }
            var luz = new GameObject("Luz_Escudo").AddComponent<Light>();
            luz.transform.SetParent(raiz.transform, false);
            luz.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            luz.type = LightType.Point; luz.color = new Color(1f, 0.45f, 0.12f); luz.intensity = 5f; luz.range = 7f; luz.shadows = LightShadows.None;
            int valvulas = def.Mapa.Sum(f => f.Count(ch => ch == 'O'));
            raiz.AddComponent<EscudoTermico>().Configurar(anillos, barrera, luz, valvulas);
        }

        static Material MaterialEscudo()
        {
            const string ruta = "Assets/Nexus/Materiales/Entorno/M_Escudo_Termico.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, ruta); }
            m.shader = Shader.Find("Universal Render Pipeline/Unlit");
            m.SetColor("_BaseColor", new Color(1f, 0.38f, 0.08f, 0.28f));
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);   // transparente aditivo: brilla
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material MaterialSimple(string nombre, Color color, float metal, float suave)
        {
            string ruta = $"Assets/Nexus/Materiales/Entorno/{nombre}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, ruta); }
            m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", suave);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ======================= Cinemáticas =======================
        static PlanoCinematica Pl(float dur, ReferenciaPlano r, Vector3 cd, Vector3 ch, Vector3 md, Vector3 mh, float fov, string quien, string texto, params AccionPlano[] acc) =>
            new PlanoCinematica { duracion = dur, referencia = r, camDesde = cd, camHasta = ch, mirarDesde = md, mirarHasta = mh, fov = fov, hablante = quien, subtitulo = texto, acciones = acc };

        static void CrearCinematicas(Transform raiz, Transform jugador)
        {
            var grupo = Grupo("Cinematicas", raiz);
            // ---- Entrada de los técnicos ----
            if (def.Intro && Entrada.HasValue)
            {
                var (_, F, dIn, puerta) = Entrada.Value;
                Vector3 sR = Vector3.Cross(Vector3.up, dIn), arriba = Vector3.up, R = F + dIn * Celda;
                var planos = new[]
                {
                    Pl(4.4f, ReferenciaPlano.Mundo, R + dIn * 2.4f + sR * 1.4f + arriba * 1.2f, R + dIn * 2.1f + sR * 1.1f + arriba * 1.3f,
                       F + arriba * 1.3f, R + dIn * 0.4f + arriba * 1.35f, 52f, null, null, AccionPlano.AbrirPuerta, AccionPlano.Caminar),
                    Pl(3.8f, ReferenciaPlano.Jugador, new Vector3(0.28f, 1.62f, 1.5f), new Vector3(0.2f, 1.62f, 1.15f),
                       new Vector3(0f, 1.57f, 0f), new Vector3(0f, 1.57f, 0f), 40f, "{NOMBRE}", "¿Qué pasó aquí...?", AccionPlano.MirarAlrededor),
                    Pl(4.0f, ReferenciaPlano.Jugador, new Vector3(0.45f, 1.75f, -0.75f), new Vector3(0.5f, 1.75f, -0.6f),
                       new Vector3(-3.5f, 1.0f, 4f), new Vector3(3.5f, 1.1f, 4f), 56f, "{NOMBRE}", "Luces de emergencia... y ni un alma del turno de noche."),
                    Pl(2.8f, ReferenciaPlano.Mundo, R + dIn * 2.6f - sR * 1.4f + arriba * 2.0f, R + dIn * 2.4f - sR * 1.2f + arriba * 1.9f,
                       F + arriba * 1.2f, F + arriba * 1.2f, 55f, "SISTEMA", "Puerta de servicio sellada. Protocolo CERBERUS activo.", AccionPlano.CerrarPuerta),
                    Pl(3.0f, ReferenciaPlano.Jugador, new Vector3(-0.3f, 1.6f, 1.35f), new Vector3(-0.25f, 1.6f, 1.15f),
                       new Vector3(0f, 1.55f, 0f), new Vector3(0f, 1.55f, 0f), 42f, "{NOMBRE}", "Genial. Ya no hay vuelta atrás: los tres discos o nada."),
                };
                planos[0].destino = R + dIn * 1.2f;
                var intro = new GameObject("Cinematica_Entrada").AddComponent<Cinematica>();
                intro.transform.SetParent(grupo, false);
                intro.Configurar(Cinematica.Momento.AlEmpezarNivel, planos, puerta, new List<AndroideIA>(), null);
                intro.ConfigurarInicio(F - dIn * 0.9f, Quaternion.LookRotation(dIn).eulerAngles.y);
                EditorUtility.SetDirty(intro);
            }

            // ---- Alarma al extraer el disco (con jefe: CERBERUS se descarga en SENTINEL-Ω) ----
            var unidades = Jefe != null ? new List<AndroideIA> { Jefe } : new List<AndroideIA>(AndroidesEstacion);
            if (Jefe == null)
                foreach (var a in Object.FindObjectsByType<AndroideIA>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (!unidades.Contains(a) && a.name.Contains("Alarma")) unidades.Add(a);
            bool hayDisco = false;
            for (int y = 0; y < alto && !hayDisco; y++) for (int x = 0; x < ancho; x++) if (En(new Vector2Int(x, y)) == 'D') { hayDisco = true; break; }
            if (hayDisco && unidades.Count > 0)
            {
                var planos = new List<PlanoCinematica>
                {
                    Pl(2.6f, ReferenciaPlano.Jugador, new Vector3(1.3f, 2.3f, 2.4f), new Vector3(1.0f, 2.1f, 2.0f), new Vector3(0f, 1.1f, 0f), new Vector3(0f, 1.2f, 0f),
                       50f, "SISTEMA", def.DialogoDisco != null && def.DialogoDisco.Length > 0 ? def.DialogoDisco[0] : "¡Extracción no autorizada del Disco Maestro! Alarma general.", AccionPlano.Alarma),
                };
                planos[0].temblor = 0.03f;
                for (int i = 0; i < Mathf.Min(3, unidades.Count); i++)
                {
                    var p = Jefe != null
                        ? Pl(3.2f, ReferenciaPlano.Androide, new Vector3(1.0f, 1.4f, 4.2f), new Vector3(0.7f, 2.0f, 3.3f), new Vector3(0f, 2.0f, 0f), new Vector3(0f, 2.3f, 0f),
                             50f, "CERBERUS", "Transferencia completa. Unidad prototipo SENTINEL-Ω en línea. Salida de emergencia bloqueada.", AccionPlano.DespertarAndroide)
                        : Pl(1.8f, ReferenciaPlano.Androide, new Vector3(0.55f, 1.75f, 2.1f), new Vector3(0.35f, 1.7f, 1.6f), new Vector3(0f, 1.55f, 0f), new Vector3(0f, 1.62f, 0f),
                             45f, i == 0 ? "SENTINEL" : null, i == 0 ? "Anomalía localizada. Todas las unidades: purgar." : null, AccionPlano.DespertarAndroide);
                    p.androide = i;
                    planos.Add(p);
                }
                planos.Add(Pl(2.4f, ReferenciaPlano.Jugador, new Vector3(0.4f, 1.65f, 1.35f), new Vector3(0.3f, 1.65f, 1.1f), new Vector3(0f, 1.55f, 0f), new Vector3(0f, 1.55f, 0f),
                              42f, "{NOMBRE}", Jefe != null ? "Esa cosa está entre la salida y yo... No hay otra opción." : "¡Tengo que salir de aquí... ya!"));
                var alarma = new GameObject("Cinematica_Alarma").AddComponent<Cinematica>();
                alarma.transform.SetParent(grupo, false);
                alarma.Configurar(Cinematica.Momento.AlExtraerDisco, planos.ToArray(), null, unidades, ObjetivoDe('D'));
                EditorUtility.SetDirty(alarma);
            }

            // ---- Final del juego (último nivel): el reinicio manual ----
            if (string.IsNullOrEmpty(def.EscenaSiguiente))
            {
                var planos = new[]
                {
                    Pl(3.4f, ReferenciaPlano.Jugador, new Vector3(0.9f, 1.8f, -1.6f), new Vector3(0.75f, 1.75f, -1.3f), new Vector3(0f, 1.3f, 1.2f), new Vector3(0f, 1.35f, 1.3f),
                       48f, "SISTEMA", "Tres Discos Duros Maestros detectados. Iniciando el reinicio manual..."),
                    Pl(4.4f, ReferenciaPlano.Jugador, new Vector3(-2.2f, 3.4f, -2.8f), new Vector3(-1.6f, 3.0f, -2.2f), new Vector3(0f, 1.0f, 2.0f), new Vector3(0f, 1.1f, 1.5f),
                       60f, "SISTEMA", "CERBERUS 7.0 restaurado. Unidades SENTINEL desconectadas. Publicando los registros del día 14.", AccionPlano.Restaurar),
                    Pl(3.6f, ReferenciaPlano.Jugador, new Vector3(1.3f, 1.62f, 0.45f), new Vector3(1.05f, 1.62f, 0.35f), new Vector3(0f, 1.57f, 0f), new Vector3(0f, 1.57f, 0f),
                       42f, "{NOMBRE}", "Se acabó... y ahora todos van a saber lo que pasó aquí."),
                };
                var final = new GameObject("Cinematica_Final").AddComponent<Cinematica>();
                final.transform.SetParent(grupo, false);
                final.Configurar(Cinematica.Momento.AlTerminarJuego, planos, null, new List<AndroideIA>(), null);
                EditorUtility.SetDirty(final);
            }
        }
    }
}
