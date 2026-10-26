param(
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $PSScriptRoot "..\src\main\resources\resourcepacks"
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

$script:Size = 32
$script:Utf8 = New-Object System.Text.UTF8Encoding($false)

function New-Image {
    $bmp = New-Object System.Drawing.Bitmap($script:Size, $script:Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $script:Size; $y++) {
        for ($x = 0; $x -lt $script:Size; $x++) {
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
        }
    }
    return $bmp
}

function C([string]$argb) {
    $a = [Convert]::ToInt32($argb.Substring(0, 2), 16)
    $r = [Convert]::ToInt32($argb.Substring(2, 2), 16)
    $g = [Convert]::ToInt32($argb.Substring(4, 2), 16)
    $b = [Convert]::ToInt32($argb.Substring(6, 2), 16)
    return [System.Drawing.Color]::FromArgb($a, $r, $g, $b)
}

function Set-Pixel($bmp, [int]$x, [int]$y, $color) {
    if ($x -ge 0 -and $x -lt $script:Size -and $y -ge 0 -and $y -lt $script:Size) {
        $bmp.SetPixel($x, $y, $color)
    }
}

function Fill-Rect($bmp, [int]$x, [int]$y, [int]$w, [int]$h, $color) {
    for ($yy = $y; $yy -lt $y + $h; $yy++) {
        for ($xx = $x; $xx -lt $x + $w; $xx++) {
            Set-Pixel $bmp $xx $yy $color
        }
    }
}

function Draw-Line($bmp, [double]$x1, [double]$y1, [double]$x2, [double]$y2, [double]$thickness, $color) {
    $half = $thickness / 2.0
    $dx = $x2 - $x1
    $dy = $y2 - $y1
    $len2 = $dx * $dx + $dy * $dy
    for ($y = 0; $y -lt $script:Size; $y++) {
        for ($x = 0; $x -lt $script:Size; $x++) {
            if ($len2 -le 0.0001) {
                $d = [Math]::Sqrt(($x - $x1) * ($x - $x1) + ($y - $y1) * ($y - $y1))
            }
            else {
                $t = (($x - $x1) * $dx + ($y - $y1) * $dy) / $len2
                if ($t -lt 0) { $t = 0 }
                if ($t -gt 1) { $t = 1 }
                $px = $x1 + $t * $dx
                $py = $y1 + $t * $dy
                $d = [Math]::Sqrt(($x - $px) * ($x - $px) + ($y - $py) * ($y - $py))
            }
            if ($d -le $half) {
                Set-Pixel $bmp $x $y $color
            }
        }
    }
}

function Write-PackMeta([string]$packDir, [string]$description) {
    New-Item -ItemType Directory -Force -Path $packDir | Out-Null
    $json = "{`n  `"pack`": {`n    `"pack_format`": 75,`n    `"description`": `"$description`"`n  }`n}`n"
    [System.IO.File]::WriteAllText((Join-Path $packDir "pack.mcmeta"), $json, $script:Utf8)
}

function Save-Image($bmp, [string]$path) {
    $dir = [System.IO.Path]::GetDirectoryName($path)
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

function Draw-Totem([string]$path, [string]$outlineHex, [string]$bodyHex, [string]$lightHex, [string]$glowHex) {
    $outline = C $outlineHex
    $body = C $bodyHex
    $light = C $lightHex
    $glow = C $glowHex
    $white = C "FFFFFFFF"
    $dark = C "66000000"
    $bmp = New-Image

    Fill-Rect $bmp 8 2 2 3 $outline
    Fill-Rect $bmp 22 2 2 3 $outline
    Fill-Rect $bmp 9 3 1 2 $light
    Fill-Rect $bmp 22 3 1 2 $light

    Fill-Rect $bmp 9 4 14 12 $outline
    Fill-Rect $bmp 6 15 20 5 $outline
    Fill-Rect $bmp 10 19 12 10 $outline
    Fill-Rect $bmp 4 15 4 5 $outline
    Fill-Rect $bmp 24 15 4 5 $outline
    Fill-Rect $bmp 10 28 4 3 $outline
    Fill-Rect $bmp 18 28 4 3 $outline

    Fill-Rect $bmp 10 5 12 10 $body
    Fill-Rect $bmp 7 16 18 3 $body
    Fill-Rect $bmp 11 19 10 9 $body
    Fill-Rect $bmp 5 16 3 3 $body
    Fill-Rect $bmp 24 16 3 3 $body
    Fill-Rect $bmp 11 28 2 2 $body
    Fill-Rect $bmp 19 28 2 2 $body

    Fill-Rect $bmp 10 5 2 9 $light
    Fill-Rect $bmp 7 16 2 2 $light
    Fill-Rect $bmp 11 19 2 8 $light
    Fill-Rect $bmp 5 16 2 1 $light
    Fill-Rect $bmp 24 16 2 1 $light

    Fill-Rect $bmp 16 5 5 9 $dark
    Fill-Rect $bmp 17 16 7 3 $dark
    Fill-Rect $bmp 17 20 3 8 $dark

    Fill-Rect $bmp 13 8 2 3 $glow
    Fill-Rect $bmp 18 8 2 3 $glow
    Set-Pixel $bmp 13 8 $white
    Set-Pixel $bmp 18 8 $white
    Fill-Rect $bmp 14 13 5 1 $light
    Set-Pixel $bmp 14 12 $light
    Set-Pixel $bmp 18 12 $light

    Fill-Rect $bmp 15 21 3 3 $glow
    Set-Pixel $bmp 16 22 $white
    Set-Pixel $bmp 14 22 $light
    Set-Pixel $bmp 18 22 $light
    Set-Pixel $bmp 16 20 $light
    Set-Pixel $bmp 16 24 $light

    Set-Pixel $bmp 4 16 $glow
    Set-Pixel $bmp 27 16 $glow
    Set-Pixel $bmp 4 18 $dark
    Set-Pixel $bmp 27 18 $dark

    Save-Image $bmp $path
}

function Draw-Sword([string]$path, [string]$outlineHex, [string]$bodyHex, [string]$edgeHex, [string]$glowHex, [string]$guardHex, [string]$gripHex) {
    $outline = C $outlineHex
    $body = C $bodyHex
    $edge = C $edgeHex
    $glow = C $glowHex
    $guard = C $guardHex
    $grip = C $gripHex
    $white = C "FFFFFFFF"
    $bmp = New-Image

    Draw-Line $bmp 9.5 23.5 28.5 4.5 6 $outline
    Draw-Line $bmp 6 27.5 10.5 23 5 $outline
    Draw-Line $bmp 5.5 19.5 13 27 4 $outline

    Draw-Line $bmp 10.5 22.5 27.5 5.5 4 $body
    Draw-Line $bmp 10.5 22.5 27.5 5.5 1 $edge
    Draw-Line $bmp 9.5 21.5 26.5 4.5 1 $edge

    Draw-Line $bmp 28.5 4.5 30 3 2 $glow
    Set-Pixel $bmp 30 3 $white
    Set-Pixel $bmp 30 4 $glow
    Set-Pixel $bmp 29 2 $glow
    Set-Pixel $bmp 31 4 $glow

    Draw-Line $bmp 6 27.5 10.5 23 3 $grip
    Draw-Line $bmp 5.5 19.5 13 27 2 $guard
    Fill-Rect $bmp 4 27 3 3 $guard
    Set-Pixel $bmp 4 27 $white

    Set-Pixel $bmp 7 26 $outline
    Set-Pixel $bmp 8 25 $outline
    Set-Pixel $bmp 9 24 $outline

    Set-Pixel $bmp 12 20 $glow
    Set-Pixel $bmp 11 19 $glow
    Set-Pixel $bmp 14 8 $glow
    Set-Pixel $bmp 21 9 $glow
    Set-Pixel $bmp 20 15 $glow

    Save-Image $bmp $path
}

$totems = @(
    @{ Id = "totem_neon";    Description = "Vexor Visual - Neon Totem";    Outline = "FF101A3A"; Body = "FF1F6BFF"; Light = "FF6FE7FF"; Glow = "FFB8FFFF" },
    @{ Id = "totem_gold";    Description = "Vexor Visual - Golden Totem";  Outline = "FF6B4400"; Body = "FFFFC93C"; Light = "FFFFF3B0"; Glow = "FFFFF8DC" },
    @{ Id = "totem_crimson"; Description = "Vexor Visual - Crimson Totem"; Outline = "FF3A0612"; Body = "FFE01E3C"; Light = "FFFF8AA0"; Glow = "FFFFD1DA" },
    @{ Id = "totem_frost";   Description = "Vexor Visual - Frost Totem";   Outline = "FF0B2D4A"; Body = "FF4FC3FF"; Light = "FFD6F4FF"; Glow = "FFFFFFFF" },
    @{ Id = "totem_emerald"; Description = "Vexor Visual - Emerald Totem"; Outline = "FF063A1E"; Body = "FF20C96B"; Light = "FFA8FFD0"; Glow = "FFE8FFF4" }
)

$swords = @(
    @{ Id = "sword_electro"; Description = "Vexor Visual - Electro Swords"; Outline = "FF0A1B2E"; Body = "FF2F8CFF"; Edge = "FF7FE9FF"; Glow = "FFFFFFFF" },
    @{ Id = "sword_neon";    Description = "Vexor Visual - Neon Swords";    Outline = "FF2A0B3A"; Body = "FFB14AFF"; Edge = "FFF2A8FF"; Glow = "FFFFFFFF" },
    @{ Id = "sword_crimson"; Description = "Vexor Visual - Crimson Swords"; Outline = "FF2A0508"; Body = "FFD91E36"; Edge = "FFFF8A8A"; Glow = "FFFFD9D9" },
    @{ Id = "sword_gold";    Description = "Vexor Visual - Golden Swords";  Outline = "FF4A3200"; Body = "FFFFC83D"; Edge = "FFFFF0A0"; Glow = "FFFFFFFF" },
    @{ Id = "sword_emerald"; Description = "Vexor Visual - Emerald Swords"; Outline = "FF04301B"; Body = "FF1FC96B"; Edge = "FF9CFFC9"; Glow = "FFFFFFFF" }
)

$materials = @(
    @{ Name = "wooden_sword";    Guard = "FF8B5A2B"; Grip = "FF3A2412" },
    @{ Name = "stone_sword";     Guard = "FF8A8A8A"; Grip = "FF2E2E33" },
    @{ Name = "iron_sword";      Guard = "FFD8D8D8"; Grip = "FF2B1B12" },
    @{ Name = "golden_sword";    Guard = "FFFFD24A"; Grip = "FF4A3200" },
    @{ Name = "diamond_sword";   Guard = "FF6FE7E0"; Grip = "FF14343A" },
    @{ Name = "netherite_sword"; Guard = "FF5A4358"; Grip = "FF1A111A" },
    @{ Name = "copper_sword";    Guard = "FFE07A3F"; Grip = "FF3A2016" }
)

foreach ($totem in $totems) {
    $packDir = Join-Path $OutputRoot $totem.Id
    Write-PackMeta $packDir $totem.Description
    $texture = Join-Path $packDir "assets\minecraft\textures\item\totem_of_undying.png"
    Draw-Totem $texture $totem.Outline $totem.Body $totem.Light $totem.Glow
    Write-Host "totem: $($totem.Id)"
}

foreach ($sword in $swords) {
    $packDir = Join-Path $OutputRoot $sword.Id
    Write-PackMeta $packDir $sword.Description
    foreach ($material in $materials) {
        $texture = Join-Path $packDir "assets\minecraft\textures\item\$($material.Name).png"
        Draw-Sword $texture $sword.Outline $sword.Body $sword.Edge $sword.Glow $material.Guard $material.Grip
    }
    Write-Host "swords: $($sword.Id)"
}

Write-Host "Done: $OutputRoot"
