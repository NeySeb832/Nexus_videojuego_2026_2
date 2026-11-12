<#
.SYNOPSIS
  Toma una captura de pantalla para la bitácora de evidencias del proyecto Nexus.

.DESCRIPTION
  Guarda un PNG numerado automáticamente en Evidencias\capturas (001_..., 002_...).
  Puede capturar una ventana concreta (por nombre de proceso o parte del título),
  aunque esté detrás de otras ventanas, o la pantalla completa.

.EXAMPLE
  .\Capturar.ps1 -Nombre "editor-abierto" -Proceso Unity
  .\Capturar.ps1 -Nombre "hub-licencia" -Titulo "Unity Hub"
  .\Capturar.ps1 -Nombre "escritorio"            # pantalla completa
#>
param(
    [Parameter(Mandatory = $true)][string]$Nombre,
    [string]$Proceso,
    [string]$Titulo,
    [string]$Carpeta = (Join-Path (Split-Path $PSScriptRoot -Parent) "capturas")
)

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class NexusWin32 {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
}
"@
[NexusWin32]::SetProcessDPIAware() | Out-Null

if (-not (Test-Path $Carpeta)) { New-Item -ItemType Directory -Path $Carpeta | Out-Null }

# Siguiente número correlativo
$ultimo = Get-ChildItem $Carpeta -Filter "*.png" |
    ForEach-Object { if ($_.Name -match '^(\d{3})_') { [int]$Matches[1] } } |
    Sort-Object -Descending | Select-Object -First 1
$num = if ($ultimo) { $ultimo + 1 } else { 1 }
$limpio = ($Nombre -replace '[^\w\-]', '-').ToLower()
$archivo = Join-Path $Carpeta ("{0:D3}_{1}.png" -f $num, $limpio)

$hwnd = [IntPtr]::Zero
if ($Proceso -or $Titulo) {
    $ventanas = Get-Process | Where-Object { $_.MainWindowHandle -ne [IntPtr]::Zero }
    if ($Proceso) { $ventanas = $ventanas | Where-Object { $_.ProcessName -like "*$Proceso*" } }
    if ($Titulo)  { $ventanas = $ventanas | Where-Object { $_.MainWindowTitle -like "*$Titulo*" } }
    $v = $ventanas | Select-Object -First 1
    if (-not $v) { Write-Error "No se encontró ninguna ventana que coincida."; exit 1 }
    $hwnd = $v.MainWindowHandle
    if ([NexusWin32]::IsIconic($hwnd)) { [NexusWin32]::ShowWindow($hwnd, 9) | Out-Null; Start-Sleep -Milliseconds 600 }
}

if ($hwnd -ne [IntPtr]::Zero) {
    $r = New-Object NexusWin32+RECT
    [NexusWin32]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    # 2 = PW_RENDERFULLCONTENT: captura también ventanas con aceleración gráfica (Unity, navegadores)
    [NexusWin32]::PrintWindow($hwnd, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc)
} else {
    Add-Type -AssemblyName System.Windows.Forms
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
}

$bmp.Save($archivo, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output $archivo
