using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nexus.Herramientas
{
    /// <summary>
    /// Música y efectos de Nexus generados por SÍNTESIS DE SONIDO (como las texturas y los shaders,
    /// todo se crea por código: no hay audios de terceros ni licencias que pedir).
    ///
    /// Cada sonido se calcula muestra por muestra con las piezas clásicas de un sintetizador:
    ///  - osciladores (seno, diente de sierra, cuadrada, triángulo) y ruido blanco o "marrón";
    ///  - filtros (paso bajo, paso alto y pasa banda tipo biquad);
    ///  - envolventes (ataque y caída exponencial) y barridos de frecuencia;
    ///  - parciales inarmónicos para los sonidos metálicos (campanas, golpes en metal).
    /// Las pistas en bucle terminan con un fundido cruzado sobre su inicio para que no se note el corte.
    /// Se guardan como WAV de 16 bits en Assets/Nexus/Audio y el banco va a Resources.
    /// </summary>
    public static class ConstructorAudio
    {
        const string Carpeta = "Assets/Nexus/Audio";
        const int SrMusica = 32000, SrEfecto = 44100;
        const double DosPi = Math.PI * 2.0;

        // ======================= Piezas de síntesis =======================
        static float Seno(double f, double t) => (float)Math.Sin(DosPi * f * t);
        static float Sierra(double fase) => (float)(2.0 * (fase - Math.Floor(fase)) - 1.0);
        static float Cuadrada(double fase) => (fase - Math.Floor(fase)) < 0.5 ? 1f : -1f;
        static float Triangulo(double fase) { double x = fase - Math.Floor(fase); return (float)(x < 0.5 ? 4 * x - 1 : 3 - 4 * x); }
        static float Caida(double t, double tau) => t < 0 ? 0f : (float)Math.Exp(-t / tau);
        static float Ataque(double t, double a) => t < 0 ? 0f : t < a ? (float)(t / a) : 1f;

        /// <summary>Filtro de un polo (paso bajo); el paso alto es la entrada menos el paso bajo.</summary>
        class Polo
        {
            float y; readonly float a;
            public Polo(float fc, int sr) { a = 1f - (float)Math.Exp(-DosPi * fc / sr); }
            public float Bajo(float x) { y += a * (x - y); return y; }
            public float Alto(float x) => x - Bajo(x);
        }

        /// <summary>Biquad pasa banda (fórmulas de R. Bristow-Johnson), con frecuencia variable.</summary>
        class PasaBanda
        {
            double b0, b1, b2, a1, a2, x1, x2, y1, y2; readonly int sr;
            public PasaBanda(int sr) { this.sr = sr; }
            public void Ajustar(double fc, double q)
            {
                double w = DosPi * fc / sr, al = Math.Sin(w) / (2 * q), a0 = 1 + al;
                b0 = al / a0; b1 = 0; b2 = -al / a0; a1 = -2 * Math.Cos(w) / a0; a2 = (1 - al) / a0;
            }
            public float Proc(float x)
            {
                double y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y; return (float)y;
            }
        }

        static float[] Buf(double seg, int sr) => new float[(int)(seg * sr)];

        static void Normalizar(float[] b, float pico)
        {
            float m = 1e-6f; foreach (var v in b) m = Math.Max(m, Math.Abs(v));
            for (int i = 0; i < b.Length; i++) b[i] *= pico / m;
        }

        /// <summary>Bucle sin corte: se generó "extra" segundos de más y esa cola se funde sobre el inicio.</summary>
        static float[] Bucle(float[] largo, int sr, double duracion, double cruce)
        {
            int n = (int)(duracion * sr), x = (int)(cruce * sr);
            var r = new float[n];
            Array.Copy(largo, r, n);
            for (int i = 0; i < x && n + i < largo.Length; i++)
            {
                float k = i / (float)x;
                r[i] = r[i] * k + largo[n + i] * (1f - k);
            }
            return r;
        }

        /// <summary>Eco simple (dos repeticiones): da sensación de sala grande.</summary>
        static void Eco(float[] b, int sr, double retardo, float fb)
        {
            int d = (int)(retardo * sr);
            for (int i = d; i < b.Length; i++) b[i] += b[i - d] * fb;
        }

        /// <summary>Campana o golpe metálico: parciales inarmónicos que caen a distinto ritmo.</summary>
        static void Metal(float[] b, int sr, double t0, double f0, float amp, double tau)
        {
            double[] r = { 1, 2.76, 5.4, 8.93 }; float[] a = { 1f, 0.5f, 0.25f, 0.12f };
            for (int i = (int)(t0 * sr); i < b.Length; i++)
            {
                double t = i / (double)sr - t0;
                if (t > tau * 6) break;
                float s = 0;
                for (int k = 0; k < 4; k++) s += a[k] * Seno(f0 * r[k], t) * Caida(t, tau / (1 + k * 0.6));
                b[i] += s * amp;
            }
        }

        /// <summary>Golpe grave: seno que baja de f1 a f2 (bombos, pisadas, explosiones).</summary>
        static void Golpe(float[] b, int sr, double t0, double f1, double f2, double tau, float amp)
        {
            double fase = 0;
            for (int i = (int)(t0 * sr); i < b.Length; i++)
            {
                double t = i / (double)sr - t0;
                if (t > tau * 7) break;
                double f = f2 + (f1 - f2) * Math.Exp(-t / (tau * 0.5));
                fase += f / sr;
                b[i] += (float)Math.Sin(DosPi * fase) * Caida(t, tau) * amp;
            }
        }

        /// <summary>Ráfaga de ruido filtrado con caída exponencial (disparos, chasquidos, vapor).</summary>
        static void Ruido(float[] b, int sr, System.Random rnd, double t0, double tau, float amp, float pasoBajo, float pasoAlto = 0f, double ataque = 0.0005)
        {
            var lp = new Polo(pasoBajo, sr); var hp = pasoAlto > 0 ? new Polo(pasoAlto, sr) : null;
            for (int i = (int)(t0 * sr); i < b.Length; i++)
            {
                double t = i / (double)sr - t0;
                if (t > tau * 8 + ataque) break;
                float x = (float)(rnd.NextDouble() * 2 - 1);
                x = lp.Bajo(x); if (hp != null) x = hp.Alto(x);
                b[i] += x * Caida(t - ataque, tau) * Ataque(t, ataque) * amp;
            }
        }

        static void Clic(float[] b, int sr, System.Random rnd, double t0, float amp, double fc)
        {
            var bp = new PasaBanda(sr); bp.Ajustar(fc, 4);
            for (int i = (int)(t0 * sr); i < b.Length && i < (int)((t0 + 0.06) * sr); i++)
            {
                double t = i / (double)sr - t0;
                b[i] += bp.Proc((float)(rnd.NextDouble() * 2 - 1)) * Caida(t, 0.006) * amp * 3f;
            }
            Metal(b, sr, t0, fc * 0.7, amp * 0.25f, 0.03);
        }

        // ======================= Música =======================
        static float[] MusicaExploracion()
        {
            int sr = SrMusica; double L = 64, X = 2; var b = Buf(L + X, sr); var rnd = new System.Random(3);
            double faseA = 0, faseB = 0; float marron = 0;
            var lpRumor = new Polo(90, sr);
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr;
                // Bordón grave (La 55 Hz) y su quinta, que entra y sale
                b[i] += Seno(55, t) * 0.18f;
                b[i] += Seno(82.41, t) * 0.05f * (0.5f + 0.5f * Seno(1.0 / 16, t));
                // Pad inquietante: segunda menor (La y Si bemol) que "late"
                float pad = Seno(220, t) * 0.03f + Seno(233.08, t) * 0.025f;
                b[i] += pad * (0.4f + 0.6f * (0.5f + 0.5f * Seno(1.0 / 16, t + 4)));
                marron = marron * 0.995f + (float)(rnd.NextDouble() * 2 - 1) * 0.02f;
                b[i] += lpRumor.Bajo(marron) * 1.3f;   // rumor grave del edificio
            }
            // Dos sierras desafinadas con un filtro que "respira" lentamente (corte variable por muestra)
            float y = 0;
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr;
                faseA += 55.3 / sr; faseB += 54.7 / sr;
                float corte = 220f + 180f * (0.5f + 0.5f * Seno(1.0 / 32, t));
                float a = 1f - (float)Math.Exp(-DosPi * corte / sr);
                y += a * ((Sierra(faseA) + Sierra(faseB)) * 0.5f - y);
                b[i] += y * 0.16f;
            }
            // La estructura "gime": metal bajo tensión que cruje a lo lejos, y golpes sordos en algún
            // lugar del edificio (en vez de campanas: el lugar está vivo y no es amable)
            var lejos = new float[b.Length];
            for (int k = 0; k < 7; k++)
            {
                double t0 = 2 + 9 * k + rnd.NextDouble() * 3, f1 = 140 + rnd.NextDouble() * 120;
                GemidoMetal(lejos, sr, rnd, t0, 2.5 + rnd.NextDouble() * 2, f1, f1 * (0.75 + rnd.NextDouble() * 0.15), 0.05f);
            }
            for (int k = 0; k < 3; k++) Golpe(lejos, sr, 8 + 20 * k + rnd.NextDouble() * 4, 55, 30, 0.6, 0.12f);
            Eco(lejos, sr, 0.41, 0.4f);
            var lpLejos = new Polo(1800, sr);
            for (int i = 0; i < b.Length; i++) b[i] += lpLejos.Bajo(lejos[i]);
            var r = Bucle(b, sr, L, X); Normalizar(r, 0.8f); return r;
        }

        static float[] MusicaMenu()
        {
            int sr = SrMusica; double L = 48, X = 2; var b = Buf(L + X, sr);
            var lp = new Polo(900, sr);
            double[] notas = { 440, 523.25, 659.25, 587.33, 523.25, 493.88, 392, 440, 440, 523.25, 659.25, 783.99, 698.46, 659.25, 587.33, 523.25 };
            var piano = new float[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr;
                b[i] += (Seno(55, t) * 0.12f + Seno(82.41, t) * 0.06f);
                float sw = 0.5f + 0.5f * Seno(1.0 / 12, t);
                float acorde = Triangulo(220 * t) + Triangulo(261.63 * t) * 0.8f + Triangulo(329.63 * t) * 0.7f;
                b[i] += lp.Bajo(acorde) * 0.05f * (0.3f + 0.7f * sw);
            }
            for (int k = 0; k < 30; k++)
            {
                double t0 = 0.5 + k * 1.5; if (t0 > L + X - 2) break;
                double f = notas[k % notas.Length];
                for (int i = (int)(t0 * sr); i < piano.Length && i < (int)((t0 + 3) * sr); i++)
                {
                    double t = i / (double)sr - t0;
                    piano[i] += (Seno(f, t) + Seno(f * 2, t) * 0.3f + Seno(f * 3, t) * 0.1f) * Caida(t, 0.9) * Ataque(t, 0.005) * 0.07f;
                }
            }
            Eco(piano, sr, 0.45, 0.3f);
            for (int i = 0; i < b.Length; i++) b[i] += piano[i];
            var r = Bucle(b, sr, L, X); Normalizar(r, 0.8f); return r;
        }

        static float[] MusicaTension()
        {
            int sr = SrMusica; double L = 30, X = 1; var b = Buf(L + X, sr); var rnd = new System.Random(5);
            const double beat = 0.6;   // 100 pulsaciones por minuto: un corazón acelerado
            for (int k = 0; k * beat < L + X; k++)
            {
                Golpe(b, sr, k * beat, 72, 44, 0.12, 0.55f);
                Golpe(b, sr, k * beat + 0.18, 66, 42, 0.1, 0.35f);
                for (int s = 0; s < 4; s++) Ruido(b, sr, rnd, k * beat + s * beat / 4, 0.01, 0.05f, 9000, 4000);
            }
            var bp = new PasaBanda(sr); bp.Ajustar(900, 0.7);
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr;
                float cuerdas = Sierra(440 * t) + Sierra(466.16 * t) + Sierra(523.25 * t) * 0.8f;
                float trem = 0.6f + 0.4f * Seno(8, t);
                b[i] += bp.Proc(cuerdas) * 0.16f * trem * (0.5f + 0.5f * Seno(1.0 / 15, t));
            }
            var r = Bucle(b, sr, L, X); Normalizar(r, 0.8f); return r;
        }

        static float[] MusicaJefe()
        {
            int sr = SrMusica; const double beat = 0.48; double L = 64 * beat, X = 0.25; var b = Buf(L + X, sr); var rnd = new System.Random(9);
            double[] bajo = { 55, 55, 65.41, 55, 49, 55, 73.42, 55 };
            for (int k = 0; k * beat < L + X; k++)
            {
                double t0 = k * beat;
                Golpe(b, sr, t0, 120, 42, 0.14, 0.7f);
                Ruido(b, sr, rnd, t0, 0.003, 0.4f, 6000);
                if (k % 2 == 1) { Ruido(b, sr, rnd, t0, 0.1, 0.35f, 4000, 1200); Golpe(b, sr, t0, 200, 180, 0.06, 0.2f); }
                Ruido(b, sr, rnd, t0 + beat / 2, 0.025, 0.08f, 12000, 6000);
                // Bajo en corcheas
                for (int c = 0; c < 2; c++)
                {
                    double f = bajo[(k * 2 + c) % bajo.Length], tn = t0 + c * beat / 2;
                    var lp = new Polo(420, sr); double fase = 0;
                    for (int i = (int)(tn * sr); i < b.Length && i < (int)((tn + beat / 2) * sr); i++)
                    {
                        double t = i / (double)sr - tn; fase += f / sr;
                        b[i] += lp.Bajo(Sierra(fase) + Sierra(fase * 1.01)) * Caida(t, 0.16) * 0.28f;
                    }
                }
                // Golpe disonante cada dos compases
                if (k % 8 == 0)
                    for (int i = (int)(t0 * sr); i < b.Length && i < (int)((t0 + 1.6) * sr); i++)
                    {
                        double t = i / (double)sr - t0;
                        b[i] += (Sierra(110 * t) + Sierra(130.81 * t) + Sierra(155.56 * t)) * Caida(t, 0.45) * 0.07f;
                    }
                // Arpegio agudo en la segunda mitad
                if (k >= 32)
                    for (int s = 0; s < 4; s++)
                    {
                        double f = new[] { 220, 261.63, 329.63, 311.13 }[s], tn = t0 + s * beat / 4;
                        for (int i = (int)(tn * sr); i < b.Length && i < (int)((tn + beat / 4) * sr); i++)
                        {
                            double t = i / (double)sr - tn;
                            b[i] += Cuadrada(f * 2 * t) * Caida(t, 0.05) * 0.035f;
                        }
                    }
            }
            var r = Bucle(b, sr, L, X); Normalizar(r, 0.85f); return r;
        }

        static float[] Ventiladores()
        {
            int sr = SrMusica; double L = 12, X = 1; var b = Buf(L + X, sr); var rnd = new System.Random(11);
            var lp = new Polo(380, sr); float marron = 0;
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr;
                marron = marron * 0.98f + (float)(rnd.NextDouble() * 2 - 1) * 0.1f;
                float aspas = 0.85f + 0.15f * Seno(23, t);
                b[i] = lp.Bajo(marron) * aspas + Seno(60, t) * 0.05f + Seno(120, t) * 0.025f + Seno(180, t) * 0.01f;
            }
            var r = Bucle(b, sr, L, X); Normalizar(r, 0.7f); return r;
        }

        static float[] Sirena()
        {
            // Se generan 3 s y se usan los 2 últimos: el filtro ya está "caliente" y la fase es entera
            // cada segundo, así el final empalma exacto con el inicio
            int sr = SrMusica; var b = Buf(3.0, sr); var lp = new Polo(2500, sr); double fase = 0;
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr, x = t - Math.Floor(t);
                double f = 450 + 650 * x;              // sube de 450 a 1100 Hz cada segundo
                fase += f / sr;
                b[i] = lp.Bajo(Cuadrada(fase)) * 0.5f + (float)Math.Sin(DosPi * fase) * 0.3f;
            }
            var r = new float[2 * sr];
            Array.Copy(b, sr, r, 0, r.Length);
            Normalizar(r, 0.7f); return r;
        }

        // ======================= Efectos =======================
        static float[] DisparoPistola()
        {
            int sr = SrEfecto; var b = Buf(0.7, sr); var rnd = new System.Random(21);
            Ruido(b, sr, rnd, 0, 0.0025, 1.2f, 12000);
            Ruido(b, sr, rnd, 0, 0.025, 0.9f, 5000);
            Golpe(b, sr, 0, 160, 55, 0.06, 0.8f);
            Ruido(b, sr, rnd, 0.01, 0.18, 0.25f, 800);
            Normalizar(b, 0.95f); return b;
        }

        static float[] DisparoEscopeta()
        {
            int sr = SrEfecto; var b = Buf(1.2, sr); var rnd = new System.Random(22);
            Ruido(b, sr, rnd, 0, 0.004, 1.2f, 10000);
            Ruido(b, sr, rnd, 0, 0.05, 1f, 3000);
            Golpe(b, sr, 0, 110, 38, 0.12, 1f);
            Ruido(b, sr, rnd, 0.02, 0.4, 0.4f, 600);
            Normalizar(b, 0.97f); return b;
        }

        static float[] Bombeo()
        {
            int sr = SrEfecto; var b = Buf(0.5, sr); var rnd = new System.Random(23);
            Clic(b, sr, rnd, 0.0, 0.8f, 2500);
            Ruido(b, sr, rnd, 0.02, 0.06, 0.15f, 1500, 300, 0.05);
            Clic(b, sr, rnd, 0.2, 1f, 2100);
            Normalizar(b, 0.85f); return b;
        }

        static float[] Recarga()
        {
            int sr = SrEfecto; var b = Buf(1.0, sr); var rnd = new System.Random(24);
            Clic(b, sr, rnd, 0.0, 0.6f, 2800);
            Ruido(b, sr, rnd, 0.05, 0.08, 0.1f, 2000, 400, 0.04);
            Clic(b, sr, rnd, 0.45, 1f, 2000);
            Golpe(b, sr, 0.45, 180, 120, 0.03, 0.3f);
            Clic(b, sr, rnd, 0.75, 0.8f, 3200);
            Normalizar(b, 0.85f); return b;
        }

        static float[] CartuchoCarga()
        {
            int sr = SrEfecto; var b = Buf(0.3, sr); var rnd = new System.Random(25);
            Ruido(b, sr, rnd, 0, 0.03, 0.2f, 1800, 300, 0.03);
            Clic(b, sr, rnd, 0.05, 0.9f, 1700);
            Normalizar(b, 0.8f); return b;
        }

        static float[] SinMunicion()
        {
            int sr = SrEfecto; var b = Buf(0.15, sr); var rnd = new System.Random(26);
            Clic(b, sr, rnd, 0, 1f, 3000);
            Normalizar(b, 0.7f); return b;
        }

        static float[] Cuchillo()
        {
            int sr = SrEfecto; var b = Buf(0.4, sr); var rnd = new System.Random(27); var bp = new PasaBanda(sr);
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr;
                bp.Ajustar(900 + 2600 * Math.Min(1, t / 0.25), 1.2);
                float env = (float)(t < 0.06 ? t / 0.06 : Math.Exp(-(t - 0.06) / 0.08));
                b[i] = bp.Proc((float)(rnd.NextDouble() * 2 - 1)) * env;
            }
            Normalizar(b, 0.75f); return b;
        }

        // ---------- Piezas del terror (survival horror clásico) ----------
        static float Dist(float x, float k) => (float)Math.Tanh(k * x);

        /// <summary>
        /// Gruñido mecánico: sierras graves con un vibrato errático, por dos filtros de "formantes"
        /// (como una garganta) y saturadas. Suena a máquina que respira, casi una voz.
        /// </summary>
        static void Grunido(float[] b, int sr, System.Random rnd, double t0, double dur, double f1, double f2, float amp, double ataque, double tau)
        {
            var bp1 = new PasaBanda(sr); bp1.Ajustar(420, 3); var bp2 = new PasaBanda(sr); bp2.Ajustar(980, 4);
            double fase = 0, desvio = 0;
            for (int i = (int)(t0 * sr); i < b.Length && i < (int)((t0 + dur) * sr); i++)
            {
                double t = i / (double)sr - t0, k = Math.Min(1, t / dur);
                desvio = desvio * 0.999 + (rnd.NextDouble() - 0.5) * 0.9;     // vibrato errático
                double f = f1 * Math.Pow(f2 / f1, k) + desvio;
                fase += f / sr;
                float s = Sierra(fase) + Sierra(fase * 1.012) * 0.7f + (float)(rnd.NextDouble() - 0.5) * 0.3f;
                float g = bp1.Proc(s) * 1.4f + bp2.Proc(s);
                float env = (float)(t < ataque ? t / ataque : Math.Exp(-(t - ataque) / tau));
                float salida = (float)Math.Min(1, (dur - t) / 0.03);   // sin cortes bruscos al final
                b[i] += Dist(g * 2.2f, 1.8f) * env * amp * salida;
            }
        }

        /// <summary>Chirrido de cuerdas: ruido por una resonancia estrecha que barre hacia el agudo.</summary>
        static void Chirrido(float[] b, int sr, System.Random rnd, double t0, double dur, double f1, double f2, float amp)
        {
            var bp = new PasaBanda(sr);
            for (int i = (int)(t0 * sr); i < b.Length && i < (int)((t0 + dur) * sr); i++)
            {
                double t = i / (double)sr - t0, k = t / dur;
                bp.Ajustar(f1 * Math.Pow(f2 / f1, k), 14);
                float env = (float)(t < 0.02 ? t / 0.02 : Math.Exp(-(t - 0.02) / (dur * 0.45)));
                b[i] += bp.Proc((float)(rnd.NextDouble() * 2 - 1)) * env * amp * 4f * (float)Math.Min(1, (dur - t) / 0.03);
            }
        }

        /// <summary>Golpe orquestal disonante: cuerdas y metales graves en clúster (La, Si bemol, Mi, Fa).</summary>
        static void Cluster(float[] b, int sr, double t0, float amp, double tau)
        {
            double[] f = { 55, 58.27, 82.41, 87.31, 110, 116.54 };
            var lp = new Polo(900, sr);
            for (int i = (int)(t0 * sr); i < b.Length; i++)
            {
                double t = i / (double)sr - t0;
                if (t > tau * 7) break;
                float s = 0;
                foreach (var x in f) s += Sierra(x * t) + Sierra(x * 1.006 * t);
                b[i] += Dist(lp.Bajo(s) * 0.4f, 1.5f) * Caida(t, tau) * Ataque(t, 0.008) * amp;
            }
        }

        /// <summary>Metal bajo tensión: una resonancia muy estrecha que se desliza (gemidos de la estructura).</summary>
        static void GemidoMetal(float[] b, int sr, System.Random rnd, double t0, double dur, double f1, double f2, float amp)
        {
            var bp1 = new PasaBanda(sr); var bp2 = new PasaBanda(sr);
            for (int i = (int)(t0 * sr); i < b.Length && i < (int)((t0 + dur) * sr); i++)
            {
                double t = i / (double)sr - t0, k = t / dur;
                double f = f1 + (f2 - f1) * k;
                bp1.Ajustar(f, 40); bp2.Ajustar(f * 2.13, 35);
                float x = (float)(rnd.NextDouble() * 2 - 1);
                float env = (float)Math.Sin(Math.PI * k) * (float)(0.7 + 0.3 * Math.Sin(DosPi * 3.1 * t));
                b[i] += (bp1.Proc(x) * 6f + bp2.Proc(x) * 3f) * env * amp;
            }
        }

        /// <summary>Impacto de bala en el blindaje: golpe seco, chispa corta y metal grave, sin campanilla.</summary>
        static float[] ImpactoMetal()
        {
            int sr = SrEfecto; var b = Buf(0.45, sr); var rnd = new System.Random(28);
            Ruido(b, sr, rnd, 0, 0.006, 0.9f, 4000);
            Golpe(b, sr, 0, 130, 60, 0.04, 0.6f);
            Metal(b, sr, 0, 380, 0.3f, 0.09);
            for (int k = 0; k < 6; k++) Ruido(b, sr, rnd, 0.01 + rnd.NextDouble() * 0.12, 0.003, 0.25f, 9000, 3500);
            Normalizar(b, 0.85f); return b;
        }

        /// <summary>
        /// Te detectó: "golpe de terror" al estilo de los survival horror. Clúster orquestal grave,
        /// bombo sordo, chirrido de cuerdas y el gruñido mecánico de la unidad.
        /// </summary>
        static float[] AndroideDetecta()
        {
            int sr = SrEfecto; var b = Buf(1.8, sr); var rnd = new System.Random(40);
            Cluster(b, sr, 0, 0.55f, 0.45);
            Golpe(b, sr, 0, 62, 30, 0.35, 0.9f);
            Chirrido(b, sr, rnd, 0.0, 0.7, 1700, 3300, 0.35f);
            Grunido(b, sr, rnd, 0.05, 1.3, 70, 52, 0.55f, 0.06, 0.45);
            Normalizar(b, 0.95f); return b;
        }

        /// <summary>Pisada pesada de la unidad: golpe grave, chasquido de metal y siseo hidráulico.</summary>
        static float[] AndroidePaso()
        {
            int sr = SrEfecto; var b = Buf(0.45, sr); var rnd = new System.Random(29);
            Golpe(b, sr, 0, 58, 34, 0.08, 1f);
            Metal(b, sr, 0, 190, 0.35f, 0.12);
            Ruido(b, sr, rnd, 0.03, 0.05, 0.12f, 9000, 3000, 0.01);
            Normalizar(b, 0.8f); return b;
        }

        static float[] Barrido(double seg, double f1, double f2, double hasta, float amp, int sr, bool sierra, double tauFinal)
        {
            var b = Buf(seg, sr); var lp = new Polo(3000, sr); double fase = 0;
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr, k = Math.Min(1, t / hasta);
                double f = f1 * Math.Pow(f2 / f1, k);
                fase += f / sr;
                float env = t < hasta ? (float)(0.3 + 0.7 * k) : Caida(t - hasta, tauFinal);
                b[i] = lp.Bajo(sierra ? Sierra(fase) : (float)Math.Sin(DosPi * fase)) * env * amp;
            }
            return b;
        }

        /// <summary>Motor grave y saturado que sube o baja (arranque y apagado de una unidad).</summary>
        static void Motor(float[] b, int sr, System.Random rnd, double t0, double dur, double f1, double f2, float amp, bool sube)
        {
            var lp = new Polo(550, sr); double fase = 0;
            for (int i = (int)(t0 * sr); i < b.Length && i < (int)((t0 + dur) * sr); i++)
            {
                double t = i / (double)sr - t0, k = t / dur;
                double f = f1 * Math.Pow(f2 / f1, k) * (1 + (rnd.NextDouble() - 0.5) * 0.02);
                fase += f / sr;
                float env = sube ? (float)(0.25 + 0.75 * k) : (float)(1 - k * 0.8);
                // Tartamudeo: el motor "engancha" y se suelta
                float gate = 0.65f + 0.35f * Cuadrada(f * 0.11 * t);
                b[i] += Dist(lp.Bajo(Sierra(fase) + Sierra(fase * 1.5) * 0.4f) * 1.6f, 2f) * env * gate * amp * (float)Math.Min(1, (dur - t) / 0.03);
            }
        }

        /// <summary>Una unidad se enciende: escape hidráulico, servos que rechinan, motor que arranca y un golpe seco.</summary>
        static float[] AndroideDespierta()
        {
            int sr = SrEfecto; var b = Buf(1.9, sr); var rnd = new System.Random(41); var bp = new PasaBanda(sr); bp.Ajustar(700, 2);
            Ruido(b, sr, rnd, 0, 0.25, 0.55f, 11000, 2500, 0.01);                 // escape hidráulico
            double fase = 0;
            for (int i = (int)(0.15 * sr); i < (int)(1.2 * sr); i++)               // servos rechinando, a tirones
            {
                double t = i / (double)sr;
                fase += (110 + 25 * Math.Sin(DosPi * 1.7 * t) + (rnd.NextDouble() - 0.5) * 30) / sr;
                float tiron = ((int)(t * 9) % 3 == 0) ? 0.25f : 1f;
                b[i] += bp.Proc(Sierra(fase)) * tiron * 0.5f;
            }
            Motor(b, sr, rnd, 0.1, 1.15, 42, 96, 0.55f, true);
            Golpe(b, sr, 1.25, 70, 38, 0.12, 1f);
            Metal(b, sr, 1.25, 260, 0.3f, 0.2);
            Grunido(b, sr, rnd, 1.28, 0.6, 58, 50, 0.35f, 0.04, 0.25);
            Normalizar(b, 0.85f); return b;
        }

        /// <summary>Una unidad cae: chispazos, el motor se apaga arrastrándose, choque de chatarra y golpe al suelo.</summary>
        static float[] AndroideDestruido()
        {
            int sr = SrEfecto; var b = Buf(2.2, sr); var rnd = new System.Random(30);
            for (int k = 0; k < 55; k++) { double t0 = rnd.NextDouble() * 1.4; Ruido(b, sr, rnd, t0, 0.004, 0.4f * (float)(1 - t0 / 1.6), 9000, 2500); }
            Motor(b, sr, rnd, 0, 1.4, 120, 28, 0.6f, false);
            foreach (var (t0, f) in new[] { (0.9, 230.0), (0.97, 310.0), (1.05, 410.0) }) Metal(b, sr, t0, f, 0.35f, 0.25);
            Ruido(b, sr, rnd, 0.9, 0.2, 0.5f, 2500);
            Golpe(b, sr, 1.1, 65, 32, 0.25, 1f);
            Normalizar(b, 0.9f); return b;
        }

        static float[] JugadorPaso()
        {
            int sr = SrEfecto; var b = Buf(0.25, sr); var rnd = new System.Random(31);
            Ruido(b, sr, rnd, 0, 0.03, 0.7f, 700);
            Golpe(b, sr, 0, 95, 60, 0.04, 0.4f);
            Ruido(b, sr, rnd, 0.01, 0.01, 0.15f, 5000, 1500);
            Normalizar(b, 0.6f); return b;
        }

        /// <summary>El androide golpea al técnico: impacto seco en el cuerpo, crujido y una descarga áspera.</summary>
        static float[] JugadorHerido()
        {
            int sr = SrEfecto; var b = Buf(0.8, sr); var rnd = new System.Random(32);
            Golpe(b, sr, 0, 78, 32, 0.1, 1f);                     // el golpe en el cuerpo
            Ruido(b, sr, rnd, 0, 0.06, 0.8f, 500);                  // carne y ropa: ruido sordo
            Ruido(b, sr, rnd, 0.004, 0.015, 0.6f, 3500, 900);       // crujido
            for (int k = 0; k < 8; k++) Ruido(b, sr, rnd, 0.01 + rnd.NextDouble() * 0.05, 0.004, 0.35f, 6000, 1500);
            var zap = new float[b.Length];                          // descarga eléctrica: ruido saturado, no un tono
            Ruido(zap, sr, rnd, 0.02, 0.12, 0.6f, 7000, 2000, 0.005);
            for (int i = 0; i < b.Length; i++) b[i] += Dist(zap[i] * 3f, 2f) * 0.25f;
            Normalizar(b, 0.9f); return b;
        }

        static float[] Puerta()
        {
            int sr = SrEfecto; var b = Buf(1.1, sr); var rnd = new System.Random(33); var lp = new Polo(600, sr); var bp = new PasaBanda(sr);
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr;
                float env = t < 0.05 ? (float)(t / 0.05) : t < 0.8 ? 1f : Caida(t - 0.8, 0.05);
                b[i] += lp.Bajo(Sierra(90 * t) + Sierra(180 * t) * 0.5f) * env * 0.25f;
                bp.Ajustar(400 + 500 * Math.Min(1, t / 0.8), 2);
                b[i] += bp.Proc((float)(rnd.NextDouble() * 2 - 1)) * env * 0.3f;
            }
            Golpe(b, sr, 0.82, 80, 50, 0.08, 0.6f);
            Metal(b, sr, 0.82, 600, 0.2f, 0.12);
            Normalizar(b, 0.75f); return b;
        }

        /// <summary>Recoger algo: roce de tela o papel y un golpe grave y apagado (nada de campanitas).</summary>
        static float[] Recoger()
        {
            int sr = SrEfecto; var b = Buf(0.7, sr); var rnd = new System.Random(42);
            Ruido(b, sr, rnd, 0, 0.05, 0.3f, 6000, 1500, 0.02);
            Ruido(b, sr, rnd, 0.09, 0.04, 0.25f, 5000, 1200, 0.015);
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr;
                b[i] += (Seno(98, t) + Seno(196, t) * 0.4f) * Caida(t - 0.02, 0.22) * Ataque(t - 0.02, 0.004) * 0.45f;
                b[i] += (Seno(146.83, t) + Seno(293.66, t) * 0.3f) * Caida(t - 0.12, 0.3) * Ataque(t - 0.12, 0.004) * 0.35f;
            }
            Normalizar(b, 0.6f); return b;
        }

        /// <summary>Navegar por la interfaz: un clic mecánico corto, como una tecla vieja.</summary>
        static float[] UIBlip()
        {
            int sr = SrEfecto; var b = Buf(0.1, sr); var rnd = new System.Random(43);
            Clic(b, sr, rnd, 0, 0.6f, 1800);
            Golpe(b, sr, 0, 160, 110, 0.015, 0.3f);
            Normalizar(b, 0.4f); return b;
        }

        /// <summary>Terminal o mensaje del sistema: golpes de máquina de escribir sobre un zumbido de monitor viejo.</summary>
        static float[] TerminalBeep()
        {
            int sr = SrEfecto; var b = Buf(0.4, sr); var rnd = new System.Random(44);
            foreach (var t0 in new[] { 0.0, 0.07, 0.12, 0.21 }) { Clic(b, sr, rnd, t0, 0.7f, 2200); Golpe(b, sr, t0, 140, 90, 0.02, 0.25f); }
            for (int i = 0; i < b.Length; i++) { double t = i / (double)sr; b[i] += (Seno(60, t) + Seno(120, t) * 0.5f) * Caida(t, 0.2) * 0.12f; }
            Normalizar(b, 0.45f); return b;
        }

        static float[] ValvulaGiro()
        {
            int sr = SrEfecto; var b = Buf(1.3, sr); var rnd = new System.Random(34); var bp = new PasaBanda(sr); bp.Ajustar(600, 2);
            double fase = 0, f = 90;
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr;
                f = Math.Max(60, Math.Min(130, f + (rnd.NextDouble() - 0.5) * 2)); fase += f / sr;
                float env = (float)(t < 0.1 ? t / 0.1 : t < 1.1 ? 1 : Math.Exp(-(t - 1.1) / 0.05));
                b[i] = bp.Proc(Sierra(fase)) * env * 0.5f;
                if ((int)(t * 3) % 2 == 0) b[i] += Seno(1900 + 300 * Math.Sin(DosPi * 7 * t), t) * 0.04f * env;
            }
            Normalizar(b, 0.7f); return b;
        }

        static float[] Vapor()
        {
            int sr = SrEfecto; var b = Buf(2.0, sr); var rnd = new System.Random(35);
            Ruido(b, sr, rnd, 0, 0.7, 0.8f, 14000, 2000, 0.02);
            Ruido(b, sr, rnd, 0, 0.3, 0.3f, 400, 0, 0.01);
            Normalizar(b, 0.8f); return b;
        }

        static float[] JefeCarga()
        {
            // Rugido que crece: gruñido mecánico grave que sube, siseo de presión y un trémolo cada vez más rápido
            int sr = SrEfecto; var b = Buf(2.3, sr); var rnd = new System.Random(45);
            Grunido(b, sr, rnd, 0, 2.25, 45, 140, 0.8f, 2.0, 0.15);
            var sis = new float[b.Length];
            Ruido(sis, sr, rnd, 0, 3, 0.4f, 12000, 3000, 2.0);
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr, k = Math.Min(1, t / 2.2);
                float trem = (float)(0.6 + 0.4 * Math.Sin(DosPi * (3 + 14 * k) * t));
                b[i] = (b[i] + sis[i] * (float)k) * trem;
            }
            Golpe(b, sr, 0, 50, 30, 0.8, 0.5f);
            Normalizar(b, 0.85f); return b;
        }

        static float[] JefeDescarga()
        {
            int sr = SrEfecto; var b = Buf(2.0, sr); var rnd = new System.Random(36);
            Ruido(b, sr, rnd, 0, 0.15, 1f, 2000);
            Golpe(b, sr, 0, 70, 28, 0.5, 1f);
            for (int k = 0; k < 30; k++) Ruido(b, sr, rnd, 0.1 + rnd.NextDouble() * 1.2, 0.005, 0.3f, 8000, 2000);
            Normalizar(b, 0.97f); return b;
        }

        static float[] Expendedora()
        {
            int sr = SrEfecto; var b = Buf(0.9, sr); var rnd = new System.Random(37);
            Golpe(b, sr, 0, 110, 60, 0.08, 0.8f);
            Ruido(b, sr, rnd, 0, 0.05, 0.4f, 2500, 600);
            Golpe(b, sr, 0.3, 140, 80, 0.05, 0.6f);
            Ruido(b, sr, rnd, 0.3, 0.04, 0.4f, 3000, 800);
            Normalizar(b, 0.85f); return b;
        }

        static float[] Senuelo()
        {
            // Señal falsa de red: ráfaga de estática de radio con un tono grave que "llama"
            int sr = SrEfecto; var b = Buf(0.35, sr); var rnd = new System.Random(46);
            var est = new float[b.Length];
            Ruido(est, sr, rnd, 0, 0.12, 0.6f, 4000, 700, 0.01);
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr;
                b[i] = est[i] * (0.6f + 0.4f * Cuadrada(28 * t)) + Seno(311, t) * Caida(t, 0.1) * 0.12f;
            }
            Normalizar(b, 0.5f); return b;
        }

        static float[] Victoria()
        {
            int sr = SrEfecto; var b = Buf(4.5, sr); var lp = new Polo(500, sr);
            double[] acorde = { 220, 277.18, 329.63, 440, 554.37 };
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr; float s = 0;
                foreach (var f in acorde) s += Sierra(f * t) + Sierra(f * 1.003 * t);
                float env = (float)(t < 1.2 ? t / 1.2 : Math.Exp(-(t - 1.2) / 1.2));
                b[i] = lp.Bajo(s) * env * 0.15f + Seno(1760, t) * env * 0.02f * (0.5f + 0.5f * Seno(6, t));
            }
            Normalizar(b, 0.8f); return b;
        }

        static float[] GameOver()
        {
            int sr = SrEfecto; var b = Buf(4.5, sr); var lp = new Polo(350, sr); var rnd = new System.Random(38);
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)sr;
                float s = Sierra(55 * t) + Sierra(58.27 * t) + Sierra(82.41 * t) * 0.7f;
                b[i] = lp.Bajo(s) * Caida(t, 1.4) * 0.35f;
            }
            Ruido(b, sr, rnd, 0, 0.3, 0.6f, 1200);
            Golpe(b, sr, 0, 60, 30, 0.6, 0.9f);
            Normalizar(b, 0.85f); return b;
        }

        // ======================= Guardado =======================
        static void GuardarWav(string ruta, float[] datos, int sr)
        {
            using (var fs = new FileStream(ruta, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int bytes = datos.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes); w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(sr); w.Write(sr * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
                foreach (var v in datos) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * 32767f), -32768, 32767));
            }
        }

        static AudioClip Guardar(string nombre, float[] datos, int sr, bool musica)
        {
            string ruta = $"{Carpeta}/{nombre}.wav";
            GuardarWav(ruta, datos, sr);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            var imp = (AudioImporter)AssetImporter.GetAtPath(ruta);
            imp.forceToMono = true;
            imp.loadInBackground = musica;
            var ajustes = imp.defaultSampleSettings;
            ajustes.loadType = musica ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            ajustes.compressionFormat = AudioCompressionFormat.Vorbis;
            ajustes.quality = musica ? 0.55f : 0.75f;
            imp.defaultSampleSettings = ajustes;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        }

        [MenuItem("Nexus/Audio/Generar sonidos")]
        public static void Generar()
        {
            Directory.CreateDirectory(Carpeta);
            Directory.CreateDirectory("Assets/Nexus/Resources");
            var inicio = DateTime.Now;
            const string rutaBanco = "Assets/Nexus/Resources/BancoSonidos.asset";
            var banco = AssetDatabase.LoadAssetAtPath<BancoSonidos>(rutaBanco);
            if (banco == null) { banco = ScriptableObject.CreateInstance<BancoSonidos>(); AssetDatabase.CreateAsset(banco, rutaBanco); }

            banco.musicaMenu = Guardar("Musica_Menu", MusicaMenu(), SrMusica, true);
            banco.musicaExploracion = Guardar("Musica_Exploracion", MusicaExploracion(), SrMusica, true);
            banco.musicaTension = Guardar("Musica_Tension", MusicaTension(), SrMusica, true);
            banco.musicaJefe = Guardar("Musica_Jefe", MusicaJefe(), SrMusica, true);
            banco.ambienteVentiladores = Guardar("Ambiente_Ventiladores", Ventiladores(), SrMusica, true);
            banco.sirenaAlarma = Guardar("Alarma_Sirena", Sirena(), SrMusica, false);

            var generadores = new Dictionary<Sfx, Func<float[]>>
            {
                [Sfx.DisparoPistola] = DisparoPistola, [Sfx.DisparoEscopeta] = DisparoEscopeta, [Sfx.EscopetaBombeo] = Bombeo,
                [Sfx.Recarga] = Recarga, [Sfx.CartuchoCarga] = CartuchoCarga, [Sfx.SinMunicion] = SinMunicion, [Sfx.Cuchillo] = Cuchillo,
                [Sfx.ImpactoMetal] = ImpactoMetal, [Sfx.AndroideDetecta] = AndroideDetecta, [Sfx.AndroidePaso] = AndroidePaso,
                [Sfx.AndroideDespierta] = AndroideDespierta, [Sfx.AndroideDestruido] = AndroideDestruido, [Sfx.JugadorPaso] = JugadorPaso,
                [Sfx.JugadorHerido] = JugadorHerido, [Sfx.Puerta] = Puerta, [Sfx.Recoger] = Recoger, [Sfx.UIBlip] = UIBlip,
                [Sfx.TerminalBeep] = TerminalBeep, [Sfx.ValvulaGiro] = ValvulaGiro, [Sfx.Vapor] = Vapor, [Sfx.JefeCarga] = JefeCarga,
                [Sfx.JefeDescarga] = JefeDescarga, [Sfx.Expendedora] = Expendedora, [Sfx.Senuelo] = Senuelo, [Sfx.Victoria] = Victoria,
                [Sfx.GameOver] = GameOver,
            };
            var nombres = Enum.GetNames(typeof(Sfx));
            banco.efectos = new AudioClip[nombres.Length];
            foreach (var kv in generadores)
                banco.efectos[(int)kv.Key] = Guardar("Sfx_" + kv.Key, kv.Value(), SrEfecto, false);
            EditorUtility.SetDirty(banco);
            AssetDatabase.SaveAssets();
            long total = 0; foreach (var f in Directory.GetFiles(Carpeta, "*.wav")) total += new FileInfo(f).Length;
            Debug.Log($"[Nexus] Audio generado: 6 pistas y {generadores.Count} efectos, {total / 1048576f:0.0} MB en WAV, en {(DateTime.Now - inicio).TotalSeconds:0.0} s.");
        }
    }
}
