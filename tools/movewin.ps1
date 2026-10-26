param([int]$X = 147, [int]$Y = 161, [string]$ExeMatch = "vexorlauncher2")

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class WinMoveRel {
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
}
"@

$p = Get-Process | Where-Object { $_.Path -like "*$ExeMatch*" -and $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $p) { Write-Error "window not found"; exit 1 }
[WinMoveRel]::SetWindowPos($p.MainWindowHandle, [IntPtr]::Zero, $X, $Y, 1120, 720, 0x0040) | Out-Null
Write-Output "moved pid=$($p.Id) to $X,$Y"
