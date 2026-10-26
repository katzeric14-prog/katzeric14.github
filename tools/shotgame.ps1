param([string]$Out = "game_legacy.png", [string]$Match = "1.8.9", [switch]$Close)

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class GameWin {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
Add-Type -AssemblyName System.Drawing

$proc = Get-CimInstance Win32_Process -Filter "Name='java.exe'" |
    Where-Object { $_.CommandLine -and $_.CommandLine.Contains("--version $Match") } |
    Select-Object -First 1
if (-not $proc) { Write-Output "no java process for $Match"; exit 1 }

$p = Get-Process -Id $proc.ProcessId
$h = $p.MainWindowHandle
if ($h -eq [IntPtr]::Zero) { Write-Output "no main window yet"; exit 2 }

if ($Close) {
    [GameWin]::SendMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
    Write-Output "WM_CLOSE sent to $($p.Id)"
    exit 0
}

[GameWin]::ShowWindow($h, 9) | Out-Null
[GameWin]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 700

$r = New-Object GameWin+RECT
[GameWin]::GetWindowRect($h, [ref]$r) | Out-Null
$w = $r.Right - $r.Left
$hgt = $r.Bottom - $r.Top
$bmp = New-Object System.Drawing.Bitmap($w, $hgt)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output "pid=$($p.Id) title=$($p.MainWindowTitle) rect=$($r.Left),$($r.Top) size=${w}x${hgt}"
