// Transición del ascensor: Nivel actual → siguiente, con capturas de cada parte
var sb = new System.Text.StringBuilder();
// Las capturas quedan junto al proyecto (carpeta capt_ascensor, al lado de Nexus/)
var dir = System.IO.Path.GetFullPath("../capt_ascensor") + System.IO.Path.DirectorySeparatorChar;
System.IO.Directory.CreateDirectory(dir);
async System.Threading.Tasks.Task Esperar(float s) { float fin = UnityEngine.Time.realtimeSinceStartup + s; while (UnityEngine.Time.realtimeSinceStartup < fin) await UnityEngine.Awaitable.NextFrameAsync(); }
async System.Threading.Tasks.Task Foto(string n) { await UnityEngine.Awaitable.EndOfFrameAsync(); var tx = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture(); System.IO.File.WriteAllBytes(dir + n + ".png", UnityEngine.ImageConversion.EncodeToPNG(tx)); UnityEngine.Object.Destroy(tx); }
Nexus.Cinematica.SaltoSimulado = true;
await Esperar(3f);
string escena0 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
var salida = UnityEngine.Object.FindFirstObjectByType<Nexus.SalidaNivel>();
var j = UnityEngine.GameObject.FindGameObjectWithTag("Player").transform;
Nexus.NivelNexus.Instancia.DiscoObtenido = true;
var a = salida.transform;
Nexus.NivelNexus.Teletransportar(j, a.TransformPoint(new UnityEngine.Vector3(0.3f, 0f, -2.2f)));
j.rotation = UnityEngine.Quaternion.LookRotation(-a.forward);
await Esperar(0.5f);
float t0 = UnityEngine.Time.realtimeSinceStartup;
salida.Interactuar(j.GetComponent<Nexus.InteraccionJugador>());
await Esperar(1.1f); await Foto(escena0 + "_1_puertas");
await Esperar(0.8f); await Foto(escena0 + "_2_entrando");
sb.Append($"Entrada: fase={Nexus.TransicionAscensor.Fase} distancia a la puerta={UnityEngine.Vector3.Distance(j.position, a.TransformPoint(new UnityEngine.Vector3(0f, 0f, -0.5f))):0.00} m · EnCurso={Nexus.Cinematica.EnCurso} pausados={Nexus.AndroideIA.Pausados}\n");
while (Nexus.TransicionAscensor.Fase == "entrar") await UnityEngine.Awaitable.NextFrameAsync();
float tv = UnityEngine.Time.realtimeSinceStartup;
sb.Append($"Entrada duró {tv - t0:0.0} s\n");
await Esperar(1.45f); await Foto(escena0 + "_3_cabina");
sb.Append("Viaje: " + Nexus.AudioNexus.Estado + "\n");
await Esperar(2.25f); await Foto(escena0 + "_4_cabina_piso");
while (Nexus.TransicionAscensor.Fase == "viaje") await UnityEngine.Awaitable.NextFrameAsync();
sb.Append($"Viaje duró {UnityEngine.Time.realtimeSinceStartup - tv:0.0} s\n");
while (Nexus.TransicionAscensor.Fase != "llegada" && Nexus.TransicionAscensor.EnCurso) await UnityEngine.Awaitable.NextFrameAsync();
await Esperar(0.45f); await Foto(escena0 + "_5_llegada");
while (Nexus.TransicionAscensor.EnCurso) await UnityEngine.Awaitable.NextFrameAsync();
await Esperar(0.3f); await Foto(escena0 + "_6_nivel_nuevo");
var j2 = UnityEngine.GameObject.FindGameObjectWithTag("Player");
sb.Append($"{escena0} → {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} en {UnityEngine.Time.realtimeSinceStartup - t0:0.0} s · EnCurso={Nexus.Cinematica.EnCurso} pausados={Nexus.AndroideIA.Pausados} puedeMoverse={j2.GetComponent<Nexus.ControladorJugador>().PuedeMoverse} · {Nexus.AudioNexus.Estado}\n");
sb.Append("Cabinas que quedaron: " + (UnityEngine.GameObject.Find("Cabina_Ascensor") != null) + " · transiciones vivas: " + (UnityEngine.GameObject.Find("Transicion_Ascensor") != null));
return sb.ToString();
