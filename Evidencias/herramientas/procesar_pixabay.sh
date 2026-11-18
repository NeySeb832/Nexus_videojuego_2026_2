#!/usr/bin/env bash
# Recorta y nivela los sonidos descargados de Pixabay para Nexus (bitácora 5.4).
#   uso: procesar_pixabay.sh <carpeta con los mp3 originales> <carpeta Assets/Nexus/Audio>
# Cada efecto se recorta a la parte útil, con fundidos cortos para que no haga clic, y se nivela a la
# sonoridad (LUFS) del sonido sintetizado que reemplaza: así se conserva la mezcla que ya estaba
# ajustada en el juego. Si el clip es tan corto que no se puede medir su sonoridad, se iguala el pico.
# Las músicas y el ambiente se convierten en bucles sin corte (el final se funde con el principio).
set -euo pipefail
ORIG="$1"; AUDIO="$2"; SAL="$AUDIO/Pixabay"; TMP="$(mktemp -d)"
mkdir -p "$SAL"

sonoridad() { ffmpeg -hide_banner -nostats -i "$1" -af ebur128 -f null - 2>&1 | grep -A3 Summary | grep -oE 'I: +[-0-9.]+' | grep -oE '[-0-9.]+$'; }
pico() { ffmpeg -hide_banner -nostats -i "$1" -af volumedetect -f null - 2>&1 | grep -oE 'max_volume: [-0-9.]+' | grep -oE '[-0-9.]+$'; }

# Ganancia para que el clip nuevo suene como su referencia sintetizada (o hasta el pico dado)
ganancia() {   # $1 clip nuevo, $2 referencia o número (pico objetivo en dB)
    local in=$(sonoridad "$1") pn=$(pico "$1")
    if [[ "$2" =~ ^-?[0-9.]+$ ]]; then awk -v p="$pn" -v o="$2" 'BEGIN{printf "%.2f", o-p}'; return; fi
    local ir=$(sonoridad "$2") pr=$(pico "$2")
    awk -v a="$in" -v b="$ir" -v pn="$pn" -v pr="$pr" 'BEGIN{ if (a>-60 && b>-60) g=b-a; else g=pr-pn; printf "%.2f", g }'
}

# efecto <nombre> <origen.mp3> <inicio> <fin> <fundido salida> <referencia | pico dB> [fundido entrada]
efecto() {
    local n="$1" src="$ORIG/$2" ss="$3" to="$4" fo="$5" ref="$6" fi="${7:-0.008}"
    local d=$(awk -v a="$ss" -v b="$to" 'BEGIN{print b-a}')
    local st=$(awk -v d="$d" -v f="$fo" 'BEGIN{print d-f}')
    # atrim y no -ss: en estos MP3 la búsqueda rápida de ffmpeg cae unas décimas antes o después
    ffmpeg -hide_banner -loglevel error -y -i "$src" -ac 1 -ar 44100 \
        -af "atrim=$ss:$to,asetpts=N/SR/TB,afade=t=in:d=$fi,afade=t=out:st=$st:d=$fo" "$TMP/$n.wav"
    [[ "$ref" =~ ^-?[0-9.]+$ ]] || ref="$AUDIO/$ref.wav"
    local g=$(ganancia "$TMP/$n.wav" "$ref")
    ffmpeg -hide_banner -loglevel error -y -i "$TMP/$n.wav" -af "volume=${g}dB,alimiter=limit=0.94:level=false" -c:a pcm_s16le "$SAL/$n.wav"
    printf "%-24s %5.2fs  ganancia %6s dB  pico final %s dB\n" "$n" "$d" "$g" "$(pico "$SAL/$n.wav")"
}

# bucle <nombre> <origen.mp3> <LUFS objetivo> <cruce s> [duración máxima]
bucle() {
    local n="$1" src="$ORIG/$2" lufs="$3" x="$4" max="${5:-}"
    local L=$(ffprobe -v error -show_entries format=duration -of csv=p=0 "$src")
    [ -n "$max" ] && L=$(awk -v a="$L" -v b="$max" 'BEGIN{print (a<b)?a:b}')
    local cola=$(awk -v l="$L" -v x="$x" 'BEGIN{print l-x}')
    # cabeza = inicio (entrando) + final (saliendo); cuerpo = el resto; la cabeza empalma con el final del cuerpo
    ffmpeg -hide_banner -loglevel error -y -i "$src" -filter_complex \
        "[0:a]aresample=44100,asplit=3[a][b][c];\
         [a]atrim=0:$x,asetpts=N/SR/TB,afade=t=in:d=$x:curve=qsin[ini];\
         [b]atrim=$cola:$L,asetpts=N/SR/TB,afade=t=out:d=$x:curve=qsin[fin];\
         [ini][fin]amix=inputs=2:normalize=0[cab];\
         [c]atrim=$x:$cola,asetpts=N/SR/TB[cue];\
         [cab][cue]concat=n=2:v=0:a=1[s]" -map "[s]" "$TMP/$n.wav"
    local g=$(awk -v a="$(sonoridad "$TMP/$n.wav")" -v o="$lufs" 'BEGIN{printf "%.2f", o-a}')
    ffmpeg -hide_banner -loglevel error -y -i "$TMP/$n.wav" -af "volume=${g}dB,alimiter=limit=0.94:level=false" -c:a libvorbis -q:a 6 "$SAL/$n.ogg"
    printf "%-24s %6.1fs  ganancia %6s dB  -> %s LUFS\n" "$n" "$(ffprobe -v error -show_entries format=duration -of csv=p=0 "$SAL/$n.ogg")" "$g" "$(sonoridad "$SAL/$n.ogg")"
}

echo "== Música y ambiente =="
bucle Px_Musica_Menu        Musica_Menu.mp3             -17   2.0
bucle Px_Musica_Exploracion Musica_Exploracion.mp3      -19.5 3.0
bucle Px_Musica_Tension     Musica_Tension.mp3          -19.5 1.5
bucle Px_Musica_Jefe        Musica_Jefe.mp3             -19   1.5
bucle Px_Ambiente_Servidores Ambiente_SalaServidores.mp3 -19  3.0
bucle Px_Alarma_Sirena      Alarma_Sirena.mp3           -7    0.15

echo "== Efectos =="
efecto Px_DisparoPistola    DisparoPistola.mp3   0.12 1.36 0.30 Sfx_DisparoPistola
efecto Px_DisparoEscopeta   DisparoEscopeta.mp3  0.00 1.97 0.45 Sfx_DisparoEscopeta
efecto Px_EscopetaBombeo    EscopetaBombeo.mp3   0.05 0.95 0.10 Sfx_EscopetaBombeo
efecto Px_Recarga           Recarga.mp3          0.00 0.66 0.06 Sfx_Recarga
efecto Px_CartuchoCarga     CartuchoCarga.mp3    0.08 0.42 0.05 Sfx_CartuchoCarga
efecto Px_SinMunicion       SinMunicion.mp3      0.00 0.63 0.10 Sfx_SinMunicion
efecto Px_Cuchillo          Cuchillo.mp3         0.00 0.40 0.08 Sfx_Cuchillo
efecto Px_ImpactoMetal      ImpactoMetal.mp3     0.17 1.10 0.25 Sfx_ImpactoMetal
efecto Px_AndroideDetecta   AndroideDetecta.mp3  0.00 2.47 0.50 Sfx_AndroideDetecta
efecto Px_AndroidePaso      AndroidePaso.mp3     0.10 1.02 0.20 Sfx_AndroidePaso
efecto Px_AndroideDestruido AndroideDestruido.mp3 0.36 1.45 0.15 Sfx_AndroideDestruido
efecto Px_JugadorHerido     JugadorHerido.mp3    0.11 0.62 0.10 Sfx_JugadorHerido
efecto Px_JugadoraHerida    JugadoraHerida.mp3   0.03 0.45 0.12 Sfx_JugadorHerido
efecto Px_Puerta            Puerta.mp3           0.20 2.10 0.35 Sfx_Puerta
efecto Px_Recoger           Recoger.mp3          0.00 0.45 0.05 Sfx_Recoger
efecto Px_UIBlip            UIBlip.mp3           0.00 0.18 0.03 Sfx_UIBlip
efecto Px_TerminalBeep      TerminalBeep.mp3     0.55 1.90 0.30 Sfx_TerminalBeep 0.05
efecto Px_ValvulaGiro       ValvulaGiro_alt.mp3  0.00 1.60 0.40 Sfx_ValvulaGiro 0.05
efecto Px_Vapor             Vapor.mp3            0.08 0.78 0.20 Sfx_Vapor
efecto Px_JefeCarga         JefeCarga.mp3        0.00 2.94 0.20 Sfx_JefeCarga
efecto Px_JefeDescarga      JefeDescarga.mp3     0.00 1.30 0.30 Sfx_JefeDescarga
efecto Px_Expendedora       Expendedora.mp3      3.60 6.40 0.30 Sfx_Expendedora 0.05
efecto Px_Senuelo           Senuelo.mp3          1.65 2.20 0.12 Sfx_Senuelo 0.04
efecto Px_GameOver          GameOver.mp3         0.00 4.80 1.00 Sfx_GameOver
# Pasos del técnico: cuatro pisadas distintas de la misma grabación (se alternan al azar)
efecto Px_JugadorPaso_1     JugadorPaso.mp3      0.27 0.60 0.08 -3
efecto Px_JugadorPaso_2     JugadorPaso.mp3      1.57 1.90 0.08 -3
efecto Px_JugadorPaso_3     JugadorPaso.mp3      3.12 3.45 0.08 -3
efecto Px_JugadorPaso_4     JugadorPaso.mp3      4.07 4.40 0.08 -3

# Ascensor entre niveles (bitácora 6.11): de una grabación de montacargas industrial de 35 s
efecto Px_AscensorPuertas   Ascensor_Mecanico.mp3 0.60 2.40 0.30 -4
efecto Px_AscensorMarcha    Ascensor_Mecanico.mp3 2.90 11.0 1.00 -3
efecto Px_AscensorParada    Ascensor_Mecanico.mp3 29.0 32.5 0.80 -3

# El androide al despertar: arranque y servos, sin el silencio que hay en medio de la grabación
ffmpeg -hide_banner -loglevel error -y -i "$ORIG/AndroideDespierta.mp3" -filter_complex \
    "[0:a]aresample=44100,pan=mono|c0=0.5*c0+0.5*c1,asplit=2[a][b];[a]atrim=0.15:0.92,asetpts=N/SR/TB,afade=t=out:st=0.67:d=0.1[x];\
     [b]atrim=1.65:2.42,asetpts=N/SR/TB,afade=t=in:d=0.02,afade=t=out:st=0.57:d=0.2[y];[x][y]concat=n=2:v=0:a=1[s]" -map "[s]" "$TMP/Px_AndroideDespierta.wav"
g=$(ganancia "$TMP/Px_AndroideDespierta.wav" "$AUDIO/Sfx_AndroideDespierta.wav")
ffmpeg -hide_banner -loglevel error -y -i "$TMP/Px_AndroideDespierta.wav" -af "volume=${g}dB,alimiter=limit=0.94:level=false" -c:a pcm_s16le "$SAL/Px_AndroideDespierta.wav"
echo "Px_AndroideDespierta     ganancia $g dB"

# Victoria: la pieza orquestal acortada a 48 s, con fundido final, a la sonoridad del remate anterior
ffmpeg -hide_banner -loglevel error -y -t 48 -i "$ORIG/Musica_Victoria.mp3" -ar 44100 -af "afade=t=out:st=43:d=5" "$TMP/v.wav"
g=$(awk -v a="$(sonoridad "$TMP/v.wav")" 'BEGIN{printf "%.2f", -18-a}')
ffmpeg -hide_banner -loglevel error -y -i "$TMP/v.wav" -af "volume=${g}dB,alimiter=limit=0.94:level=false" -c:a libvorbis -q:a 6 "$SAL/Px_Victoria.ogg"
echo "Px_Victoria              ganancia $g dB"
rm -rf "$TMP"
