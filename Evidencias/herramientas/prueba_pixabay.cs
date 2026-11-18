// Sonidos de Pixabay en el juego: capas de música, efectos 3D, pisadas variadas, quejido de Deisy y final
var sb = new System.Text.StringBuilder();
async System.Threading.Tasks.Task Esperar(float s) { float fin = UnityEngine.Time.realtimeSinceStartup + s; while (UnityEngine.Time.realtimeSinceStartup < fin) await UnityEngine.Awaitable.NextFrameAsync(); }
Nexus.Cinematica.SaltoSimulado = true;
await Esperar(3f);
sb.Append("Escena " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ": " + Nexus.AudioNexus.Estado + "\n");
var j = UnityEngine.GameObject.FindGameObjectWithTag("Player").transform;
string Sonando() { var l = new System.Collections.Generic.List<string>(); foreach (var s in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsSortMode.None)) if (s.isPlaying && s.clip != null && s.spatialBlend > 0.5f) l.Add(s.clip.name); return string.Join(",", l); }
// Cada efecto en 3D, a 3 m del técnico
int ok = 0; var malos = new System.Collections.Generic.List<string>();
foreach (Nexus.Sfx t in System.Enum.GetValues(typeof(Nexus.Sfx)))
{
    if (t == Nexus.Sfx.Victoria || t == Nexus.Sfx.GameOver) continue;
    Nexus.AudioNexus.Efecto(t, j.position + j.forward * 3f, 0.01f);
    await UnityEngine.Awaitable.NextFrameAsync();
    bool hay = false;
    foreach (var s in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsSortMode.None))
        if (s.isPlaying && s.clip != null && s.clip.name.StartsWith("Px_") && s.clip.name.Contains(t == Nexus.Sfx.JugadorPaso ? "JugadorPaso" : t.ToString())) hay = true;
    if (hay) ok++; else malos.Add(t.ToString());
    await Esperar(0.35f);   // la separación mínima de algunos tipos
}
sb.Append($"Efectos 3D de Pixabay sonando: {ok}" + (malos.Count > 0 ? " · sin sonar: " + string.Join(",", malos) : "") + "\n");
// Pisadas: cuántas tomas distintas salen en 24 pasos
var banco = UnityEngine.Resources.Load<Nexus.BancoSonidos>("BancoSonidos");
var distintas = new System.Collections.Generic.HashSet<string>();
for (int i = 0; i < 24; i++) distintas.Add(banco.Clip(Nexus.Sfx.JugadorPaso).name);
sb.Append("Pisadas distintas en 24 pasos: " + distintas.Count + "\n");
// Quejido según el personaje
var vida = j.GetComponent<Nexus.VidaJugador>();
var campo = typeof(Nexus.VidaJugador).GetField("invulnerableHasta", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
foreach (int p in new[] { 0, 1 })
{
    Nexus.EstadoPartida.PersonajeElegido = p; Nexus.EstadoPartida.Salud = Nexus.EstadoPartida.SaludMaxima;
    campo.SetValue(vida, 0f);
    vida.RecibirDanio(j.position + j.forward);
    await UnityEngine.Awaitable.NextFrameAsync();
    string q = ""; foreach (var s in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsSortMode.None)) if (s.isPlaying && s.clip != null && s.clip.name.Contains("Herid")) q = s.clip.name;
    sb.Append((p == 0 ? "Neyder" : "Deisy") + " herido → " + q + "\n");
    await Esperar(0.5f);
}
Nexus.EstadoPartida.PersonajeElegido = 0; Nexus.EstadoPartida.Salud = Nexus.EstadoPartida.SaludMaxima;
campo.SetValue(vida, 1e9f);
// Persecución y alarma
Nexus.AndroideIA.AlarmaGeneral(8f);
Nexus.AlarmaNivel.Activar();
await Esperar(3f);
sb.Append("Persecución + alarma: " + Nexus.AudioNexus.Estado + "\n");
Nexus.AlarmaNivel.Desactivar();
// Final: la pieza de Victoria suena y se corta al volver al menú
Nexus.AudioNexus.Final(true);
await Esperar(1f);
var inter = UnityEngine.GameObject.Find("AudioNexus/Interfaz").GetComponent<UnityEngine.AudioSource>();
sb.Append("Victoria sonando: " + inter.isPlaying + "\n");
UnityEngine.SceneManagement.SceneManager.LoadScene("00_Menu");
await Esperar(3f);
sb.Append("En el menú: interfaz sonando=" + inter.isPlaying + " · " + Nexus.AudioNexus.Estado + "\n");
return sb.ToString();
