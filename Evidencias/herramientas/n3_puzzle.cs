// Nivel 3 — prueba del puzzle: lee las presiones, abre una válvula fuera de orden (sobrepresión) y
// después las tres en el orden correcto. Devuelve lo que pasó en cada paso.
var sb = new System.Text.StringBuilder();
var nivel = Nexus.NivelNexus.Instancia;
var vs = new System.Collections.Generic.List<Nexus.ValvulaRefrigeracion>(Nexus.ValvulaRefrigeracion.Todas);
foreach (var v in vs) sb.Append(v.TextoAccion + " | ");
vs.Sort((a, b) => a.Presion.CompareTo(b.Presion));
async System.Threading.Tasks.Task CerrarDialogos()
{
    for (int i = 0; i < 20 && Nexus.HUDNexus.DialogoAbierto; i++) { Nexus.HUDNexus.AvanceSimulado = true; for (int f = 0; f < 15; f++) await UnityEngine.Awaitable.NextFrameAsync(); }
}
float t0 = nivel.TiempoRestante;
// 1) Orden equivocado: primero la de presión media
vs[1].Interactuar(null);
await UnityEngine.Awaitable.NextFrameAsync();
sb.Append($"\nFuera de orden ({vs[1].Nombre}): abiertas {Nexus.EscudoTermico.Abiertas}, dialogo={Nexus.HUDNexus.DialogoAbierto}, tiempo {t0:0.0}→{nivel.TiempoRestante:0.0}");
await CerrarDialogos();
// 2) La menor, luego otra fuera de orden después de una correcta
vs[0].Interactuar(null);
float t1 = nivel.TiempoRestante;
vs[2].Interactuar(null);
await UnityEngine.Awaitable.NextFrameAsync();
sb.Append($"\nCorrecta ({vs[0].Nombre}) y luego mal ({vs[2].Nombre}): abiertas {Nexus.EscudoTermico.Abiertas}, {vs[0].Nombre} abierta={vs[0].Abierta}, tiempo {t1:0.0}→{nivel.TiempoRestante:0.0}");
await CerrarDialogos();
// 3) Orden correcto completo
float t2 = nivel.TiempoRestante;
foreach (var v in vs) v.Interactuar(null);
for (int f = 0; f < 30; f++) await UnityEngine.Awaitable.NextFrameAsync();
var escudo = UnityEngine.GameObject.Find("Escudo_Termico");
sb.Append($"\nEn orden ({vs[0].Nombre}, {vs[1].Nombre}, {vs[2].Nombre}): abiertas {Nexus.EscudoTermico.Abiertas}/{Nexus.EscudoTermico.Total}, barrera activa={escudo.GetComponent<UnityEngine.Collider>().enabled}, tiempo {t2:0.0}→{nivel.TiempoRestante:0.0}");
return sb.ToString();
