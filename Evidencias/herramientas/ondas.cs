// Lee los WAV generados, mide pico y volumen medio (RMS) y dibuja la forma de onda de algunos en un PNG
var carpeta = "Assets/Nexus/Audio";
var sb = new System.Text.StringBuilder();
float[] Leer(string ruta, out int sr)
{
    var bytes = System.IO.File.ReadAllBytes(ruta);
    sr = System.BitConverter.ToInt32(bytes, 24);
    int n = (bytes.Length - 44) / 2; var d = new float[n];
    for (int i = 0; i < n; i++) d[i] = System.BitConverter.ToInt16(bytes, 44 + i * 2) / 32768f;
    return d;
}
foreach (var f in System.IO.Directory.GetFiles(carpeta, "*.wav"))
{
    var d = Leer(f, out int sr); double s = 0; float pico = 0; int malos = 0;
    foreach (var v in d) { if (float.IsNaN(v)) malos++; s += v * v; pico = System.Math.Max(pico, System.Math.Abs(v)); }
    // Salto en el punto de bucle (último → primero): si es grande, se oiría un clic al repetir
    float salto = System.Math.Abs(d[d.Length - 1] - d[0]);
    sb.Append($"{System.IO.Path.GetFileNameWithoutExtension(f)}: {d.Length / (float)sr:0.00}s pico {pico:0.00} rms {System.Math.Sqrt(s / d.Length):0.000} salto {salto:0.000}{(malos > 0 ? " NaN!" : "")}\n");
}
// Formas de onda
var lista = new[] { "Musica_Exploracion", "Musica_Jefe", "Alarma_Sirena", "Sfx_DisparoEscopeta", "Sfx_AndroideDespierta", "Sfx_Puerta", "Sfx_JefeCarga", "Sfx_Recoger" };
int W = 1200, H = 90 * lista.Length;
var tex = new UnityEngine.Texture2D(W, H);
var fondo = new UnityEngine.Color(0.06f, 0.08f, 0.1f);
var px = new UnityEngine.Color[W * H]; for (int i = 0; i < px.Length; i++) px[i] = fondo;
for (int k = 0; k < lista.Length; k++)
{
    var d = Leer($"{carpeta}/{lista[k]}.wav", out int sr);
    int baseY = H - (k + 1) * 90 + 45;
    var col = UnityEngine.Color.HSVToRGB(k / (float)lista.Length, 0.6f, 1f);
    for (int x = 0; x < W; x++)
    {
        int a = (int)((long)d.Length * x / W), b = (int)((long)d.Length * (x + 1) / W);
        float mn = 0, mx = 0; for (int i = a; i < b; i++) { mn = System.Math.Min(mn, d[i]); mx = System.Math.Max(mx, d[i]); }
        for (int y = (int)(baseY + mn * 40); y <= (int)(baseY + mx * 40); y++) if (y >= 0 && y < H) px[y * W + x] = col;
    }
    for (int x = 0; x < W; x++) px[(H - (k + 1) * 90) * W + x] = new UnityEngine.Color(0.2f, 0.25f, 0.3f);
}
tex.SetPixels(px); tex.Apply();
System.IO.File.WriteAllBytes(@"SALIDA", UnityEngine.ImageConversion.EncodeToPNG(tex));
sb.Append("orden de las ondas (de arriba abajo): " + string.Join(", ", lista));
return sb.ToString();
