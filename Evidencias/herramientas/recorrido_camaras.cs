// Recorre el nivel caminando (1,8 m/s) por una lista de celdas (columna, fila del mapa de texto) y
// mide las cámaras: cortes, tamaño del técnico en pantalla y tiempo fuera de cuadro o tapado.
int alto = ALTO;
var ruta = new UnityEngine.Vector2[] { RUTA };
var j = UnityEngine.GameObject.FindGameObjectWithTag("Player").transform;
foreach (var a in new System.Collections.Generic.List<Nexus.AndroideIA>(Nexus.AndroideIA.Todos)) a.gameObject.SetActive(false);
UnityEngine.Vector3 W(UnityEngine.Vector2 c) => new UnityEngine.Vector3(c.x * 3f + 1.5f, 0f, (alto - 1 - c.y) * 3f + 1.5f);
var g = Nexus.GestorCamaras.Instancia;
Nexus.NivelNexus.Teletransportar(j, W(ruta[0]));
for (int i = 0; i < 20; i++) await UnityEngine.Awaitable.NextFrameAsync();
int cortes0 = g.Cambios; float tiempo = 0f, fuera = 0f, chico = 0f, minimo = 1f, suma = 0f; int muestras = 0;
string ultima = g.Activa.name; var camaras = new System.Collections.Generic.List<string> { ultima };
for (int k = 1; k < ruta.Length; k++)
{
    var desde = W(ruta[k - 1]); var hasta = W(ruta[k]);
    var dir = (hasta - desde); float largo = dir.magnitude; dir /= largo;
    j.rotation = UnityEngine.Quaternion.LookRotation(dir);
    for (float d = 0f; d < largo; )
    {
        float dt = UnityEngine.Time.deltaTime; d += 1.8f * dt;
        Nexus.NivelNexus.Teletransportar(j, desde + dir * UnityEngine.Mathf.Min(d, largo));
        await UnityEngine.Awaitable.NextFrameAsync();
        var cam = g.Activa; tiempo += dt;
        var (dentro, visible, _) = Nexus.GestorCamaras.Evaluar(cam, j.position, 0f, 0.3f);
        if (!dentro || !visible) fuera += dt;
        float t = Nexus.GestorCamaras.TamanoEnPantalla(cam, j.position);
        if (t < 0.09f) chico += dt;
        minimo = UnityEngine.Mathf.Min(minimo, t); suma += t; muestras++;
        if (cam.name != ultima) { ultima = cam.name; camaras.Add(ultima); }
    }
}
return $"recorrido {tiempo:0.0} s · cortes {g.Cambios - cortes0} · fuera de cuadro o tapado {fuera:0.0} s · más pequeño que 9% {chico:0.0} s · tamaño mínimo {minimo:0.00} medio {suma / muestras:0.00}\n" + string.Join(" > ", camaras);
