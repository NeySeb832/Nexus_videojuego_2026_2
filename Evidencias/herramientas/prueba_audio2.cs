// Persecución (capa de tensión) y efecto 3D de un disparo
var sb = new System.Text.StringBuilder();
async System.Threading.Tasks.Task Esperar(float s) { float fin = UnityEngine.Time.realtimeSinceStartup + s; while (UnityEngine.Time.realtimeSinceStartup < fin) await UnityEngine.Awaitable.NextFrameAsync(); }
Nexus.Cinematica.SaltoSimulado = true;
await Esperar(3f);
var j = UnityEngine.GameObject.FindGameObjectWithTag("Player").transform;
typeof(Nexus.VidaJugador).GetField("invulnerableHasta", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(j.GetComponent<Nexus.VidaJugador>(), 1e9f);
Nexus.AndroideIA.AlarmaGeneral(8f);   // todos cazan al técnico
await Esperar(2.5f);
int cazando = 0; foreach (var a in Nexus.AndroideIA.Todos) if (a.EstadoActual == Nexus.AndroideIA.Estado.Persecucion) cazando++;
sb.Append($"Persecución ({cazando} androides cazando): {Nexus.AudioNexus.Estado}\n");
Nexus.AudioNexus.Efecto(Nexus.Sfx.DisparoEscopeta, j.position + j.forward * 3f);
await UnityEngine.Awaitable.NextFrameAsync();
foreach (var s in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsSortMode.None))
    if (s.isPlaying && s.clip != null && s.clip.name.StartsWith("Sfx_")) sb.Append($"sonando: {s.clip.name} 3D={s.spatialBlend:0} a {UnityEngine.Vector3.Distance(s.transform.position, j.position):0.0} m · ");
return sb.ToString();
