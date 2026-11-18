<#
.SYNOPSIS
  Captura una ventana concreta por su título (aunque otras la tapen) y la guarda en un PNG.
  Se usó para las capturas de los manuales de instalación (Explorador, asistente de extracción,
  aviso de SmartScreen y la ventana del juego).

.EXAMPLE
  .\capturar_ventana.ps1 -Titulo "Nexus_Manual" -Salida C:\ruta\explorador.png -Ancho 1100 -Alto 640
  .\capturar_ventana.ps1 -Listar        # muestra los títulos de las ventanas visibles
#>
param(
    [string]$Titulo,
    [string]$Salida,
    [int]$Ancho = 0,
    [int]$Alto = 0,
    [switch]$Listar
)

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class VentanasNexus {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc f, IntPtr l);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int hh, uint f);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int a, out RECT r, int size);
    public static List<KeyValuePair<IntPtr, string>> Visibles() {
        var l = new List<KeyValuePair<IntPtr, string>>();
        EnumWindows((h, p) => {
            if (!IsWindowVisible(h)) return true;
            var sb = new StringBuilder(512); GetWindowText(h, sb, 512);
            if (sb.Length > 0) l.Add(new KeyValuePair<IntPtr, string>(h, sb.ToString()));
            return true;
        }, IntPtr.Zero);
        return l;
    }
}
"@
[VentanasNexus]::SetProcessDPIAware() | Out-Null

if ($Listar) { [VentanasNexus]::Visibles() | ForEach-Object { $_.Value }; exit 0 }

$v = [VentanasNexus]::Visibles() | Where-Object { $_.Value -like $Titulo } | Select-Object -First 1
if (-not $v) { Write-Error "No hay ninguna ventana visible con el título '$Titulo'."; exit 1 }
$hwnd = $v.Key

if ($Ancho -gt 0 -and $Alto -gt 0) {
    # Tamaño fijo para que todas las capturas del manual se vean iguales (sin cambiar el orden de las ventanas)
    [VentanasNexus]::SetWindowPos($hwnd, [IntPtr]::Zero, 80, 60, $Ancho, $Alto, 0x0014) | Out-Null
    Start-Sleep -Milliseconds 700
}

# Rectángulo visible (sin la sombra invisible que Windows 11 añade alrededor de la ventana)
$r = New-Object VentanasNexus+RECT
$ok = [VentanasNexus]::DwmGetWindowAttribute($hwnd, 9, [ref]$r, 16) -eq 0
$rw = New-Object VentanasNexus+RECT
[VentanasNexus]::GetWindowRect($hwnd, [ref]$rw) | Out-Null
if (-not $ok) { $r = $rw }

$w = $rw.Right - $rw.Left; $h = $rw.Bottom - $rw.Top
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[VentanasNexus]::PrintWindow($hwnd, $hdc, 2) | Out-Null   # 2 = PW_RENDERFULLCONTENT
$g.ReleaseHdc($hdc); $g.Dispose()

# Recorta la sombra: se queda con la parte visible de la ventana
$rect = New-Object System.Drawing.Rectangle ($r.Left - $rw.Left), ($r.Top - $rw.Top), ($r.Right - $r.Left), ($r.Bottom - $r.Top)
$final = $bmp.Clone($rect, $bmp.PixelFormat)
$final.Save($Salida, [System.Drawing.Imaging.ImageFormat]::Png)
$final.Dispose(); $bmp.Dispose()
Write-Output "$Salida ($($rect.Width)x$($rect.Height)) <- '$($v.Value)'"
