<#
.SYNOPSIS
    Готовит файлы обновления Vexor Launcher и manifest.json для загрузки на хостинг.

.DESCRIPTION
    Копирует собранный exe в папку версии, считает SHA-256 и создаёт манифест.
    URL в манифесте должен быть абсолютным и HTTPS (HTTP разрешён только для localhost).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\publish-update.ps1 -BaseUrl "https://example.com/vexor/updates" -Notes "Что нового"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,
    [string]$Version = '0.1.4',
    [string]$Notes = 'Обновление Vexor Launcher',
    [string]$Exe = 'dist\VexorLauncher.exe',
    [string]$AssetUrl = '',
    [string]$Output = 'dist\update'
)

$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }

$root = (Resolve-Path (Split-Path -Parent $MyInvocation.MyCommand.Path)).Path.TrimEnd('\')
Push-Location $root
try
{
    $base = $BaseUrl.Trim().TrimEnd('/')
    $uri = $null
    if (-not [Uri]::TryCreate($base, [UriKind]::Absolute, [ref]$uri)) { throw 'BaseUrl должен быть абсолютным адресом.' }
    $isHttps = $uri.Scheme -eq [Uri]::UriSchemeHttps
    $isLocal = $uri.Scheme -eq [Uri]::UriSchemeHttp -and $uri.IsLoopback
    if (-not ($isHttps -or $isLocal)) { throw 'BaseUrl должен использовать HTTPS (HTTP — только для localhost).' }

    $exeUrl = $base + '/' + $Version + '/VexorLauncher.exe'
    if ($AssetUrl)
    {
        $assetUri = $null
        if (-not [Uri]::TryCreate($AssetUrl.Trim(), [UriKind]::Absolute, [ref]$assetUri))
        {
            throw 'AssetUrl должен быть абсолютным адресом.'
        }
        $assetHttps = $assetUri.Scheme -eq [Uri]::UriSchemeHttps
        $assetLocal = $assetUri.Scheme -eq [Uri]::UriSchemeHttp -and $assetUri.IsLoopback
        if (-not ($assetHttps -or $assetLocal)) { throw 'AssetUrl должен использовать HTTPS (HTTP — только для localhost).' }
        $exeUrl = $assetUri.AbsoluteUri
    }

    if (-not (Test-Path -LiteralPath $Exe)) { throw "Не найден собранный файл: $Exe" }
    $codeVersion = $null
    $appInfo = Get-Content 'src\Core\AppInfo.cs' -Raw -Encoding UTF8
    if ($appInfo -match 'Version\s*=\s*"([^"]+)"') { $codeVersion = $Matches[1] }
    if (-not $codeVersion) { throw 'Не удалось прочитать версию из src\Core\AppInfo.cs.' }
    if ($codeVersion -ne $Version) { throw "Версия в AppInfo.cs ($codeVersion) не совпадает с -Version ($Version)." }

    $versionDir = Join-Path $Output $Version
    if (Test-Path -LiteralPath $versionDir) { Remove-Item -LiteralPath $versionDir -Recurse -Force }
    New-Item -ItemType Directory -Path $versionDir -Force | Out-Null

    $target = Join-Path $versionDir 'VexorLauncher.exe'
    Copy-Item -LiteralPath $Exe -Destination $target
    $file = Get-Item -LiteralPath $target
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash.ToLowerInvariant()

    $manifest = [ordered]@{
        version    = $Version
        notes      = $Notes
        executable = 'VexorLauncher.exe'
        files      = @(
            [ordered]@{
                path   = 'VexorLauncher.exe'
                url    = $exeUrl
                sha256 = $hash
                size   = $file.Length
            }
        )
    }

    $manifestPath = Join-Path $Output 'manifest.json'
    $json = ($manifest | ConvertTo-Json -Depth 5)
    [IO.File]::WriteAllText($manifestPath, $json + [Environment]::NewLine,
        (New-Object System.Text.UTF8Encoding($false)))

    Write-Host 'Обновление готово' -ForegroundColor Green
    Write-Host ('  версия:    ' + $Version)
    Write-Host ('  файл:      ' + $file.FullName)
    Write-Host ('  размер:    ' + $file.Length + ' байт')
    Write-Host ('  SHA256:    ' + $hash)
    Write-Host ('  манифест:  ' + (Resolve-Path $manifestPath).Path)
    Write-Host ''
    Write-Host 'Загрузите на хостинг:'
    Write-Host ('  ' + $base + '/manifest.json')
    Write-Host ('  ' + $exeUrl)
}
catch
{
    Write-Host ('Ошибка подготовки обновления: ' + $_.Exception.Message) -ForegroundColor Red
    exit 1
}
finally
{
    Pop-Location
}
