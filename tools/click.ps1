param([int]$X, [int]$Y)

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class WinClick {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(int flags, int dx, int dy, int data, int extra);
}
"@

[WinClick]::SetCursorPos($X, $Y) | Out-Null
Start-Sleep -Milliseconds 120
[WinClick]::mouse_event(0x0002, 0, 0, 0, 0)
Start-Sleep -Milliseconds 60
[WinClick]::mouse_event(0x0004, 0, 0, 0, 0)
Write-Output "clicked $X,$Y"
