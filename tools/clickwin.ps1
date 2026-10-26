param([int]$X, [int]$Y, [string]$ExeMatch = "vexorlauncher2", [string]$Title = "Vexor Launcher")

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class WinClickRel {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(int flags, int dx, int dy, int data, int extra);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

$p = Get-Process | Where-Object { $_.Path -like "*$ExeMatch*" -and $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $p) { Write-Error "window not found"; exit 1 }

$shell = New-Object -ComObject WScript.Shell
$shell.AppActivate($Title) | Out-Null
Start-Sleep -Milliseconds 500
[WinClickRel]::ShowWindow($p.MainWindowHandle, 9) | Out-Null
for ($i = 0; $i -lt 8; $i++) {
    if ([WinClickRel]::GetForegroundWindow() -eq $p.MainWindowHandle) { break }
    [WinClickRel]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 250
}
Start-Sleep -Milliseconds 200

$r = New-Object 'WinClickRel+RECT'
[WinClickRel]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$absX = $r.Left + $X
$absY = $r.Top + $Y
[WinClickRel]::SetCursorPos($absX, $absY) | Out-Null
Start-Sleep -Milliseconds 120
[WinClickRel]::mouse_event(0x0002, 0, 0, 0, 0)
Start-Sleep -Milliseconds 60
[WinClickRel]::mouse_event(0x0004, 0, 0, 0, 0)
Write-Output "clicked $absX,$absY (rect $($r.Left),$($r.Top))"
