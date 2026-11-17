// Prueba de huida del Nivel 1: toma el disco, salta la cinemática de alarma y corre (5 m/s) por la
// ruta más corta hasta el montacargas. Mide la distancia mínima a los androides y si alguno lo atrapa.
int alto = 16;
UnityEngine.Vector3 W(float x, float y) => new UnityEngine.Vector3(x * 3f + 1.5f, 0f, (alto - 1 - y) * 3f + 1.5f);
var ruta = new[] { W(7, 3.4f), W(7, 5), W(7, 6), W(7, 7), W(7, 11), W(7, 12), W(9, 13), W(10, 13), W(11, 13), W(12, 12.3f) };
var j = UnityEngine.GameObject.FindGameObjectWithTag("Player").transform;
Nexus.Cinematica.SaltoSimulado = true;
for (int q = 0; q < 40; q++) await UnityEngine.Awaitable.NextFrameAsync();
foreach (var pu in UnityEngine.Object.FindObjectsByType<Nexus.PuertaSeguridad>(UnityEngine.FindObjectsSortMode.None)) if (!pu.Sellada) { pu.Energizar(); pu.Abrir(); }
Nexus.NivelNexus.Teletransportar(j, ruta[0]);
Nexus.Coleccionable disco = null;
foreach (var c in UnityEngine.Object.FindObjectsByType<Nexus.Coleccionable>(UnityEngine.FindObjectsSortMode.None)) if (c.Tipo == Nexus.TipoColeccionable.DiscoDuro) disco = c;
disco.transform.position = j.position + UnityEngine.Vector3.up;
for (int q = 0; q < 30; q++) await UnityEngine.Awaitable.NextFrameAsync();
Nexus.Cinematica.SaltoSimulado = true;   // salta la cinemática de alarma
while (Nexus.Cinematica.EnCurso) await UnityEngine.Awaitable.NextFrameAsync();
await UnityEngine.Awaitable.WaitForSecondsAsync(REACCIONf);   // lo que tarda el jugador en reaccionar
int salud0 = Nexus.EstadoPartida.Salud; float minimo = 99f, t = 0f; string quien = "";
for (int k = 1; k < ruta.Length; k++)
{
    var desde = ruta[k - 1]; var hasta = ruta[k]; var dir = hasta - desde; float largo = dir.magnitude; dir /= largo;
    j.rotation = UnityEngine.Quaternion.LookRotation(dir);
    for (float d = 0f; d < largo; )
    {
        float dt = UnityEngine.Time.deltaTime; d += 5f * dt; t += dt;
        Nexus.NivelNexus.Teletransportar(j, desde + dir * UnityEngine.Mathf.Min(d, largo));
        await UnityEngine.Awaitable.NextFrameAsync();
        foreach (var a in Nexus.AndroideIA.Todos)
        {
            float dist = UnityEngine.Vector3.Distance(a.transform.position, j.position);
            if (dist < minimo) { minimo = dist; quien = a.name; }
        }
    }
}
var s = new System.Text.StringBuilder($"huida {t:0.0} s · atrapado {salud0 - Nexus.EstadoPartida.Salud} veces · androide más cercano {minimo:0.0} m ({quien}) |");
foreach (var a in Nexus.AndroideIA.Todos) s.Append($" {a.name}:{a.EstadoActual} a {UnityEngine.Vector3.Distance(a.transform.position, j.position):0} m");
return s.ToString();
