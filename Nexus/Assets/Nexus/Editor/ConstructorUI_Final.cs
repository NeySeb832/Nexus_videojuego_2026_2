using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Pantalla final (Canvas 3) con estética de fallo crítico de sistema: anillos del "ojo" SENTINEL,
    /// título glitch, retrato con estática, registro de terminal, pulso que se aplana e informe.
    /// </summary>
    public static partial class ConstructorUI
    {
        const string RutaFinal = RutaUI + "/Final";

        static RectTransform ConstruirPantallaFinal(GameObject raiz, Sprite blanco)
        {
            GenerarRecursosFinal();
            Texture2D Tex(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{RutaFinal}/{n}.png");
            Sprite Spr(string n) => AssetDatabase.LoadAssetAtPath<Sprite>($"{RutaFinal}/{n}.png");
            var marco = CargarSprite("Marco");
            var acentos = new System.Collections.Generic.List<Graphic>();
            var gris = new Color(0.75f, 0.75f, 0.78f);

            var canvas = NuevoCanvas("Canvas_Final", raiz.transform, 10);

            Image Lleno(string nombre, Transform padre, Sprite s, Color c)
            {
                var img = Icono(nombre, padre, s, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                img.preserveAspect = false;
                img.rectTransform.anchorMin = Vector2.zero; img.rectTransform.anchorMax = Vector2.one; img.rectTransform.sizeDelta = Vector2.zero;
                img.color = c;
                return img;
            }
            RawImage LlenoRaw(string nombre, Transform padre, Texture2D t, Rect uv, Color c)
            {
                var rt = Rect(nombre, padre, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
                var r = rt.gameObject.AddComponent<RawImage>();
                r.texture = t; r.uvRect = uv; r.color = c; r.raycastTarget = false;
                return r;
            }
            CanvasGroup Bloque(string nombre, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tam)
            {
                var rt = Rect(nombre, padre, ancla, pos, tam);
                var g = rt.gameObject.AddComponent<CanvasGroup>();
                g.blocksRaycasts = false;
                return g;
            }

            var fondo = Lleno("Fondo", canvas, blanco, new Color(0.03f, 0f, 0f, 0.95f));
            fondo.raycastTarget = true;   // bloquea clics al juego de fondo
            var rejilla = LlenoRaw("Rejilla", canvas, Tex("Rejilla"), new Rect(0, 0, 30, 17), new Color(1f, 0.2f, 0.15f, 0.07f));

            var contenido = Rect("Contenido", canvas, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            contenido.anchorMin = Vector2.zero; contenido.anchorMax = Vector2.one; contenido.sizeDelta = Vector2.zero;

            // ---- Cabecera ----
            var cab = Bloque("Bloque_Cabecera", contenido, new Vector2(0.5f, 1), Vector2.zero, new Vector2(1920, 90));
            var tCodigo = Texto("Codigo", cab.transform, "NEXUS OS // FALLO CRÍTICO DEL SISTEMA", 18, Rojo, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(60, -30), new Vector2(1100, 28));
            var tEnlace = Texto("Estado_Enlace", cab.transform, "ENLACE: PERDIDO", 18, Rojo, TextAlignmentOptions.Right, new Vector2(1, 1), new Vector2(-60, -30), new Vector2(500, 28));
            var lineaSup = Icono("Linea", cab.transform, blanco, new Vector2(0.5f, 1), new Vector2(0, -66), new Vector2(1800, 2));
            lineaSup.preserveAspect = false; lineaSup.color = new Color(1, 0.2f, 0.15f, 0.4f);
            acentos.AddRange(new Graphic[] { tCodigo, tEnlace, lineaSup });

            // ---- Ojo SENTINEL: anillos y núcleo ----
            var bAnillos = Bloque("Bloque_Anillos", contenido, new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(760, 760));
            var radar = Icono("Anillo_Radar", bAnillos.transform, Spr("Anillo_Radar"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 760));
            radar.color = new Color(1, 0.2f, 0.15f, 0.32f);
            var segmentos = Icono("Anillo_Segmentos", bAnillos.transform, Spr("Anillo_Segmentos"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 620));
            segmentos.color = new Color(1, 0.2f, 0.15f, 0.5f);
            var punteado = Icono("Anillo_Punteado", bAnillos.transform, Spr("Anillo_Punteado"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 520));
            punteado.color = new Color(1, 0.2f, 0.15f, 0.4f);
            var nucleo = Icono("Nucleo", bAnillos.transform, Spr("Nucleo"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 320));
            acentos.AddRange(new Graphic[] { radar, segmentos, punteado });

            // ---- Título glitch ----
            var bTitulo = Bloque("Bloque_Titulo", contenido, new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(1700, 260));
            TextMeshProUGUI Capa(string n)
            {
                var t = Texto(n, bTitulo.transform, "ANOMALÍA PURGADA", 100, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1700, 130));
                t.fontStyle = FontStyles.Bold; t.characterSpacing = 6;
                return t;
            }
            var tC = Capa("Titulo_Cian"); var tR = Capa("Titulo_Rojo"); var tTit = Capa("Titulo");
            var tMotivo = Texto("Motivo", bTitulo.transform, "", 26, gris, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1300, 40));

            // ---- Técnico ----
            var bRetrato = Bloque("Bloque_Retrato", contenido, new Vector2(0, 1), new Vector2(60, -96), new Vector2(700, 180));
            var mRet = Icono("Marco", bRetrato.transform, marco, new Vector2(0, 1), Vector2.zero, new Vector2(170, 170));
            mRet.type = Image.Type.Sliced; mRet.preserveAspect = false; mRet.color = new Color(1, 0.2f, 0.15f, 0.7f);
            var ret = Icono("Retrato", mRet.rectTransform, null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(158, 158));
            var rtEst = Rect("Estatica", mRet.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(158, 158));
            var estRet = rtEst.gameObject.AddComponent<RawImage>();
            estRet.texture = Tex("Estatica"); estRet.color = new Color(1, 1, 1, 0.35f); estRet.raycastTarget = false;
            var tRet = Texto("Etiqueta", bRetrato.transform, "TÉCNICO", 21, Color.white, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(194, -20), new Vector2(500, 90));
            tRet.fontStyle = FontStyles.Bold;
            acentos.Add(mRet);

            // ---- Registro de terminal ----
            var bReg = Bloque("Bloque_Registro", contenido, new Vector2(0, 0), new Vector2(60, 220), new Vector2(780, 230));
            var tReg = Texto("Registro", bReg.transform, "", 19, new Color(1f, 0.78f, 0.74f), TextAlignmentOptions.TopLeft, new Vector2(0, 1), Vector2.zero, new Vector2(780, 230));
            tReg.rectTransform.pivot = new Vector2(0, 1);
            ((RectTransform)bReg.transform).pivot = new Vector2(0, 0);

            // ---- Monitor de pulso ----
            var bPulso = Bloque("Bloque_Pulso", contenido, new Vector2(0.5f, 0), new Vector2(0, 220), new Vector2(560, 150));
            ((RectTransform)bPulso.transform).pivot = new Vector2(0.5f, 0);
            var tPulso = Texto("Etiqueta", bPulso.transform, "SIGNOS VITALES", 16, Rojo, TextAlignmentOptions.Left, new Vector2(0, 1), Vector2.zero, new Vector2(560, 22));
            var fondoPulso = Icono("Fondo", bPulso.transform, blanco, new Vector2(0, 1), new Vector2(0, -28), new Vector2(560, 110));
            fondoPulso.preserveAspect = false; fondoPulso.color = new Color(0.02f, 0.01f, 0.01f, 0.85f);
            var mPulso = Icono("Marco", bPulso.transform, marco, new Vector2(0, 1), new Vector2(0, -28), new Vector2(560, 110));
            mPulso.type = Image.Type.Sliced; mPulso.preserveAspect = false; mPulso.color = new Color(1, 0.2f, 0.15f, 0.5f);
            var pFinal = Icono("Pulso_Final", bPulso.transform, Spr("Pulso_Final"), new Vector2(0, 1), new Vector2(4, -32), new Vector2(552, 102));
            pFinal.preserveAspect = false; pFinal.type = Image.Type.Filled;
            pFinal.fillMethod = Image.FillMethod.Horizontal; pFinal.fillOrigin = 0; pFinal.color = Rojo;
            var rtVivo = Rect("Pulso_Vivo", bPulso.transform, new Vector2(0, 1), new Vector2(4, -32), new Vector2(552, 102));
            var pVivo = rtVivo.gameObject.AddComponent<RawImage>();
            pVivo.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaUI + "/Pulso.png");
            pVivo.uvRect = new Rect(0, 0, 2, 1); pVivo.raycastTarget = false; pVivo.color = Rojo;
            acentos.AddRange(new Graphic[] { tPulso, mPulso, pFinal, pVivo });

            // ---- Informe ----
            var bInf = Bloque("Bloque_Informe", contenido, new Vector2(1, 0), new Vector2(-60, 220), new Vector2(440, 230));
            ((RectTransform)bInf.transform).pivot = new Vector2(1, 0);
            var tInf = Texto("Informe", bInf.transform, "", 19, gris, TextAlignmentOptions.TopLeft, new Vector2(0, 1), Vector2.zero, new Vector2(440, 230));

            // ---- Comandos ----
            var botones = new Button[3];
            var textos = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                var rt = Rect("Comando_" + i, contenido, new Vector2(0.5f, 0), new Vector2((i - 1) * 490, 96), new Vector2(450, 66));
                rt.gameObject.AddComponent<CanvasGroup>();
                var img = rt.gameObject.AddComponent<Image>();
                img.color = Color.white;
                var m = Icono("Marco", rt, marco, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(450, 66));
                m.type = Image.Type.Sliced; m.preserveAspect = false; m.color = new Color(1, 0.2f, 0.15f, 0.8f);
                acentos.Add(m);
                var b = rt.gameObject.AddComponent<Button>();
                var cb = b.colors;
                cb.normalColor = new Color(0.05f, 0.02f, 0.02f, 0.8f);
                cb.highlightedColor = new Color(0.45f, 0.08f, 0.06f, 0.95f);
                cb.selectedColor = cb.highlightedColor;
                cb.pressedColor = new Color(0.7f, 0.12f, 0.08f, 1f);
                cb.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                b.colors = cb;
                textos[i] = Texto("Texto", rt, "[ COMANDO ]", 24, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 60));
                textos[i].fontStyle = FontStyles.Bold;
                botones[i] = b;
            }

            // ---- Efectos por encima de todo ----
            var escaneo = LlenoRaw("Escaneo", canvas, Tex("Escaneo"), new Rect(0, 0, 1, 270), Color.white);
            var estatica = LlenoRaw("Estatica", canvas, Tex("Estatica"), new Rect(0, 0, 4, 2.25f), new Color(1, 1, 1, 0.035f));
            var destello = Lleno("Destello", canvas, blanco, Color.clear);
            destello.raycastTarget = false;

            // ---- Componente ----
            var pf = raiz.AddComponent<PantallaFinal>();
            var so = new SerializedObject(pf);
            void Ref(string c, Object v) => so.FindProperty(c).objectReferenceValue = v;
            void Arr<T>(string c, T[] vs) where T : Object
            {
                var p = so.FindProperty(c); p.arraySize = vs.Length;
                for (int i = 0; i < vs.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = vs[i];
            }
            Ref("canvasHud", raiz.transform.Find("Canvas_HUD").gameObject);
            Ref("contenido", contenido);
            Arr("bloques", new[] { bAnillos, bTitulo, bRetrato, bReg, bPulso, bInf, cab });
            Arr("acentos", acentos.ToArray());
            Ref("fondo", fondo); Ref("escaneo", escaneo); Ref("estatica", estatica); Ref("rejilla", rejilla); Ref("destello", destello);
            Ref("anilloRadar", radar.rectTransform); Ref("anilloSegmentos", segmentos.rectTransform); Ref("anilloPunteado", punteado.rectTransform); Ref("nucleo", nucleo);
            Ref("titulo", tTit); Ref("tituloR", tR); Ref("tituloC", tC); Ref("motivo", tMotivo); Ref("codigo", tCodigo); Ref("estadoEnlace", tEnlace);
            Ref("etiquetaRetrato", tRet); Ref("registro", tReg); Ref("informe", tInf); Ref("etiquetaPulso", tPulso);
            Ref("retrato", ret); Ref("estaticaRetrato", estRet);
            Arr("retratos", new[] { AssetDatabase.LoadAssetAtPath<Sprite>(RutaUI + "/Iconos/Retrato_Neyder.png"), AssetDatabase.LoadAssetAtPath<Sprite>(RutaUI + "/Iconos/Retrato_Deisy.png") });
            Ref("pulsoFinal", pFinal); Ref("pulsoVivo", pVivo);
            Arr("botones", botones); Arr("textosBotones", textos);
            so.ApplyModifiedPropertiesWithoutUndo();
            return canvas;
        }

        // ---------------- Recursos gráficos procedurales ----------------
        static void GenerarRecursosFinal()
        {
            Directory.CreateDirectory(RutaFinal);
            float Ang(float x, float y) => Mathf.Repeat(Mathf.Atan2(y, x) * Mathf.Rad2Deg, 360f);

            Guardar("Anillo_Radar", 512, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y), a = Ang(x, y);
                if (r > 0.93f && r < 0.955f) return Color.white;
                float marca = Mathf.Repeat(a, 6f);
                bool larga = Mathf.Repeat(a, 30f) < 1.2f;
                if (marca < 0.8f && r > (larga ? 0.80f : 0.88f) && r < 0.93f) return Color.white;
                if (r > 0.98f && r < 0.99f && Mathf.Repeat(a, 2f) < 1f) return new Color(1, 1, 1, 0.6f);
                return Color.clear;
            }, true);
            Guardar("Anillo_Segmentos", 512, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y), a = Ang(x, y);
                bool arco = (a > 10f && a < 80f) || (a > 130f && a < 200f) || (a > 250f && a < 320f);
                if (arco && r > 0.70f && r < 0.80f) return Color.white;
                if (r > 0.655f && r < 0.665f) return new Color(1, 1, 1, 0.7f);
                return Color.clear;
            }, true);
            Guardar("Anillo_Punteado", 512, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y), a = Ang(x, y);
                if (r > 0.86f && r < 0.92f && Mathf.Repeat(a, 15f) < 8f) return Color.white;
                if (r > 0.6f && r < 0.61f) return new Color(1, 1, 1, 0.5f);
                return Color.clear;
            }, true);
            Guardar("Nucleo", 256, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                if (r > 1f) return Color.clear;
                float a = Mathf.Pow(1f - r, 2.4f);
                if (r < 0.16f) a = 1f;
                if (r > 0.3f && r < 0.33f) a = Mathf.Max(a, 0.8f);
                return new Color(1, 1, 1, a);
            }, true);
            Guardar("Escaneo", 4, (x, y) => y > 0f ? new Color(0, 0, 0, 0.35f) : Color.clear, false, FilterMode.Point);
            var rnd = new System.Random(99);
            Guardar("Estatica", 256, (x, y) => { float v = (float)rnd.NextDouble(); return new Color(v, v, v, (float)rnd.NextDouble()); }, false, FilterMode.Point);
            Guardar("Rejilla", 64, (x, y) => (x < -0.96f || y < -0.96f) ? Color.white : Color.clear, false);

            // Último latido que se apaga y queda plano
            const int w = 1024, h = 128;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = Color.clear;
            float Altura(float u)
            {
                float v = 0f;
                float[] centros = { 0.12f, 0.33f, 0.52f };
                float[] fuerza = { 1f, 0.55f, 0.22f };
                for (int k = 0; k < 3; k++)
                {
                    float d = u - centros[k];
                    v += fuerza[k] * (0.4f * Mathf.Exp(-Mathf.Pow(d / 0.008f, 2)) - 0.14f * Mathf.Exp(-Mathf.Pow((d + 0.012f) / 0.006f, 2))
                                    - 0.12f * Mathf.Exp(-Mathf.Pow((d - 0.015f) / 0.006f, 2)) + 0.08f * Mathf.Exp(-Mathf.Pow((d - 0.06f) / 0.02f, 2)));
                }
                return 0.45f + v;
            }
            int prev = (int)(Altura(0) * h);
            for (int x = 0; x < w; x++)
            {
                int y = Mathf.Clamp((int)(Altura(x / (float)w) * h), 2, h - 3);
                for (int yy = Mathf.Min(prev, y) - 1; yy <= Mathf.Max(prev, y) + 1; yy++)
                    for (int dx = 0; dx < 2; dx++)
                        if (yy >= 0 && yy < h && x + dx < w) pix[yy * w + x + dx] = Color.white;
                prev = y;
            }
            t.SetPixels(pix); t.Apply();
            string ruta = $"{RutaFinal}/Pulso_Final.png";
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(ruta);
            var ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
            ti.textureType = TextureImporterType.Sprite; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            var ajustes = new TextureImporterSettings();
            ti.ReadTextureSettings(ajustes); ajustes.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(ajustes);
            ti.SaveAndReimport();
        }

        static void Guardar(string nombre, int tam, System.Func<float, float, Color> f, bool sprite, FilterMode filtro = FilterMode.Bilinear)
        {
            var t = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
            for (int y = 0; y < tam; y++)
                for (int x = 0; x < tam; x++)
                    t.SetPixel(x, y, f((x + 0.5f) / tam * 2f - 1f, (y + 0.5f) / tam * 2f - 1f));
            t.Apply();
            string ruta = $"{RutaFinal}/{nombre}.png";
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(ruta);
            var ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
            ti.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            ti.wrapMode = sprite ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            ti.filterMode = filtro;
            ti.SaveAndReimport();
        }
    }
}
