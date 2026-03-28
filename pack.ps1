# Build Release and zip for Dungeon Helper — layout: plugins/{PluginName}/*
param(
    [string]$Version = "1.1.0",
    [string]$PluginName = "KeyRep"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

$proj = Join-Path $root "src\KeyRep\KeyRep.csproj"
$staging = Join-Path $root "dist\pack_staging"
$pluginOut = Join-Path $staging "plugins\$PluginName"

Write-Host "Publishing Release -> $pluginOut"
if (Test-Path $staging) {
    Remove-Item -Recurse -Force $staging
}
New-Item -ItemType Directory -Force -Path $pluginOut | Out-Null

dotnet publish $proj -c Release -p:Platform=x64 -o $pluginOut
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$removeNames = @(
    "System.Drawing.Common.dll",
    "Microsoft.Windows.SDK.NET.dll",
    "VoK.Sdk.dll",
    "WinRT.Runtime.dll",
    "Newtonsoft.Json.dll",
    "log4net.dll"
)
foreach ($name in $removeNames) {
    $p = Join-Path $pluginOut $name
    if (Test-Path $p) {
        Write-Host "Removing $name"
        Remove-Item -Force $p
    }
}
Get-ChildItem -Path $pluginOut -Filter *.pdb -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "Removing $($_.Name)"
    Remove-Item -Force $_.FullName
}

Write-Host "Package folder contents:"
Get-ChildItem $pluginOut | ForEach-Object { Write-Host "  $($_.Name)" }

function Write-DungeonHelperPluginZip {
    param(
        [string]$ZipPath,
        [string]$FolderPath,
        [string]$Name
    )
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $tmp = "$ZipPath.$([Guid]::NewGuid().ToString('n')).tmp.zip"
    $fs = [System.IO.File]::Open($tmp, [System.IO.FileMode]::Create, [System.IO.FileAccess]::ReadWrite)
    try {
        $archive = [System.IO.Compression.ZipArchive]::new($fs, [System.IO.Compression.ZipArchiveMode]::Create)
        try {
            $files = Get-ChildItem -LiteralPath $FolderPath -File | Sort-Object Name
            foreach ($f in $files) {
                $entryName = "plugins/$Name/$($f.Name)"
                [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                    $archive,
                    $f.FullName,
                    $entryName,
                    [System.IO.Compression.CompressionLevel]::Optimal
                ) | Out-Null
            }
        } finally {
            $archive.Dispose()
        }
    } finally {
        $fs.Dispose()
    }
    if (Test-Path -LiteralPath $ZipPath) {
        Remove-Item -LiteralPath $ZipPath -Force -ErrorAction SilentlyContinue
    }
    Move-Item -LiteralPath $tmp -Destination $ZipPath -Force
}

$zip = Join-Path $root "dist\${PluginName}_$Version.zip"
New-Item -ItemType Directory -Force -Path (Split-Path $zip) | Out-Null
Write-DungeonHelperPluginZip -ZipPath $zip -FolderPath $pluginOut -Name $PluginName
Write-Host "Created $zip"

$dh = Join-Path $env:APPDATA "Dungeon Helper\plugins\$PluginName"
New-Item -ItemType Directory -Force -Path $dh | Out-Null
Copy-Item -Path (Join-Path $pluginOut "*") -Destination $dh -Force
Write-Host "Copied plugin binaries to $dh"

Write-Host "Done."
