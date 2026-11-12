using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    /// <summary>
    /// Pantalla final (Canvas 3) con estética de fallo crítico del sistema Nexus:
    /// destello e interferencia de entrada, título con efecto glitch (capas roja y cian desfasadas),
    /// el "ojo" del androide SENTINEL (anillos de radar girando con un núcleo que late), retrato del
    /// técnico con estática, registro de terminal que se escribe solo, monitor de pulso que se aplana
    /// e informe del incidente. La versión de Victoria usa la misma pantalla en verde/cian.
    /// Todo se anima con tiempo no escalado porque el juego queda en pausa (timeScale = 0).
    /// </summary>
    public class PantallaFinal : MonoBehaviour
    {
        [Header("Estructura")]
        [SerializeField] GameObject canvasHud;
        [SerializeField] RectTransform contenido;
        [SerializeField] CanvasGroup[] bloques;           // anillos, título, retrato, registro, pulso, informe, cabecera
        [SerializeField] Graphic[] acentos;                // se tiñen con el color del resultado

        [Header("Fondo y efectos")]
        [SerializeField] Image fondo;
        [SerializeField] RawImage escaneo, estatica, rejilla;
        [SerializeField] Image destello;

        [Header("Ojo SENTINEL")]
        [SerializeField] RectTransform anilloRadar, anilloSegmentos, anilloPunteado;
        [SerializeField] Image nucleo;

        [Header("Textos")]
        [SerializeField] TMP_Text titulo, tituloR, tituloC, motivo, codigo, estadoEnlace, etiquetaRetrato, registro, informe, etiquetaPulso;

        [Header("Técnico")]
        [SerializeField] Image retrato;
        [SerializeField] Sprite[] retratos;
        [SerializeField] RawImage estaticaRetrato;

        [Header("Pulso")]
        [SerializeField] Image pulsoFinal;
        [SerializeField] RawImage pulsoVivo;

        [Header("Botones")]
        [SerializeField] Button[] botones;
        [SerializeField] TMP_Text[] textosBotones;

        static readonly Color Rojo = new Color(1f, 0.22f, 0.16f);
        static readonly Color Verde = new Color(0.3f, 1f, 0.75f);

        Color acento;
        bool victoria, activa;
        float proximoGlitch, finGlitch;
        Vector2[] posBotones;

        void Awake()
        {
            posBotones = new Vector2[botones.Length];
            for (int i = 0; i < botones.Length; i++) posBotones[i] = ((RectTransform)botones[i].transform).anchoredPosition;
        }

        public void Mostrar(bool esVictoria, string textoTitulo, string textoMotivo)
        {
            victoria = esVictoria;
            activa = true;
            if (canvasHud != null) canvasHud.SetActive(false);
            StopAllCoroutines();
            StartCoroutine(Secuencia(textoTitulo, textoMotivo));
        }

        // ======================= Secuencia de entrada =======================
        IEnumerator Secuencia(string textoTitulo, string textoMotivo)
        {
            acento = victoria ? Verde : Rojo;
            foreach (var g in acentos) { var c = acento; c.a = g.color.a; g.color = c; }
            registro.color = Color.Lerp(acento, Color.white, 0.55f);
            foreach (var b in botones)
            {
                var cb = b.colors;
                cb.highlightedColor = new Color(acento.r * 0.45f, acento.g * 0.45f, acento.b * 0.45f, 0.95f);
                cb.selectedColor = cb.highlightedColor;
                cb.pressedColor = new Color(acento.r * 0.7f, acento.g * 0.7f, acento.b * 0.7f, 1f);
                b.colors = cb;
            }
            fondo.color = victoria ? new Color(0f, 0.035f, 0.045f, 0.96f) : new Color(0.03f, 0f, 0f, 0.95f);
            rejilla.color = new Color(acento.r, acento.g, acento.b, 0.07f);
            nucleo.color = new Color(acento.r, acento.g, acento.b, 0.55f);
            titulo.color = victoria ? new Color(0.85f, 1f, 0.95f) : new Color(1f, 0.85f, 0.82f);
            tituloR.color = new Color(1f, 0.1f, 0.15f, 0.75f);
            tituloC.color = new Color(0.1f, 0.9f, 1f, 0.65f);
            foreach (var t in new[] { titulo, tituloR, tituloC }) { t.text = textoTitulo; t.maxVisibleCharacters = 0; }
            motivo.text = textoMotivo; motivo.maxVisibleCharacters = 0;
            codigo.text = victoria ? "NEXUS OS  //  REINICIO MANUAL COMPLETADO" : "NEXUS OS  //  FALLO CRÍTICO DEL SISTEMA  //  0x5E47-1NE1";
            registro.text = "";
            informe.text = Informe();
            PrepararTecnico();
            PrepararBotones();
            foreach (var b in bloques) b.alpha = 0f;
            pulsoFinal.gameObject.SetActive(!victoria);
            pulsoVivo.gameObject.SetActive(victoria);
            pulsoFinal.fillAmount = 0f;
            etiquetaPulso.text = victoria ? "SIGNOS VITALES  ·  ESTABLES" : "SIGNOS VITALES  ·  SIN SEÑAL";

            // 1) Destello y estática fuerte, como un monitor que se apaga de golpe
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime * 4f)
            {
                destello.color = new Color(1f, 1f, 1f, 1f - t);
                estatica.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.6f, 0.035f, t));
                yield return null;
            }
            destello.color = Color.clear;

            // 2) El ojo del SENTINEL se abre: los anillos llegan desde fuera
            StartCoroutine(Aparecer(bloques[0], 0.5f, anilloRadar, 2.2f));
            StartCoroutine(Aparecer(bloques[6], 0.4f, null, 1f));
            yield return new WaitForSecondsRealtime(0.25f);

            // 3) Título escrito letra a letra con glitch
            bloques[1].alpha = 1f;
            for (int i = 0; i <= textoTitulo.Length; i++)
            {
                foreach (var t in new[] { titulo, tituloR, tituloC }) t.maxVisibleCharacters = i;
                if (i % 3 == 0) Glitch(0.06f);
                yield return new WaitForSecondsRealtime(0.045f);
            }
            for (int i = 0; i <= textoMotivo.Length; i++) { motivo.maxVisibleCharacters = i; yield return new WaitForSecondsRealtime(0.015f); }

            // 4) Técnico, monitor de pulso e informe
            StartCoroutine(Aparecer(bloques[2], 0.4f, null, 1f));
            StartCoroutine(Aparecer(bloques[4], 0.4f, null, 1f));
            StartCoroutine(Aparecer(bloques[5], 0.6f, null, 1f));
            if (!victoria) StartCoroutine(Aplanar());

            // 5) Registro de terminal, línea a línea
            // (maxVisibleCharacters cuenta solo letras visibles: las etiquetas nunca aparecen a medias)
            bloques[3].alpha = 1f;
            registro.text = "";
            registro.maxVisibleCharacters = 0;
            int visibles = 0;
            foreach (var linea in Registro())
            {
                registro.text += linea + "\n";
                registro.ForceMeshUpdate();
                int total = registro.textInfo.characterCount;
                for (; visibles <= total; visibles += 2)
                {
                    registro.maxVisibleCharacters = visibles;
                    yield return new WaitForSecondsRealtime(0.01f);
                }
                visibles = total;
                registro.maxVisibleCharacters = total;
                yield return new WaitForSecondsRealtime(0.12f);
            }

            // 6) Los comandos entran deslizándose desde abajo
            for (int i = 0; i < botones.Length; i++)
                if (botones[i].gameObject.activeSelf) StartCoroutine(Deslizar(botones[i], posBotones[i], i * 0.12f));
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (var b in botones) if (b.gameObject.activeSelf && b.interactable) { b.Select(); break; }
        }

        IEnumerator Aparecer(CanvasGroup g, float duracion, RectTransform escalar, float escalaInicial)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duracion)
            {
                float k = 1f - Mathf.Pow(1f - t, 3f);
                g.alpha = k;
                if (escalar != null) g.transform.localScale = Vector3.one * Mathf.Lerp(escalaInicial, 1f, k);
                yield return null;
            }
            g.alpha = 1f;
            g.transform.localScale = Vector3.one;
        }

        IEnumerator Aplanar()
        {
            // La línea dibuja un último latido y queda plana
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 2.6f)
            {
                pulsoFinal.fillAmount = t;
                yield return null;
            }
            pulsoFinal.fillAmount = 1f;
        }

        IEnumerator Deslizar(Button b, Vector2 destino, float retraso)
        {
            var rt = (RectTransform)b.transform;
            var g = b.GetComponent<CanvasGroup>();
            rt.anchoredPosition = destino + Vector2.down * 90f;
            g.alpha = 0f;
            yield return new WaitForSecondsRealtime(retraso);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                float k = 1f - Mathf.Pow(1f - t, 3f);
                rt.anchoredPosition = Vector2.Lerp(destino + Vector2.down * 90f, destino, k);
                g.alpha = k;
                yield return null;
            }
            rt.anchoredPosition = destino;
            g.alpha = 1f;
        }

        // ======================= Animación continua =======================
        void Update()
        {
            if (!activa) return;
            float t = Time.unscaledTime;

            // Ojo del SENTINEL
            anilloRadar.localRotation = Quaternion.Euler(0f, 0f, -t * 9f);
            anilloSegmentos.localRotation = Quaternion.Euler(0f, 0f, t * 24f);
            anilloPunteado.localRotation = Quaternion.Euler(0f, 0f, -t * 45f);
            float latido = victoria ? 0.5f + 0.5f * Mathf.Sin(t * 2f) : Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 2.4f)), 8f);
            nucleo.transform.localScale = Vector3.one * (0.92f + 0.14f * latido);
            var cn = nucleo.color; cn.a = 0.35f + 0.35f * latido; nucleo.color = cn;

            // Líneas de escaneo que bajan, estática que cambia cada cuadro
            escaneo.uvRect = new Rect(0f, -t * 0.6f, escaneo.uvRect.width, escaneo.uvRect.height);
            estatica.uvRect = new Rect(UnityEngine.Random.value, UnityEngine.Random.value, estatica.uvRect.width, estatica.uvRect.height);
            if (estaticaRetrato != null)
            {
                estaticaRetrato.uvRect = new Rect(UnityEngine.Random.value, UnityEngine.Random.value, 1f, 1f);
                var ce = estaticaRetrato.color; ce.a = victoria ? 0f : 0.25f + 0.25f * Mathf.PerlinNoise(t * 3f, 0f); estaticaRetrato.color = ce;
            }
            if (pulsoVivo.gameObject.activeSelf) pulsoVivo.uvRect = new Rect(t * 0.3f, 0f, 2f, 1f);

            estadoEnlace.alpha = Mathf.Repeat(t, 1f) < 0.6f ? 1f : 0.25f;

            // Glitch periódico: todo se descuadra un instante y las capas de color se separan
            if (t >= proximoGlitch)
            {
                proximoGlitch = t + UnityEngine.Random.Range(1.2f, 3.2f);
                Glitch(UnityEngine.Random.Range(0.08f, 0.2f));
            }
            bool enGlitch = t < finGlitch;
            float amp = enGlitch ? 14f : 2.5f;
            tituloR.rectTransform.anchoredPosition = titulo.rectTransform.anchoredPosition + new Vector2(-amp * 0.6f + Ruido(t, 1f) * amp * 0.5f, Ruido(t, 2f) * 1.5f);
            tituloC.rectTransform.anchoredPosition = titulo.rectTransform.anchoredPosition + new Vector2(amp * 0.6f + Ruido(t, 3f) * amp * 0.5f, Ruido(t, 4f) * 1.5f);
            contenido.anchoredPosition = enGlitch ? new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-3f, 3f)) : Vector2.zero;
            if (!enGlitch && estatica.color.a > 0.04f) estatica.color = new Color(1f, 1f, 1f, Mathf.MoveTowards(estatica.color.a, 0.035f, Time.unscaledDeltaTime));
        }

        void Glitch(float duracion)
        {
            finGlitch = Time.unscaledTime + duracion;
            estatica.color = new Color(1f, 1f, 1f, 0.22f);
        }

        static float Ruido(float t, float semilla) => Mathf.PerlinNoise(t * 7f, semilla * 10f) * 2f - 1f;

        // ======================= Contenido =======================
        void PrepararTecnico()
        {
            int p = Mathf.Clamp(EstadoPartida.PersonajeElegido, 0, retratos.Length - 1);
            retrato.sprite = retratos[p];
            retrato.color = victoria ? Color.white : new Color(1f, 0.55f, 0.52f);
            string hex = ColorUtility.ToHtmlStringRGB(acento);
            etiquetaRetrato.text = $"TÉCNICO: {EstadoPartida.NombrePersonaje}\n<color=#{hex}>" +
                                   (victoria ? "MISIÓN CUMPLIDA" : "SEÑAL BIOMÉTRICA PERDIDA") + "</color>";
            estadoEnlace.text = victoria ? "ENLACE: ESTABLE" : "ENLACE: PERDIDO";
        }

        void PrepararBotones()
        {
            var nivel = NivelNexus.Instancia;
            string[] textos = victoria
                ? new[] { "", "", "[ MENÚ PRINCIPAL ]" }
                : new[] { "[ REINTENTAR NIVEL ]", "[ CARGAR RESPALDO ]", "[ MENÚ PRINCIPAL ]" };
            Action[] acciones = { () => nivel?.Reintentar(), () => nivel?.CargarPartida(), () => nivel?.IrAlMenu() };
            for (int i = 0; i < botones.Length; i++)
            {
                botones[i].onClick.RemoveAllListeners();
                bool visible = textos[i].Length > 0;
                botones[i].gameObject.SetActive(visible);
                textosBotones[i].text = textos[i];
                int k = i;
                botones[i].onClick.AddListener(() => acciones[k]());
                botones[i].GetComponent<CanvasGroup>().alpha = 0f;
            }
            botones[1].interactable = EstadoPartida.HayGuardado;
            if (!EstadoPartida.HayGuardado) textosBotones[1].text = "[ SIN RESPALDO ]";
            if (victoria) posBotones[2] = new Vector2(0f, posBotones[2].y);
        }

        string Informe()
        {
            var nivel = NivelNexus.Instancia;
            float t = nivel != null ? nivel.TiempoRestante : 0f;
            string hex = ColorUtility.ToHtmlStringRGB(acento);
            string Fila(string a, string b) => $"{a}<pos=62%><color=#{hex}>{b}</color>\n";
            return $"<size=115%><color=#{hex}>INFORME DEL INCIDENTE</color></size>\n" +
                   Fila("NIVEL", nivel != null ? nivel.NombreNivel.Split('—')[0].Trim() : "-") +
                   Fila("DISCOS MAESTROS", $"{EstadoPartida.Discos}/{EstadoPartida.DiscosTotales}") +
                   Fila("CARTUCHOS", EstadoPartida.Refrigerantes.ToString()) +
                   Fila("NOTAS LEÍDAS", EstadoPartida.NotasLeidas.Count.ToString()) +
                   Fila("OPORTUNIDADES", $"{EstadoPartida.Salud}/{EstadoPartida.SaludMaxima}") +
                   Fila("REFRIGERACIÓN", $"{(int)(t / 60f):00}:{(int)(t % 60f):00}");
        }

        List<string> Registro()
        {
            var nivel = NivelNexus.Instancia;
            string hora = DateTime.Now.ToString("HH:mm:ss");
            string respaldo = EstadoPartida.HayGuardado ? "SÍ" : "NO";
            string lugar = nivel != null ? nivel.NombreNivel.ToUpperInvariant() : "SECTOR DESCONOCIDO";
            if (victoria)
                return new List<string>
                {
                    $"> [{hora}] DISCOS MAESTROS VERIFICADOS<pos=58%>3/3",
                    "> REINICIO MANUAL DEL SISTEMA<pos=58%>OK",
                    "> ACTUALIZACIÓN CERBERUS 7.1<pos=58%>PURGADA",
                    "> ANDROIDES SENTINEL<pos=58%>DESACTIVADOS",
                    "> REFRIGERACIÓN<pos=58%>RESTABLECIDA",
                    "> TEMPERATURA DEL NÚCLEO: 41% Y BAJANDO"
                };
            bool termico = nivel != null && nivel.TiempoRestante <= 0.01f;
            if (termico)
                return new List<string>
                {
                    $"> [{hora}] TEMPERATURA DEL NÚCLEO<pos=58%>100%",
                    "> SISTEMA DE REFRIGERACIÓN<pos=58%>FALLIDO",
                    "> RACKS B-1 A B-9<pos=58%>EN COMBUSTIÓN",
                    "> PUERTAS DE EVACUACIÓN<pos=58%>BLOQUEADAS",
                    $"> DISCOS MAESTROS RECUPERADOS<pos=58%>{EstadoPartida.Discos}/{EstadoPartida.DiscosTotales}",
                    $"> MISIÓN FALLIDA  ·  RESPALDO DISPONIBLE: {respaldo}"
                };
            return new List<string>
            {
                $"> [{hora}] ANOMALÍA DETECTADA — {lugar}",
                "> PROTOCOLO SENTINEL<pos=58%>PURGA EJECUTADA",
                "> OPORTUNIDADES DE EVASIÓN<pos=58%>0/3",
                "> ENLACE BIOMÉTRICO DEL TÉCNICO<pos=58%>PERDIDO",
                $"> TEMPERATURA DEL NÚCLEO<pos=58%>{(nivel != null ? nivel.Temperatura : 100f):0}%",
                $"> MISIÓN FALLIDA  ·  RESPALDO DISPONIBLE: {respaldo}"
            };
        }
    }
}
