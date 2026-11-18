// Nivel 3 completo desde el disco: cinemática del jefe, combate contra SENTINEL-Ω (el técnico se
// mueve entre dos puntos del salón y dispara solo cuando el jefe está expuesto y lo ve), reinicio
// manual y victoria. Guarda capturas de la pantalla del juego (con HUD) en CARPETA.
string carpeta = @"CARPETA";
int nFoto = 0; var log = new System.Text.StringBuilder();
async System.Threading.Tasks.Task Foto(string nombre)
{
    await UnityEngine.Awaitable.EndOfFrameAsync();
    var tex = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(carpeta, $"{++nFoto:00}_{nombre}.png"), UnityEngine.ImageConversion.EncodeToPNG(tex));
    UnityEngine.Object.Destroy(tex);
}
async System.Threading.Tasks.Task Esperar(float s) { float fin = UnityEngine.Time.realtimeSinceStartup + s; while (UnityEngine.Time.realtimeSinceStartup < fin) await UnityEngine.Awaitable.NextFrameAsync(); }
int alto = 21;
UnityEngine.Vector3 W(float x, float y) => new UnityEngine.Vector3(x * 3f + 1.5f, 0f, (alto - 1 - y) * 3f + 1.5f);
var j = UnityEngine.GameObject.FindGameObjectWithTag("Player").transform;
var arma = j.GetComponent<Nexus.ArmaJugador>();
var vida = j.GetComponent<Nexus.VidaJugador>();
typeof(Nexus.VidaJugador).GetField("invulnerableHasta", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(vida, 1e9f);
var inv = j.GetComponent<Nexus.InventarioJugador>();
inv.Agregar(Nexus.TipoColeccionable.Pistola, 1); inv.Agregar(Nexus.TipoColeccionable.BalasPistola, 40);
arma.Equipar(Nexus.TipoArma.Pistola);
// Las patrullas no estorban en esta prueba: solo el jefe y sus refuerzos
foreach (var a in new System.Collections.Generic.List<Nexus.AndroideIA>(Nexus.AndroideIA.Todos)) if (!a.name.Contains("Omega") && !a.name.Contains("Estacion")) a.gameObject.SetActive(false);
// Disco
Nexus.Coleccionable disco = null;
foreach (var c in UnityEngine.Object.FindObjectsByType<Nexus.Coleccionable>(UnityEngine.FindObjectsSortMode.None)) if (c.Tipo == Nexus.TipoColeccionable.DiscoDuro) disco = c;
Nexus.NivelNexus.Teletransportar(j, W(11, 12.4f)); j.rotation = UnityEngine.Quaternion.Euler(0, 0, 0);
disco.transform.position = j.position + UnityEngine.Vector3.up;
await Esperar(1.5f); await Foto("alarma");
await Esperar(2.4f); await Foto("jefe_se_enciende");
await Esperar(2.6f); await Foto("jefe_en_linea");
while (Nexus.Cinematica.EnCurso) await UnityEngine.Awaitable.NextFrameAsync();
var jefe = Nexus.JefeSentinel.Instancia; var ia = jefe.GetComponent<Nexus.AndroideIA>();
var puntos = new[] { W(6, 13), W(16, 13), W(16, 7), W(6, 7) }; int destino = 0;
float t = 0f; int cargas = 0; bool cargandoAntes = false, fotoCarga = false, fotoExpuesto = false, fotoFase2 = false;
int balas0 = Nexus.EstadoPartida.CargadorPistola + inv.Cantidad(Nexus.TipoColeccionable.BalasPistola);
while (!Nexus.JefeSentinel.Derrotado && t < 180f)
{
    float dt = UnityEngine.Time.deltaTime; t += dt;
    var h = ia.transform.position - j.position; h.y = 0f; float d = h.magnitude;
    bool expuesto = ia.MultiplicadorDanio > 1.5f, vulnerable = ia.MultiplicadorDanio >= 1f;
    bool Ve(UnityEngine.Vector3 desde) { var o2 = desde + UnityEngine.Vector3.up * 1.35f; var p2 = ia.Pecho; return !UnityEngine.Physics.Raycast(o2, (p2 - o2).normalized, out var h2, (p2 - o2).magnitude, ~(1 << 2), UnityEngine.QueryTriggerInteraction.Ignore) || h2.collider.GetComponentInParent<Nexus.AndroideIA>() == ia; }
    // Como un jugador: si el jefe se acerca, corre hacia el punto del salón más lejano desde el que lo siga viendo
    if (!expuesto && d < 8f)
    {
        float mejor = -1f; for (int k = 0; k < puntos.Length; k++) { float dk = (puntos[k] - ia.transform.position).magnitude; if (Ve(puntos[k]) && dk > mejor) { mejor = dk; destino = k; } }
        var ir = puntos[destino] - j.position; ir.y = 0f;
        if (ir.magnitude > 0.5f) Nexus.NivelNexus.Teletransportar(j, j.position + ir.normalized * 5f * dt);
    }
    j.rotation = UnityEngine.Quaternion.LookRotation(h.normalized);
    arma.ApuntarSimulado = vulnerable;
    bool linea = Ve(j.position);
    if (vulnerable && linea && UnityEngine.Time.frameCount % 6 == 0) arma.DisparoSimulado = true;
    if (jefe.Cargando && !cargandoAntes) cargas++;
    cargandoAntes = jefe.Cargando;
    await UnityEngine.Awaitable.NextFrameAsync();
    if (jefe.Cargando && !fotoCarga && ia.MultiplicadorDanio > 1f) { fotoCarga = true; await Esperar(1.0f); await Foto("jefe_cargando"); }
    else if (cargas >= 2 && expuesto && !fotoExpuesto) { fotoExpuesto = true; await Foto("jefe_expuesto_disparos"); }
    else if (!fotoFase2 && ia.Salud <= ia.SaludMaxima * 0.5f) { fotoFase2 = true; await Esperar(0.5f); await Foto("jefe_llama_refuerzos"); }
}
arma.ApuntarSimulado = false;
int balas1 = Nexus.EstadoPartida.CargadorPistola + inv.Cantidad(Nexus.TipoColeccionable.BalasPistola);
log.Append($"jefe derrotado={Nexus.JefeSentinel.Derrotado} en {t:0.0} s · cargas {cargas} · balas usadas {balas0 - balas1} · vida {ia.Salud:0}/{ia.SaludMaxima:0}");
await Esperar(1.2f); await Foto("jefe_destruido");
// Reinicio manual en la salida de emergencia
Nexus.NivelNexus.Teletransportar(j, W(11, 18.6f)); j.rotation = UnityEngine.Quaternion.Euler(0, 180, 0);
await Esperar(0.6f);
foreach (var s in UnityEngine.Object.FindObjectsByType<Nexus.SalidaNivel>(UnityEngine.FindObjectsSortMode.None)) s.Interactuar(j.GetComponent<Nexus.InteraccionJugador>());
await Esperar(1.8f); await Foto("final_discos_detectados");
await Esperar(3.4f); await Foto("final_cerberus_restaurado");
await Esperar(4.2f); await Foto("final_se_acabo");
await Esperar(3.5f); await Foto("victoria");
log.Append($" · final: escala de tiempo {UnityEngine.Time.timeScale} · fotos {nFoto}");
return log.ToString();
