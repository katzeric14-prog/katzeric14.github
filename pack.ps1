<#
.SYNOPSIS
    Собирает Vexor Launcher в один переносимый dist\VexorLauncher.exe.

.DESCRIPTION
    Скрипт сам находит все .cs в src, встраивает иконку, манифест и PNG-ресурсы
    и вызывает csc.exe из .NET Framework 4.x. Внешних DLL и файлов не остаётся.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\pack.ps1

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\pack.ps1 -Run
#>
[CmdletBinding()]
param(
    [string]$Output = 'dist\VexorLauncher.exe',
    [switch]$Run
)

$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }
$root = (Resolve-Path (Split-Path -Parent $MyInvocation.MyCommand.Path)).Path.TrimEnd('\')
Push-Location $root

$rspPath = $null
$tempOut = $null

try
{
    $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
    if (-not (Test-Path (Join-Path $framework 'csc.exe')))
    {
        $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
    }
    $csc = Join-Path $framework 'csc.exe'
    if (-not (Test-Path $csc)) { throw 'Не найден csc.exe из .NET Framework 4.x.' }

    $sources = @(Get-ChildItem -Path 'src' -Filter '*.cs' -Recurse -File |
        Sort-Object FullName |
        ForEach-Object { $_.FullName.Substring($root.Length + 1) })
    if ($sources.Count -eq 0) { throw 'В папке src не найдено ни одного .cs файла.' }

    $required = @(
        'assets\vexor.ico',
        'src\app.manifest',
        'assets\loader-fabric.png',
        'assets\loader-forge.png'
    )
    foreach ($file in $required)
    {
        if (-not (Test-Path $file)) { throw "Не найден обязательный файл: $file" }
    }

    $resources = @(
        @{ File = 'assets\loader-fabric.png'; Name = 'Vexor.Assets.loader-fabric.png' },
        @{ File = 'assets\loader-forge.png'; Name = 'Vexor.Assets.loader-forge.png' }
    )

    $references = @(
        'System.dll',
        'System.Core.dll',
        'System.Drawing.dll',
        'System.Windows.Forms.dll',
        'System.Net.Http.dll',
        'System.Security.dll',
        'System.IO.Compression.dll',
        'System.IO.Compression.FileSystem.dll'
    ) | ForEach-Object { Join-Path $framework $_ }
    $references += Join-Path $framework 'WPF\PresentationCore.dll'
    $references += Join-Path $framework 'WPF\WindowsBase.dll'

    $xaml = Get-ChildItem (Join-Path $env:WINDIR 'Microsoft.NET\assembly\GAC_MSIL\System.Xaml') `
        -Recurse -Filter 'System.Xaml.dll' -File -ErrorAction SilentlyContinue |
        Sort-Object FullName | Select-Object -Last 1
    if (-not $xaml) { throw 'Не найден System.Xaml.dll в GAC.' }
    $references += $xaml.FullName

    foreach ($reference in $references)
    {
        if (-not (Test-Path $reference)) { throw "Не найдена сборка: $reference" }
    }

    $outDir = Split-Path -Parent $Output
    if ($outDir -and -not (Test-Path $outDir))
    {
        New-Item -ItemType Directory -Path $outDir | Out-Null
    }
    $tempOut = Join-Path $outDir ([IO.Path]::GetFileNameWithoutExtension($Output) + '.new.exe')
    if (Test-Path $tempOut) { Remove-Item $tempOut -Force }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('/nologo')
    $lines.Add('/target:winexe')
    $lines.Add('/optimize+')
    $lines.Add('/codepage:65001')
    $lines.Add('/out:' + $tempOut)
    $lines.Add('/win32icon:assets\vexor.ico')
    $lines.Add('/win32manifest:src\app.manifest')
    foreach ($resource in $resources)
    {
        $lines.Add('/resource:' + $resource.File + ',' + $resource.Name)
    }
    foreach ($reference in $references)
    {
        $lines.Add('/reference:' + $reference)
    }
    foreach ($source in $sources)
    {
        $lines.Add($source)
    }

    $rspDir = if ($outDir) { $outDir } else { '.' }
    $rspPath = Join-Path $rspDir ('.pack-' + $PID + '.rsp')
    [IO.File]::WriteAllLines($rspPath, $lines.ToArray(), (New-Object System.Text.UTF8Encoding($true)))

    Write-Host 'Сборка Vexor Launcher'
    Write-Host ('  исходники: ' + $sources.Count)
    Write-Host ('  ресурсы:   ' + $resources.Count + ' PNG + иконка + манифест')
    Write-Host ('  компилятор: ' + $csc)
    Write-Host ''

    $compilerOutput = & $csc ('@' + $rspPath) 2>&1
    $exitCode = $LASTEXITCODE
    if ($compilerOutput) { $compilerOutput | ForEach-Object { Write-Host $_ } }
    if ($exitCode -ne 0) { throw "csc завершился с кодом $exitCode." }
    if (-not (Test-Path $tempOut)) { throw 'csc не создал выходной файл.' }

    Move-Item -Force $tempOut $Output
    $tempOut = $null

    $file = Get-Item $Output
    $hash = (Get-FileHash -Algorithm SHA256 $file.FullName).Hash
    Write-Host ''
    Write-Host ('Готово: ' + $file.FullName) -ForegroundColor Green
    Write-Host ('  размер: ' + [Math]::Round($file.Length / 1MB, 2) + ' МБ')
    Write-Host ('  SHA256: ' + $hash)

    if ($Run)
    {
        Start-Process -FilePath $file.FullName
        Write-Host '  лаунчер запущен.'
    }
}
catch
{
    Write-Host ('Ошибка упаковки: ' + $_.Exception.Message) -ForegroundColor Red
    exit 1
}
finally
{
    if ($rspPath -and (Test-Path -LiteralPath $rspPath)) { Remove-Item -LiteralPath $rspPath -Force -ErrorAction SilentlyContinue }
    if ($tempOut -and (Test-Path -LiteralPath $tempOut)) { Remove-Item -LiteralPath $tempOut -Force -ErrorAction SilentlyContinue }
    Pop-Location
}
