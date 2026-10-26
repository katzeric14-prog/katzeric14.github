param(
    [string]$Out = "shot.png",
    [string]$ExeMatch = "vexorlauncher2",
    [string]$Title = "Vexor Launcher",
    [switch]$Full
)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class WinShot {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}
"@

$p = Get-Process | Where-Object { $_.Path -like "*$ExeMatch*" -and $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $p) { Write-Error "window not found"; exit 1 }

$shell = New-Object -ComObject WScript.Shell
$shell.AppActivate($Title) | Out-Null
Start-Sleep -Milliseconds 400
[WinShot]::ShowWindow($p.MainWindowHandle, 9) | Out-Null
for ($i = 0; $i -lt 8; $i++) {
    if ([WinShot]::GetForegroundWindow() -eq $p.MainWindowHandle) { break }
    [WinShot]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 250
}
Start-Sleep -Milliseconds 500

$r = New-Object 'WinShot+RECT'
[WinShot]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$w = $r.Right - $r.Left
$h = $r.Bottom - $r.Top
if ($Full) {
    $r.Left = 0
    $r.Top = 0
    $w = [System.Windows.Forms.SystemInformation]::VirtualScreen.Width
    $h = [System.Windows.Forms.SystemInformation]::VirtualScreen.Height
}

$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()

Write-Output ("pid=" + $p.Id + " rect=" + $r.Left + "," + $r.Top + " size=" + $w + "x" + $h)
