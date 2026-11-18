#!/usr/bin/env bash
# Compila los documentos de Nexus (APA 7) con pdflatex + biber.
#   uso: bash compilar.sh [carpeta/archivo.tex ...]   (sin argumentos compila los tres)
cd "$(dirname "$0")"
docs=("$@"); [ ${#docs[@]} -eq 0 ] && docs=(manual_windows/Manual_Instalacion_Windows.tex manual_android/Manual_Instalacion_Android.tex desarrollo/Desarrollo_Nexus.tex)
for d in "${docs[@]}"; do
  [ -f "$d" ] || { echo "no existe $d"; continue; }
  dir=$(dirname "$d"); base=$(basename "$d" .tex)
  ( cd "$dir" && pdflatex -interaction=nonstopmode "$base.tex" >/dev/null 2>&1; biber "$base" >/dev/null 2>&1
    pdflatex -interaction=nonstopmode "$base.tex" >/dev/null 2>&1; pdflatex -interaction=nonstopmode "$base.tex" >/dev/null 2>&1
    echo "== $d: $(grep -c '^!' "$base.log") errores, $(grep -c 'undefined' "$base.log") referencias sin definir, $(grep -o 'Output written on .*' "$base.log")" )
done
